using PrototypeOmega;

namespace DirectorySelectorDemo;

// 2026-03-16: ce qui reste a faire
// ?? pretty much completed !!  let's make a backup !!
// publish NuGet to the world ?

public partial class Form1 : Form {
    private List<string> _lstDirectory = [];

    public Form1() {
        InitializeComponent();
        this._lstDirectory.Add(@"C:\");
        this._lstDirectory.Add(@"E:\");

        this.cmdOpenDialog.Click += this.CmdOpenDialog_Click;
    }

    private void CmdOpenDialog_Click(object? sender, EventArgs e) {
        using (DirectorySelector objDialog = new()) {
            objDialog.Title = "Select a directory... or more...";
            objDialog.Size = new System.Drawing.Size(500, 500);
            objDialog.MinimumSize = objDialog.Size;
            objDialog.UserSelection = this._lstDirectory;

            if (objDialog.ShowDialog() == DialogResult.OK) {
                System.Diagnostics.Debug.WriteLine("User confirmed the dialog.");
                this._lstDirectory = objDialog.UserSelection;
                System.Diagnostics.Debug.WriteLine(this._lstDirectory.Count);
            }
        }
    }
}
