namespace DirectorySelectorDemo;

partial class Form1 {
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing) {
        if (disposing && (components != null)) {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent() {
        this.cmdOpenDialog = new Button();
        this.SuspendLayout();
        // 
        // cmdOpenDialog
        // 
        this.cmdOpenDialog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        this.cmdOpenDialog.Location = new Point(12, 12);
        this.cmdOpenDialog.Name = "cmdOpenDialog";
        this.cmdOpenDialog.Size = new Size(206, 69);
        this.cmdOpenDialog.TabIndex = 0;
        this.cmdOpenDialog.Text = "Dialog";
        this.cmdOpenDialog.UseVisualStyleBackColor = true;
        // 
        // Form1
        // 
        this.AutoScaleMode = AutoScaleMode.None;
        this.ClientSize = new Size(230, 93);
        this.Controls.Add(this.cmdOpenDialog);
        this.Font = new Font("Segoe UI", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
        this.MaximizeBox = false;
        this.Name = "Form1";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Text = "Demo Dialog";
        this.ResumeLayout(false);
    }

    #endregion

    private Button cmdOpenDialog;
}
