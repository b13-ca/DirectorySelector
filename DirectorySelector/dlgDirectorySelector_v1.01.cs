// Copyright 2026, Patrice CHARBONNEAU
//                 a.k.a. Sigma3Wolf
//                 oIId: v2.00/2032/160e0e6a3176a8c4235332aa8e0d422c
//                 All rights reserved.
//                 https://b13.ca/
//
// This source code is licensed under the [BSD 3-Clause "New" or "Revised" License] found
// in the LICENSE file in the root directory of this source tree.

//*************************************************************************************************//
//** WARNING: If you modify this file, you MUST rename it to exclude the version number :WARNING **//
//*************************************************************************************************//
//      Usage: Custom Template for Dialog using custom OCX
// Dependency:

//    History:
// v1.00 - 2026-03-15:	Init;
// v1.01 - 2026-03-16:  oIId changed;

using System.ComponentModel;

#region PrototypeOmega namespace
#pragma warning disable IDE0130
namespace PrototypeOmega;
#pragma warning restore IDE0130
#endregion PrototypeOmega namespace

//[ToolboxItemFilter("PrototypeOmega", ToolboxItemFilterType.Allow)]
//[Guid("9A2B1C3D-4E5F-6A7B-8C9E-0E1F2A3B4C5E")]
[ToolboxItem(true)]
[ToolboxBitmap(typeof(DirectorySelector))]
[Category("PrototypeOmega")]
[Description("A custom directory selection dialog component.")]
public class DirectorySelector : Component {
    //public DirectorySelector() {}

    // Form min size must be (+25, +50) more then DirectorySelector windows dialog
    //private static readonly Size MINIMUM_SIZE = new Size(350 + 25, 400 + 50);
    private const string MINIMUM_SIZE_STR = "375, 450";
    private static readonly Size MINIMUM_SIZE = new Size(375, 450);
    
    private Size _minimumSize = MINIMUM_SIZE;
    [Category("PrototypeOmega.Appearance")]
    [Description("The MinimumSize of the dialog window.")]
    [DefaultValue(typeof(Size), MINIMUM_SIZE_STR)]
    public Size MinimumSize {
        get {
            return this._minimumSize;
        }
        set {
            this._minimumSize = this.GetValidSize(value);
        }
    }

    private Size _size = MINIMUM_SIZE;
    [Category("PrototypeOmega.Appearance")]
    [Description("The Size of the dialog window.")]
    [DefaultValue(typeof(Size), MINIMUM_SIZE_STR)]
    public Size Size {
        get {
            return this._size;
        }
        set {
            this._size = this.GetValidSize(value);
        }
    }

    private string _title = "Directory Selector";
    [Category("PrototypeOmega.Appearance")]
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
    [Category("PrototypeOmega.UserSelection")]
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

    private Size GetValidSize(Size psz) {
        int lngSx = psz.Width;
        int lngSy = psz.Height;

        if (lngSx < MINIMUM_SIZE.Width) {
            lngSx = MINIMUM_SIZE.Width;
        }

        if (lngSy < MINIMUM_SIZE.Height) {
            lngSy = MINIMUM_SIZE.Height;
        }

        return new Size(lngSx, lngSy);
    }

    public DialogResult ShowDialog(IWin32Window? owner = null) {
        DialogResult objRet = DialogResult.None;

        // Just-in-time initialization of the UI
        using (DialogDirectorySelectorForm objForm = new DialogDirectorySelectorForm(this._minimumSize, this._size, this._title, this._lstDirectory)) {
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
}

internal class DialogDirectorySelectorForm : Form {
    public readonly DirTreeViewOcx _tree;

    public DialogDirectorySelectorForm(Size pszMinimumSize, Size pszSize, string pstrTitle, List<string> plstUserSelection) {
        this.MinimumSize = pszMinimumSize;
        this.Size = pszSize;
        this.Text = pstrTitle;
        
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MaximizeBox = true;
        this.MinimizeBox = false;

        this._tree = new(this, plstUserSelection) {
            Top = 0,
            Left = 0,
            Width = this.ClientSize.Width,
            Height = this.ClientSize.Height,
            Dock = DockStyle.Fill
        };
        this.Controls.Add(this._tree);
    }
}
