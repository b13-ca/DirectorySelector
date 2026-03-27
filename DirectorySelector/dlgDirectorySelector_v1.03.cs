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
//      Usage: Custom Template for Dialog using custom OCX
// Dependency: ocxDirectorySelector_v1.04
#endregion Usage and dependency

#region History
//    History:
// v1.00 - 2026-03-15:	Init;
// v1.01 - 2026-03-16:  oIId changed;
// v1.02 - 2026-03-26:  now using b13 namespace;
// v1.03 - 2026-03-27:  MinimumSize now coming from ocxDirectorySelector;
#endregion History

using System.ComponentModel;

#region b13 namespace
#pragma warning disable IDE0130
namespace b13;
#pragma warning restore IDE0130
#endregion b13 namespace

//https://www.softpost.org/dotnet/csproj-file-explained-propetygroups-targetframework-itemgroup
//<GenerateDocumentationFile>true</GenerateDocumentationFile>
//[ToolboxItemFilter("b13", ToolboxItemFilterType.Allow)]
//[Guid("9A2B1C3D-4E5F-6A7B-8C9E-0E1F2A3B4C5E")]
[ToolboxItem(true)]
[ToolboxBitmap(typeof(DirectorySelector))]
[Category("b13")]
[Description("A custom directory selection dialog component.")]
public class DirectorySelector : Component {
    //public DirectorySelector() {}

    #region Public Properties
    [Category("b13.Behavior")]
    [Description("Allow the component to accept multiple selection")]
    [DefaultValue(typeof(bool), "True")]
    public bool MultiSelect { get; set; } = true;

    private Size _size = GetDialogValidSize(new Size(0, 0));
    [Category("b13.Appearance")]
    [Description("The Size of the dialog window.")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Size Size {
        get {
            return this._size;
        }
        set {
            this._size = GetDialogValidSize(value);
        }
    }

    private Size _minimumDialogSize = GetDialogValidSize(new Size(0,0));
    [Category("b13.Appearance")]
    [Description("The MinimumSize of the dialog window.")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Size MinimumDialogSize {
        get {
            return this._minimumDialogSize;
        }
        set {
            this._minimumDialogSize = GetDialogValidSize(value);
        }
    }

    private static Size GetDialogValidSize(Size psz) {
        // [Form minSize] must be (+25, +50) more then [DirTreeViewOcx]
        Size ocxMinSize = DirTreeViewOcx.ABSOLUTE_MIN_SIZE;
        Size frmMinSize = new Size(ocxMinSize.Width + 25, ocxMinSize.Height + 50);

        int lngSx = psz.Width;
        if (lngSx < frmMinSize.Width) {
            lngSx = frmMinSize.Width;
        }

        int lngSy = psz.Height;
        if (lngSy < frmMinSize.Height) {
            lngSy = frmMinSize.Height;
        }

        return new Size(lngSx, lngSy);
    }

    private string _title = "Directory Selector";
    [Category("b13.Appearance")]
    [Description("The title of the dialog window.")]
    [DefaultValue("Directory Selector")]
    public string Title {
        get {
            return this._title;
        }
        set {
            this._title = value;
        }
    }

    private List<string> _lstDirectory = [];
    [Category("b13.UserSelection")]
    [Description("The list of directories selected by the user.")]
    [Browsable(false)] // Hide from property grid because it's runtime data
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public List<string> UserSelection {
        get {
            return [.. this._lstDirectory];
        }
        set {
            this._lstDirectory = value;
        }
    }
    #endregion Public Properties

    #region Public functions
    public DialogResult ShowDialog(IWin32Window? owner = null) {
        DialogResult objRet = DialogResult.None;

        // Just-in-time initialization of the UI
        using (DialogDirectorySelectorForm objForm = new DialogDirectorySelectorForm(this._minimumDialogSize, this._size, this._title, this.MultiSelect, this._lstDirectory)) {
            try {
                // ShowDialog returns the DialogResult of the form
                objRet = objForm.ShowDialog(owner);
                // Check the result specifically for OK
                if (objRet == DialogResult.OK) {
                    // Only assign the final value if the operation succeeded (Just-in-Time)
                    this._lstDirectory = [.. objForm._tree.GetUserSelection()];
                } else {
                    this._lstDirectory = [];
                    //System.Diagnostics.Debug.WriteLine("Dialog cancelled or closed. Result: " + objRet.ToString());
                }
            } catch {
                //} catch (Exception ex) {
                //System.Diagnostics.Debug.WriteLine("Failed to show dialog: " + ex.Message);
                objRet = DialogResult.Abort;
            }
        }

        return objRet;
    }
    #endregion Public functions
}

internal class DialogDirectorySelectorForm : Form {
    public readonly DirTreeViewOcx _tree;

    public DialogDirectorySelectorForm(Size pszMinimumSize, Size pszSize, string pstrTitle, bool pblnMultiSelect, List<string> plstUserSelection) {
        this.MinimumSize = pszMinimumSize;
        this.Size = pszSize;
        this.Text = pstrTitle;

        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MaximizeBox = true;
        this.MinimizeBox = false;

        this._tree = new(this, plstUserSelection, pblnMultiSelect) {
            Top = 0,
            Left = 0,
            Width = this.ClientSize.Width,
            Height = this.ClientSize.Height,
            Dock = DockStyle.Fill,
        };
        this.Controls.Add(this._tree);
    }
}
