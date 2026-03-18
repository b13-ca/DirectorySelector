using PrototypeOmega;

namespace DirectorySelectorDemo;

public partial class Form1 : Form {
    private List<string> _lstDirectory = [];

    public Form1() {
        InitializeComponent();
        this.cmdOpenDialog.Click += this.CmdOpenDialog_Click;

        //** Here you can add default checked value **//
        this._lstDirectory.Add(@"C:\");
        //this._lstDirectory.Add(@"E:\");
    }

    private void CmdOpenDialog_Click(object? sender, EventArgs e) {
        using (DirectorySelector objDialog = new()) {
            objDialog.Title = "Select a directory... or more...";
            objDialog.Size = new System.Drawing.Size(500, 500);
            objDialog.MinimumSize = objDialog.Size;

            //** Here you tell the component to select the predefined value **//
            objDialog.UserSelection = this._lstDirectory;

            //** You show the dialog to the user **//
            if (objDialog.ShowDialog() == DialogResult.OK) {
                //** if user press ok, you can get back his selection **//

                //** THIS IS WHERE YOU GET BACK USER SELECTION **//
                this._lstDirectory = objDialog.UserSelection;
                //System.Diagnostics.Debug.WriteLine(this._lstDirectory.Count);
            }
        }
    }
}
