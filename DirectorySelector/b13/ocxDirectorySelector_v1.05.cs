// Copyright 2026, Patrice CHARBONNEAU
//                 a.k.a. Sigma3Wolf
//                 oIId: v2.00/2032/160e0e6a3176a8c4235332aa8e0d422c
//                 All rights reserved.
//                 https://b13.ca/
//
// This source code is licensed under the [BSD 3-Clause "New" or "Revised" License] found
// in the LICENSE file in the root directory of this source tree.

#region Usage and dependency
//*************************************************************************************************//
//** WARNING: If you modify this file, you MUST rename it to exclude the version number :WARNING **//
//*************************************************************************************************//
//      Usage: Custom TreeView control to be used in Directory selection
// Dependency: vcxDirectory_2.08
#endregion Usage and dependency

#region History
//    History:
// v1.00 - 2026-03-14:	Init;
// v1.01 - 2026-03-15:  Run as component, accept Form as constructor parameter
// v1.02 - 2026-03-16:  oIId changed;
// v1.03 - 2026-03-26:  now using non static vcxDirectory_v2.10;
//                      now using b13 namespace;
//                      MultiSelect is now part of constructor and made private;
// v1.04 - 2026-03-27:  Adding ABSOLUTE_MIN_SIZE & MinimumSize as property;
// v1.05 - 2026-03-27:  Fixing .Name vs .Text property; .Name is now unused;

#endregion History

using System.ComponentModel;
using System.Runtime.InteropServices;

#region b13 namespace
#pragma warning disable IDE0130
namespace b13;
#pragma warning restore IDE0130
#endregion b13 namespace

[ToolboxItem(false)]
[Description("A custom treeview for directory browsing")]
//[DefaultEvent("NodeChecked")] //plus tard...
public class DirTreeViewOcx : UserControl {
    #region local variables
    private readonly StructDirectoryEx _directoryEx;
    private readonly InternalTreeView _tree;
    private readonly Panel _topPanelLabel;
    private readonly Label _topLabel;
    private readonly Panel _pnlButton;
    private readonly Button _cmdButtonCancel;
    private readonly Button _cmdButtonSelect;

    private TreeNode? _LastNodeChecked = null;
    private string _LastNodeCheckedKey = "";

    private List<string> _lstInitialSelection = [];
    private List<string> _lstDirectory = [];
    private readonly Form _Parent;
    private const int _SpacerX = 5;

    private readonly Dictionary<string, CustomTreeNode> _NodeData = [];
    #endregion local variables

    #region InternalTreeView
    private class InternalTreeView : TreeView {
        public InternalTreeView() {
            this.DoubleBuffered = true;

            // Configuration du TreeView interne (caché de l'extérieur)
            this.BackColor = Color.WhiteSmoke;
            this.BorderStyle = BorderStyle.None;
            this.DrawMode = TreeViewDrawMode.OwnerDrawAll;
            this.Dock = DockStyle.Fill;
        }

        //we block double click on TreeView
        protected override void WndProc(ref Message m) {
            if ((m.Msg == 0x0203) || (m.Msg == 0x0201)) {
                // 1. on traite le double click comme le simple click
                int lngX = (int)((long)m.LParam & 0xFFFF);
                int lngY = (int)(((long)m.LParam >> 16) & 0xFFFF);

                TreeViewHitTestInfo objInfo = this.HitTest(lngX, lngY);
                if (objInfo.Location != TreeViewHitTestLocations.None || objInfo.Node != null) {
                    // On appelle manuellement le MouseDown pour forcer la remontée
                    MouseButtons objButton = MouseButtons.Left;
                    if (m.Msg == 0x0203) {
                        //Convert double click to Right Button
                        //then in OnInternalMouseDown, we use it to [objNode.Expand() / objNode.Collapse();]
                        objButton = MouseButtons.Right;
                    }

                    this.OnMouseDown(new MouseEventArgs(objButton, 1, lngX, lngY, 0));

                    // On retourne Zero pour que Windows ne traite PAS l'expansion
                    m.Result = IntPtr.Zero;
                    return;
                }
            }

            base.WndProc(ref m);
        }
    }
    #endregion InternalTreeView

    #region Constructor
    private readonly Dictionary<string, string> _dicUserSelection = new(StringComparer.OrdinalIgnoreCase);
    public DirTreeViewOcx(Form pobjParent, List<string> plstUserSelection, bool pblnMultiSelect = true) {
        // InitializeComponent()
        this._directoryEx = new();
        this._tree = new();
        this._topPanelLabel = new();
        this._topLabel = new();
        this._pnlButton = new();
        this._cmdButtonCancel = new();
        this._cmdButtonSelect = new();
        //***********************************************

        this._Parent = pobjParent;

        //use this in your WinForm Desktop app: Application.SetCompatibleTextRenderingDefault(false);
        this.DoubleBuffered = true;
        this.Font = new Font("Consolas", 16F, FontStyle.Regular);
        this.BorderStyle = BorderStyle.FixedSingle;

        // Liaison des événements internes vers tes méthodes privées
        this._tree.DrawNode += (s, e) => { this.OnInternalDrawNode(e); };
        this._tree.MouseDown += (s, e) => { this.OnInternalMouseDown(e); };
        this._tree.BeforeExpand += (s, e) => { this.OnInternalBeforeExpand(e); };
        this.Controls.Add(this._tree);

        this._topPanelLabel.BackColor = Color.CadetBlue;
        this._topPanelLabel.Height = 25;
        this._topPanelLabel.Dock = DockStyle.Top;
        this.Controls.Add(this._topPanelLabel);

        this._topLabel.AutoSize = false;
        this._topLabel.ForeColor = Color.WhiteSmoke;
        this._topLabel.TextAlign = ContentAlignment.TopCenter;
        this._topLabel.Font = this.Font;
        this._topLabel.Top = 0;
        this._topLabel.Left = 0;
        this._topLabel.Width = this._topPanelLabel.Width;
        this._topLabel.Height = this._topPanelLabel.Height;
        this._topLabel.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
        this._topLabel.Top = -1;
        this._topPanelLabel.Controls.Add(this._topLabel);

        //*******************************************
        //**  Button
        this._pnlButton.Margin = new Padding(0);
        this._pnlButton.BackColor = this._tree.BackColor;
        this._pnlButton.Height = 50;
        this._pnlButton.Dock = DockStyle.Bottom;
        this._pnlButton.Font = this.Font;
        this.Controls.Add(this._pnlButton);

        int lngTop = 5;
        this._cmdButtonSelect.Text = "Select";
        this._cmdButtonSelect.Tag = "Select";
        this._cmdButtonSelect.Height = this._pnlButton.Height - (2 * lngTop);
        this._cmdButtonSelect.Width = 115;
        this._cmdButtonSelect.BackColor = Color.FromArgb(32, 178, 170);
        this._cmdButtonSelect.Top = lngTop;
        this._cmdButtonSelect.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
        this._cmdButtonSelect.Left = this._pnlButton.Width - this._cmdButtonSelect.Width - lngTop;
        this._cmdButtonSelect.DialogResult = DialogResult.OK;
        this._Parent.AcceptButton = this._cmdButtonSelect;
        this._pnlButton.Controls.Add(this._cmdButtonSelect);
        this._cmdButtonSelect.MouseLeave += this.Button_MouseLeave;
        this._cmdButtonSelect.MouseEnter += this.Button_MouseEnter;
        this._cmdButtonSelect.Click += this.CmdButton_Click;

        this._cmdButtonCancel.Text = "Cancel";
        this._cmdButtonCancel.Tag = "Cancel";
        this._cmdButtonCancel.Height = this._pnlButton.Height - (2 * lngTop);
        this._cmdButtonCancel.Width = 115;
        this._cmdButtonCancel.BackColor = Color.FromArgb(32, 178, 170);
        this._cmdButtonCancel.Top = lngTop;
        this._cmdButtonCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
        this._cmdButtonCancel.Left = this._cmdButtonSelect.Left - this._cmdButtonCancel.Width - lngTop;
        this._cmdButtonCancel.DialogResult = DialogResult.Cancel;
        this._Parent.CancelButton = this._cmdButtonCancel;
        this._pnlButton.Controls.Add(this._cmdButtonCancel);
        this._cmdButtonCancel.MouseLeave += this.Button_MouseLeave;
        this._cmdButtonCancel.MouseEnter += this.Button_MouseEnter;
        this._cmdButtonCancel.Click += this.CmdButton_Click;
        this.MultiSelect = true;  //pblnMultiSelect; not ready yet
        this._lstInitialSelection = plstUserSelection;

        FixButtons();

        //see: OnHandleCreated
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            // Nettoyage manuel des ressources lourdes
            // Même si Controls.Dispose() aide, être explicite est plus performant
            this._tree?.Dispose();
            this._topPanelLabel?.Dispose();
            this._topLabel?.Dispose();
            this._pnlButton?.Dispose();
            this._cmdButtonCancel?.Dispose();
            this._cmdButtonSelect?.Dispose();
            System.Diagnostics.Trace.WriteLine("b13: DirTreeViewOcx Disposed.");
        }
        base.Dispose(disposing);
    }
    #endregion Constructor

    #region Properties
    public static readonly Size ABSOLUTE_MIN_SIZE = new Size(350, 400);
    private Size _minimumSize = ABSOLUTE_MIN_SIZE;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new Size MinimumSize {
        get {
            return this._minimumSize;
        }
        set {
            this._minimumSize = GetValidSize(value);
        }
    }

    private static Size GetValidSize(Size psz) {
        int lngSx = psz.Width;
        int lngSy = psz.Height;

        if (lngSx < ABSOLUTE_MIN_SIZE.Width) {
            lngSx = ABSOLUTE_MIN_SIZE.Width;
        }

        if (lngSy < ABSOLUTE_MIN_SIZE.Height) {
            lngSy = ABSOLUTE_MIN_SIZE.Height;
        }

        return new Size(lngSx, lngSy);
    }
    #endregion Properties

    #region Public Function
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ColorSquare { get; set; } = Color.Navy;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ColorPlus { get; set; } = Color.DarkRed;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ColorDot { get; set; } = Color.Purple;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int PenWidth { get; set; } = 2;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowExtendedLabel { get; set; } = true;

    public static List<TreeNode> GetAllChildNodes(TreeNode pobjParent) {
        List<TreeNode> lstRet = [];

        if (pobjParent != null) {
            // Initialisation JIT de la pile de travail
            Stack<TreeNode> stackNodes = new Stack<TreeNode>();

            // On prépare la pile avec les enfants du parent fourni
            foreach (TreeNode objChild in pobjParent.Nodes) {
                stackNodes.Push(objChild);
            }

            while (stackNodes.Count > 0) {
                TreeNode objCurrent = stackNodes.Pop();
                lstRet.Add(objCurrent);

                // On ajoute les enfants du noeud actuel à la pile pour traitement futur
                foreach (TreeNode objSubChild in objCurrent.Nodes) {
                    stackNodes.Push(objSubChild);
                }
            }
        }

        return lstRet;
    }

    public List<string> GetUserSelection() {
        return [.. this._lstDirectory];
    }
    #endregion Public Function

    #region Private section
    private bool MultiSelect { get; set; } = true;

    private void BuildDefaultDictionary(List<string> plstUserSelection) {
        foreach (string strItem in plstUserSelection) {
            bool blnSuccess = CheckNodeFromKey(strItem);
            if (blnSuccess) {
                this._dicUserSelection[strItem] = "";
                if (this.MultiSelect == false) {
                    break;
                }
            }
        }
    }

    private void CmdButton_Click(object? sender, EventArgs e) {
        if (sender != null) {
            Button objButton = (Button)sender;
            string strButtonTag = objButton.Tag + "";
            if (strButtonTag == "Select") {
                this._lstDirectory = this.GetCheckedNodePathsIterative();
            } else {
                this._lstDirectory = [];
            }
        }
    }

    private bool CheckNodeFromKey(string pstrNodeKey) {
        //1. Check if it exist
        bool blnResult = StructDirectoryEx.IsValidPath(pstrNodeKey);

        if (blnResult) {
            //2. Crawl up to that node
        }

        return blnResult;
    }

    private List<string> GetCheckedNodePathsIterative() {
        // Variable de retour : Initialisée systématiquement
        List<string> lstRet = [];

        if (this._tree.Nodes.Count > 0) {
            // Initialisation JIT de la pile de travail
            Stack<TreeNode> stackNodes = new Stack<TreeNode>();

            // On charge les nœuds racines dans la pile
            foreach (TreeNode objRoot in this._tree.Nodes) {
                stackNodes.Push(objRoot);
            }

            while (stackNodes.Count > 0) {
                TreeNode objCurrent = stackNodes.Pop();

                // Vérification de l'état Checked
                if (objCurrent.Checked) {
                    // Dans votre structure, le DirectoryPath est stocké dans .Text
                    if (!string.IsNullOrEmpty(objCurrent.Text)) {
                        lstRet.Add(objCurrent.Text);
                    }
                } else {
                    // On ajoute les enfants à la pile pour traitement ultérieur
                    if (objCurrent.Nodes.Count > 0) {
                        foreach (TreeNode objChild in objCurrent.Nodes) {
                            stackNodes.Push(objChild);
                        }
                    }
                }
            }
        }

        return lstRet;
    }

    private static void FixButtons() {
        //Fix Button based on Font
        //Size szText = GetStringSize("Cancel", this.Font);

        //_pnlButton
        //_cmdButtonCancel
        //    _cmdButtonSelect
    }

    private void Button_MouseEnter(object? sender, EventArgs e) {
        //Color o4 = Color.CadetBlue;
        Color c3 = Color.FromArgb(255, 135, 206, 235);

        //Color.FromArgb(173, 216, 230);
        if (sender != null) {
            Button objButton = (Button)sender;
            objButton.BackColor = c3;
            objButton.Top = objButton.Top + 1;
            objButton.Left = objButton.Left + 1;
        }
    }

    private void Button_MouseLeave(object? sender, EventArgs e) {
        Color c0 = Color.FromArgb(32, 178, 170);

        if (sender != null) {
            Button objButton = (Button)sender;
            objButton.BackColor = c0;
            objButton.Top = objButton.Top - 1;
            objButton.Left = objButton.Left - 1;
        }
    }
    
    private TreeNodeCollection Nodes {
        get {
            return this._tree.Nodes;
        }
    }

    private int _TopLabelMaxChar = 0;
    private int TopLabelMaxChar {
        get {
            return this._TopLabelMaxChar;
        }

        set {
            this._TopLabelMaxChar = value;

            //now we fix the label
            this.ShowSelectionLabel();
        }
    }

    private string _TopLabelText = "";
    private string TopLabelText {
        get {
            return this._TopLabelText;
        }
        set {
            this._TopLabelText = value;

            //now we fix the label
            this.ShowSelectionLabel();
        }
    }

    private void ShowSelectionLabel() {
        string strRet = this.TopLabelText;
        if (strRet.Length > 0) {
            if (!this.ShowExtendedLabel) {
                strRet = System.IO.Path.GetFileName(strRet);
            }

            strRet = SplitStringFromMiddle(strRet, this.TopLabelMaxChar);

            //if (strRet.Length > 5 && strRet.Length > this.TopLabelMaxChar) {
            //    //we need truncate
            //    //ReadOnlySpan<char> spnTmp = strRet.AsSpan().Slice(2, strRet.Length - 5);

            //    //if (!string.IsNullOrEmpty(strRet)) {
            //    //    ReadOnlySpan<char> spnTmp = strRet.AsSpan();

            //    //    // Création des deux segments sans allocation
            //    //    int intMid = spnTmp.Length / 2;
            //    //    ReadOnlySpan<char> spnLeft = spnTmp.Slice(0, intMid);
            //    //    ReadOnlySpan<char> spnRight = spnTmp.Slice(intMid);

            //    //    // Initialisation finale avec la syntaxe de concaténation interpolée
            //    //    strRet = $"{spnLeft}(...){spnRight}";
            //    //}
            //}
        }
        this._topLabel.Text = strRet;
    }

    private static string SplitStringFromMiddle(string pstrText, int plngNewSize) {
        string strRet = pstrText;
        if (!string.IsNullOrEmpty(pstrText)) {
            if (pstrText.Length > plngNewSize) {
                // Initialisation JIT du calcul des segments
                // On calcule combien de caractères on garde au total
                // et on divise par 2 pour la gauche et la droite.
                int lngKeep = plngNewSize - 3; // On réserve 5 places pour "..."
                if (lngKeep > 0) {
                    // Initialisation JIT du calcul des segments
                    // On calcule combien de caractères on garde au total
                    // et on divise par 2 pour la gauche et la droite.
                    int lngSideL = lngKeep / 2;
                    int lngSideR = lngKeep - lngSideL;
                    ReadOnlySpan<char> spnPath = strRet.AsSpan();

                    // Slicing sans allocation
                    ReadOnlySpan<char> spnLeft = spnPath.Slice(0, lngSideL);
                    ReadOnlySpan<char> spnRight = spnPath.Slice(spnPath.Length - lngSideR);

                    // Construction finale avec interpolation
                    strRet = $"{spnLeft}...{spnRight}";
                } else {
                    strRet = "...";
                }
            }
        }

        return strRet;
    }

    private int BoxHeight { get; set; } = 0;

    private int GetMaxCharTopLabel(int plngCurrentWidth) {
        int lngRet = 256;

        if (plngCurrentWidth > 0) {
            int intMaxAvailableWidth = plngCurrentWidth;
            Span<char> spnBuffer = stackalloc char[lngRet];
            spnBuffer.Fill('W');
            ReadOnlySpan<char> spnText = spnBuffer;

            while (spnText.Length > 0) {
                // Use GDI to measure current span
                Size szText = GetStringSize(spnText.ToString(), this._topLabel.Font);
                if (szText.Width <= intMaxAvailableWidth) {
                    // We found the length that fits
                    lngRet = spnText.ToString().Length;
                    break;
                }

                // Reduce the length by 1 from the right for the next iteration
                spnText = spnText.Slice(0, spnText.Length - 1);
            }
        }

        return lngRet;
    }

    private static Size GetStringSize(string pstrText, Font pobjFont) {
        // Initialisation JIT de la variable de retour avec une valeur par défaut
        Size szRet = Size.Empty;

        if (!string.IsNullOrEmpty(pstrText) && pobjFont != null) {
            // Mesure GDI (plus précise pour le rendu WinForms standard)
            szRet = TextRenderer.MeasureText(pstrText, pobjFont);
        }

        return szRet;
    }

    private int PopulateTreeView(List<string>? plstRootNodes) {
        int lngRet = 0;

        List<string>? lstNodes = plstRootNodes;
        if (lstNodes == null) {
            //By default the TreeView contain all Physical Drive
            lstNodes = this._directoryEx.GetPhysicalDrives();
        }

        if (lstNodes.Count > 0) {
            try {
                this._tree.BeginUpdate();
                this._NodeData.Clear();
                this._tree.Nodes.Clear();

                foreach (string strEntry in lstNodes) {
                    CustomTreeNode objData = new CustomTreeNode(strEntry);
                    this.AddNode(objData); //second argument is not mandatory here for root object
                    lngRet++;
                }

                TreeNode objFirst = this._tree.Nodes[0];
                this._tree.SelectedNode = objFirst;
                objFirst.EnsureVisible();
                //} catch (Exception ex) {
                //System.Diagnostics.Debug.WriteLine($"Erreur init drives : {ex.Message}");
            } finally {
                this._tree.EndUpdate();
            }
        }

        return lngRet;
    }

    private static int GetExpandButtonPosX(TreeNode pobjNode) {
        int lngRet = pobjNode.Bounds.Left - 16;

        return lngRet;
    }

    private int GetCheckboxButtonPosX(int plngPlusRectX) {
        int lngRet = plngPlusRectX + this.BoxHeight + _SpacerX;

        return lngRet;
    }
    #endregion Private section

    #region MinSize override section
    private const int WM_WINDOWPOSCHANGING = 0x0046;
    private const int SWP_NOSIZE = 0x0001;

    [StructLayout(LayoutKind.Sequential)]
    private struct WINDOWPOS {
        public IntPtr hwnd;
        public IntPtr hwndInsertAfter;
        public int x;
        public int y;
        public int cx;
        public int cy;
        public int flags;
    }

    protected override void WndProc(ref Message m) {
        if (m.Msg == WM_WINDOWPOSCHANGING) {
            // this enforce Minsize of 350x400 in IDE
            WINDOWPOS? objPos = Marshal.PtrToStructure<WINDOWPOS>(m.LParam);

            if (objPos.HasValue) {
                WINDOWPOS objUpdate = objPos.Value;
                // Only enforce if the size is actually being changed
                if ((objUpdate.flags & SWP_NOSIZE) == 0) {
                    bool blnChanged = false;

                    if (objUpdate.cx < ABSOLUTE_MIN_SIZE.Width) {
                        objUpdate.cx = ABSOLUTE_MIN_SIZE.Width;
                        blnChanged = true;
                    }

                    if (objUpdate.cy < ABSOLUTE_MIN_SIZE.Height) {
                        objUpdate.cy = ABSOLUTE_MIN_SIZE.Height;
                        blnChanged = true;
                    }

                    // Write back only if we modified the dimensions
                    if (blnChanged) {
                        Marshal.StructureToPtr(objUpdate, m.LParam, false);
                        m.Result = IntPtr.Zero;
                        return;
                    }
                }
            }
        }

        base.WndProc(ref m);
    }
    #endregion MinSize override section

    #region OnEvent
    protected override void OnResize(EventArgs e) {
        base.OnResize(e);

        int intCurrentWidth = this.ClientSize.Width;
        //int intCurrentHeight = this.ClientSize.Height;

        this.TopLabelMaxChar = this.GetMaxCharTopLabel(intCurrentWidth);
    }

    protected override void OnFontChanged(EventArgs e) {
        // 1. Call base first to ensure the new font is applied internally
        base.OnFontChanged(e);

        // 2. Intercept the change
        if (this.Font != null) {
            // 2. Explicitly sync the label font to the new UserControl font
            if (this._topLabel != null) {
                //this._topLabel.Font = this.Font;

                //this._topPanelLabel.Height = (int)Math.Ceiling(this.Font.GetHeight()) + 4;
                //this._topLabel.Height = this._topPanelLabel.Height;
                //this.TopLabelMaxChar = this.GetMaxCharTopLabel(this.ClientSize.Width);
            }
        }
    }

    protected override void OnHandleCreated(EventArgs e) {
        base.OnHandleCreated(e);

        //CheckNodeFromKey
        // this is how we check for Program vs VsEditor
        //if (this.DesignMode == false) {
        //    //this.PopulateTreeView(null);
        //}

        //we want it anyway... but now we know how to avoid doing stuff in design mode ... ;)
        this.BuildDefaultDictionary(this._lstInitialSelection);
        this.PopulateTreeView(null);
        //System.Diagnostics.Debug.WriteLine("Runtime : Arbre peuplé.");
    }
    #endregion OnEvent
    
    //private void panel1_Paint(object sender, PaintEventArgs e) {
    //    //https://stackoverflow.com/questions/8283631/graphics-drawstring-vs-textrenderer-drawtextwhich-can-deliver-better-quality
    //    //NOTE: Use GDI (.net compatible) and NOT GDI+ (leaking in .net)
    //    //GDI   : TextRenderer.MeasureText, TextRenderer.DrawText
    //    //GDI+  : graphics.MeasureString, graphics.DrawString
    //    //use   : Application.SetCompatibleTextRenderingDefault(false);

    //    //GDI (i.e. TextRenderer)
    //    String s = "The quick brown fox jumped over the lazy dog";
    //    Point origin = new Point(11, 11);
    //    Font font = this.Font;

    //    e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;

    //    TextRenderer.DrawText(e.Graphics, s, font, origin, SystemColors.InfoText);
    //}

    //private static int GetCheckboxPosition(int plngStartPos, int plngBoxSize) {
    //    int lngRet = plngStartPos;
    //    lngRet = lngRet + _CheckboxAlign + plngBoxSize;

    //    return lngRet;
    //}

    #region Private functions
    private void OnInternalDrawNode(DrawTreeNodeEventArgs e) {
        if (e != null && e.Node != null && !string.IsNullOrEmpty(e.Node.Text)) {
            TreeNode objNode = e.Node;
            if (e.Bounds.Width > 0 && e.Bounds.Height > 0) {
                //const int lngPaddingX = 3;
                int lngPaddingY = (e.Bounds.Height / 8) + 2;
                int lngScrollbarWidth = SystemInformation.VerticalScrollBarWidth;

                bool blnHasChild = false;
                CustomTreeNode? objTreeNode = this.GetNodeDataByKey(objNode.Text);
                if (objTreeNode != null) {
                    blnHasChild = objTreeNode.HasChild;
                }

                this.BoxHeight = e.Bounds.Height - (2 * lngPaddingY);
                int lngPlusRectX = GetExpandButtonPosX(objNode);
                int lngCheckRectX = this.GetCheckboxButtonPosX(lngPlusRectX);
                Rectangle rctPlus = new Rectangle(lngPlusRectX, (e.Bounds.Top + lngPaddingY + 1), this.BoxHeight, this.BoxHeight);
                Rectangle rctCheck = new Rectangle(lngCheckRectX, (rctPlus.Y - 1), this.BoxHeight + 2, this.BoxHeight + 2);

                //int lngCheckRectX = lngPlusRectX + this.BoxHeight + _SpacerX;
                int lngTextDelaY = 1;
                int lngTextDelaX = e.Bounds.Height / 7;

                int lngTextPosX = (rctCheck.Left + rctCheck.Width - lngTextDelaX);
                int lngTextPosY = e.Bounds.Top + lngTextDelaY;
                int lngTextPosWidth = this.ClientSize.Width - lngTextPosX - lngScrollbarWidth;
                int lngTextPosHeight = e.Bounds.Height - lngTextDelaY;
                Rectangle rectText = new Rectangle(lngTextPosX, lngTextPosY, lngTextPosWidth, lngTextPosHeight);

                // 2. Gestion de l'arrière-plan de sélection
                if ((e.State & TreeNodeStates.Focused) != 0) {
                    // Initialisation de la couleur de sélection (ex: bleu clair système ou personnalisé)
                    Color clrSelect = Color.FromArgb(200, 220, 240); // Exemple de bleu très clair

                    // On calcule la zone totale identique rectFullRow
                    int lngDeltaFullRect = rctCheck.Width;
                    Rectangle rectFullRow = new Rectangle(rectText.Left - lngDeltaFullRect, rectText.Top, rectText.Width + lngDeltaFullRect, rectText.Height);
                    using (Brush objBrush = new SolidBrush(clrSelect)) {
                        e.Graphics.FillRectangle(objBrush, rectFullRow);
                    }

                    // On dessine le rectangle de focus par-dessus l'arrière-plan
                    ControlPaint.DrawFocusRectangle(e.Graphics, rectFullRow);
                } else {
                    // Fond normal
                    using (Brush objBrush = new SolidBrush(this._tree.BackColor)) {
                        e.Graphics.FillRectangle(objBrush, new Rectangle(0, e.Bounds.Top, this.ClientSize.Width, e.Bounds.Height));
                    }
                }

                // 3. Dessin des glyphes et du texte (par-dessus le fond)
                // 3. Dessin du bouton d'expansion [+] / [-]
                //int lngDot = 0;
                Color colGlyph = this.ColorPlus;
                if (!blnHasChild) {
                    //lngDot = 3; //that's the reduction size, valid value are [0-4];
                    colGlyph = this.ColorDot;
                }
                using (Pen objPen = new Pen(this.ColorSquare, this.PenWidth)) {
                    e.Graphics.DrawRectangle(objPen, rctPlus);
                }

                // [+] / [-]
                using (Pen objPen = new Pen(colGlyph, this.PenWidth)) {
                    int lngDotSize = 3;
                    if (blnHasChild) {
                        lngDotSize = (rctPlus.Height - (int)(rctPlus.Height / 2.15f));
                        if ((lngDotSize & 1) == 0) {
                            lngDotSize++;
                        }
                    }

                    int lngHalfX = rctPlus.Left + (rctPlus.Width - lngDotSize) / 2;
                    int lngHalfY = rctPlus.Top + (rctPlus.Height - lngDotSize) / 2;

                    int lngPosX = rctPlus.Left + (rctPlus.Width / 2);
                    int lngPosY = rctPlus.Top + (rctPlus.Height / 2);

                    e.Graphics.DrawLine(objPen, lngHalfX, lngPosY, lngHalfX + lngDotSize + 1, lngPosY);
                    if (objNode.IsExpanded == false) {
                        e.Graphics.DrawLine(objPen, lngPosX, lngHalfY, lngPosX, lngHalfY + lngDotSize + 1);
                    }
                }

                // 4. Dessin de la CheckBox
                //System.Diagnostics.Debug.WriteLine(rectCheck.ToString());
                ControlPaint.DrawCheckBox(e.Graphics, rctCheck, objNode.Checked ? ButtonState.Checked : ButtonState.Normal);

                // 5. Dessin du Texte
                // [Texte] (Note: on peut changer la ForeColor ici si le fond est foncé)

                //https://stackoverflow.com/questions/8283631/graphics-drawstring-vs-textrenderer-drawtextwhich-can-deliver-better-quality
                //NOTE: Use GDI (.net compatible) and NOT GDI+ (leaking in .net)
                //GDI   : TextRenderer.MeasureText, TextRenderer.DrawText
                //GDI+  : graphics.MeasureString, graphics.DrawString
                //use   : Application.SetCompatibleTextRenderingDefault(false);
                TextRenderer.DrawText(e.Graphics, objNode.Text, this.Font, rectText, this.ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            }
        }
    }

    private void ChangeParentState(List<TreeNode> pobjParents, TreeNode pobjNode) {
        bool blnState = pobjNode.Checked;

        if (!blnState) {
            //Change all Parent accordingly
            foreach (TreeNode objParent in pobjParents) {
                objParent.Checked = false;
            }
        } else {
            //that one is sooo nice :) I'm glad I made it
            //ok here it's more complicated.
            //we need to loop through Parent going up watching all siblings
            //if all siblings are .Checked, then we can .Checked the parent
            TreeNode objNode = pobjNode;
            foreach (TreeNode objParent in pobjParents) {
                //System.Diagnostics.Debug.WriteLine(objParent.Text);
                List<TreeNode> objSiblings = this.GetAllSiblingNodes(objNode);
                objNode = objParent;    //we'll need this on next loop;

                bool blnIdentical = true;
                foreach (TreeNode objSibling in objSiblings) {
                    if (!objSibling.Checked) {
                        blnIdentical = false;
                        break;
                    }
                }

                if (blnIdentical) {
                    objParent.Checked = true;
                } else {
                    //we're done
                    break;
                }
            }
        }
    }

    private List<TreeNode> GetAllSiblingNodes(TreeNode pobjNode) {
        // Initialisation de la variable de retour (Liste courte [])
        List<TreeNode> lstRet = [];

        if (pobjNode != null) {
            // Initialisation JIT de la collection à parcourir
            // Si Parent est null, on utilise les Nodes à la racine du TreeView
            TreeNodeCollection targetNodes = pobjNode.Parent?.Nodes ?? this._tree.Nodes;

            foreach (TreeNode objSibling in targetNodes) {
                // On ajoute tout le monde sauf le nœud de référence lui-même
                if (objSibling != pobjNode) {
                    lstRet.Add(objSibling);
                }
            }
        }

        return lstRet;
    }

    private static List<TreeNode> GetAllParentNodes(TreeNode pobjChild) {
        List<TreeNode> lstRet = [];

        if (pobjChild != null) {
            // Initialisation JIT du curseur de remontée
            TreeNode? objCurrent = pobjChild.Parent;

            while (objCurrent != null) {
                lstRet.Add(objCurrent);

                // Remontée au niveau supérieur
                objCurrent = objCurrent.Parent;
            }
        }

        return lstRet;
    }

    private void OnInternalMouseDown(MouseEventArgs e) {
        // 1. Initialisation
        TreeViewHitTestInfo objInfo = this._tree.HitTest(e.Location);
        TreeNode? objNode = objInfo.Node;

        if (objNode != null && !string.IsNullOrEmpty(objNode.Text)) {
            if (this._tree.Focused == false) {
                this._tree.Focus();
            }

            CustomTreeNode? objTreeNode = this.GetNodeDataByKey(objNode.Text);
            if (objTreeNode != null) {
                // 2. Définition de la zone réactive (Hitbox)
                // On définit une zone de 40 pixels de large juste à gauche du texte
                int lngPlusRectX = GetExpandButtonPosX(objNode);
                int lngCheckRectX = this.GetCheckboxButtonPosX(lngPlusRectX);

                // Clic sur le [+] / [-]
                if ((e.X >= lngPlusRectX && e.X <= (lngPlusRectX + this.BoxHeight)) || e.Button == MouseButtons.Right) {
                    if (objTreeNode.HasChild) {
                        if (objNode.IsExpanded) {
                            objNode.Collapse();
                        } else {
                            objNode.Expand();
                        }
                    }

                    this._tree.SelectedNode = objNode;
                    this.TopLabelText = objNode.Text;

                    // we don't need to invalidate because internal proc use global invalidate on [.Expand] / [.Collapse] change
                    //this._tree.Invalidate(objNode.Bounds);
                    return;
                } else if (e.X >= lngCheckRectX && e.X <= (lngCheckRectX + this.BoxHeight)) {
                    // Clic sur la CheckBox
                    objNode.Checked = !objNode.Checked;

                    //Keeping value for SimpleSelect
                    if (this.MultiSelect == false) {
                        if (objNode.Checked) {
                            //if (this._LastNodeChecked != null) {
                            //    this._LastNodeChecked.Checked = false;
                            //    //this.ChangeCheckedValueChild(this._LastNodeChecked);
                            //}

                                if (this._LastNodeCheckedKey.Length > 0) {
                                //retrieve the real node
                                //next line is not good, retrieve a CustomTreeNode, NOT a node
                                CustomTreeNode? objSelectedNode = this.GetNodeDataByKey(this._LastNodeCheckedKey);
                                if (objSelectedNode != null) {
                                    //neeed fix here
                                    //objSelectedNode?.Checked = false;
                                    //this.ChangeCheckedValueChild(objSelectedNode);
                                }
                            }

                            this._LastNodeCheckedKey = objNode.Text;
                        } else {
                            this._LastNodeCheckedKey = "";
                        }
                    }
                    ChangeCheckedValueChild(objNode);
                    this.ChangeCheckedValueParent(objNode);

                    this._tree.SelectedNode = objNode;
                    this.TopLabelText = objNode.Text;

                    // we don't need to invalidate because internal proc use global invalidate on [.Checked] change
                    //this._tree.Invalidate(objNode.Bounds);
                    return;
                } else {
                    this._tree.SelectedNode = objNode;
                    this.TopLabelText = objNode.Text;

                    this._tree.Invalidate(objNode.Bounds);
                }
            }
        }

        base.OnMouseDown(e);
    }

    private static void ChangeCheckedValueChild(TreeNode pobjNode) {
        //Change all Child accordingly
        List<TreeNode> objChilds = GetAllChildNodes(pobjNode);
        foreach (TreeNode objChild in objChilds) {
            objChild.Checked = pobjNode.Checked;
        }
    }

    private void ChangeCheckedValueParent(TreeNode pobjNode) {
        List<TreeNode> objParents = GetAllParentNodes(pobjNode);
        this.ChangeParentState(objParents, pobjNode);
    }

    private TreeNode? AddNode(CustomTreeNode pData, TreeNode? pobjParentNode = null) {
        TreeNode? objRet = null;

        //if (pData != null && pData.Node != null && !string.IsNullOrEmpty(pData.KeyPath)) {
        if (pData != null) {
            //bool blnValid = this._directoryEx.IsFolderValid(pData.KeyPath);
            bool blnValid = this._directoryEx.IsFolderValid(pData.Text);
            if (blnValid) {
                //pData.Node.Text = pData.KeyPath;

                // Si pParent est null, on ajoute à la racine (base.Nodes)
                //if (pobjParentNode != null) {
                //    pobjParentNode.Add(pData.Node);
                //} else {
                //    this._tree.Nodes.Add(pData.Node);
                //}
                TreeNodeCollection targetNodes = pobjParentNode?.Nodes ?? this._tree.Nodes;

                //b13 fix:
                //Check if the node is selected, if so, Checked = true
                //if (this._dicUserSelection.ContainsKey(pData.KeyPath)) {
                //    this._dicUserSelection.Remove(pData.KeyPath);
                //    pData.Node.Checked = true;
                //}
                if (this._dicUserSelection.ContainsKey(pData.Text)) {
                    this._dicUserSelection.Remove(pData.Text);
                    pData.Checked = true;
                }

                targetNodes.Add(pData.TreeNode);
                if (pobjParentNode != null && pobjParentNode.Checked) {
                    pData.Checked = pobjParentNode.Checked;
                }

                //do we need to add Dummy [+] node
                //pData.HasChild = this._directoryEx.HasSubDirectories(pData.KeyPath);
                pData.HasChild = this._directoryEx.HasSubDirectories(pData.Text);
                if (pData.HasChild) {
                    //System.Diagnostics.Debug.WriteLine($"::{pData.Node.Text}");
                    pData.TreeNode.Nodes.Add("+"); // Dummy pour le [+]
                }

                //this._NodeData[pData.KeyPath] = pData;
                this._NodeData[pData.Text] = pData;
                objRet = pData.TreeNode;
                //System.Diagnostics.Debug.WriteLine($"Node ajouté avec clé : {pData.DirectoryPath}");
            }
        }

        return objRet;
    }

    private void ClearAll() {
        try {
            this._tree.BeginUpdate();
            this._NodeData.Clear();
            this._tree.Nodes.Clear();
        } finally {
            this._tree.EndUpdate();
        }
        //System.Diagnostics.Debug.WriteLine("Arbre et dictionnaire vidés.");
    }

    private CustomTreeNode? GetNodeDataByKey(string pstrKeyId) {
        CustomTreeNode? objRet = null;

        if (!string.IsNullOrEmpty(pstrKeyId)) {
            this._NodeData.TryGetValue(pstrKeyId, out objRet);
        }

        return objRet;
    }

    private void OnInternalBeforeExpand(TreeViewCancelEventArgs e) {
        // 1. Déclaration explicite de la nullabilité
        TreeNode? objParentNode = e.Node;

        if (objParentNode != null) {
            // 2. Vérification du dummy [+] node
            if (objParentNode.Nodes.Count == 1 && objParentNode.Nodes[0].Text == "+") {
                try {
                    this._tree.BeginUpdate();
                    objParentNode.Nodes.Clear();
                    string strPath = objParentNode.Text;  // Le Name contient le chemin complet

                    // Logique de chargement des sous-répertoires ici
                    if (System.IO.Directory.Exists(strPath)) {
                        // 3. Initialisation directe avec la valeur finale
                        string[] arrDirs = this._directoryEx.GetVisibleDirectories(strPath);

                        foreach (string strEntry in arrDirs) {
                            // On crée la data et on l'ajoute au noeud qui s'expand
                            CustomTreeNode objData = new CustomTreeNode(strEntry);

                            // On utilise la méthode Add de notre UserControl
                            this.AddNode(objData, objParentNode);
                        }
                    }
                    //} catch (Exception ex) {
                    //System.Diagnostics.Debug.WriteLine($"Erreur d'expansion : {ex.Message}");
                } finally {
                    this._tree.EndUpdate();
                }
            }
        }
    }
    #endregion Private functions
}

#region class CustomTreeNode
public class CustomTreeNode {
    private TreeNode _node;
    private string _shortText;

    public CustomTreeNode(string pstrKey) {
        if (String.IsNullOrEmpty(pstrKey)) {
            throw new ArgumentNullException(nameof(pstrKey));
        }

        this._node = new TreeNode(pstrKey);  //This set Text, NOT NAME
        this._shortText = System.IO.Path.GetFileName(pstrKey);
        this.HasChild = false;
    }

    public TreeNode TreeNode {
        get {
            return this._node;
        }
    }

    public string Text {
        get {
            return this._node.Text;
        }

        private set {
            this._node.Text = value;
        }
    }

    public bool Checked {
        get {
            return this._node.Checked;
        }

        set {
            this._node.Checked = value;
        }
    }

    public string ShortText {
        get {
            return this._shortText;
        }

        private set {
            this._shortText = value;
        }
    }

    public bool HasChild {
        get; set;
    }
}
#endregion class CustomTreeNode