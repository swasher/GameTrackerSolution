namespace GameTrackerClient
{
    partial class SettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.foldersListBox = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.addFolderButton = new System.Windows.Forms.Button();
            this.removeFolderButton = new System.Windows.Forms.Button();
            this.saveButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.showHiddenCheckBox = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // foldersListBox
            // 
            this.foldersListBox.FormattingEnabled = true;
            this.foldersListBox.ItemHeight = 15;
            this.foldersListBox.Location = new System.Drawing.Point(12, 27);
            this.foldersListBox.Name = "foldersListBox";
            this.foldersListBox.Size = new System.Drawing.Size(411, 184);
            this.foldersListBox.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 15);
            this.label1.TabIndex = 1;
            this.label1.Text = "Watched Folders:";
            // 
            // addFolderButton
            // 
            this.addFolderButton.Location = new System.Drawing.Point(429, 27);
            this.addFolderButton.Name = "addFolderButton";
            this.addFolderButton.Size = new System.Drawing.Size(100, 23);
            this.addFolderButton.TabIndex = 2;
            this.addFolderButton.Text = "Add...";
            this.addFolderButton.UseVisualStyleBackColor = true;
            this.addFolderButton.Click += new System.EventHandler(this.addFolderButton_Click);
            // 
            // removeFolderButton
            // 
            this.removeFolderButton.Location = new System.Drawing.Point(429, 56);
            this.removeFolderButton.Name = "removeFolderButton";
            this.removeFolderButton.Size = new System.Drawing.Size(100, 23);
            this.removeFolderButton.TabIndex = 3;
            this.removeFolderButton.Text = "Remove";
            this.removeFolderButton.UseVisualStyleBackColor = true;
            this.removeFolderButton.Click += new System.EventHandler(this.removeFolderButton_Click);
            // 
            // showHiddenCheckBox
            // 
            this.showHiddenCheckBox.AutoSize = true;
            this.showHiddenCheckBox.Location = new System.Drawing.Point(12, 226);
            this.showHiddenCheckBox.Name = "showHiddenCheckBox";
            this.showHiddenCheckBox.Size = new System.Drawing.Size(158, 19);
            this.showHiddenCheckBox.TabIndex = 4;
            this.showHiddenCheckBox.Text = "Show hidden applications";
            this.showHiddenCheckBox.UseVisualStyleBackColor = true;
            // 
            // saveButton
            // 
            this.saveButton.Location = new System.Drawing.Point(348, 255);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(75, 23);
            this.saveButton.TabIndex = 5;
            this.saveButton.Text = "Save";
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(454, 255);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 6;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // SettingsForm
            // 
            this.AcceptButton = this.saveButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(541, 290);
            this.Controls.Add(this.showHiddenCheckBox);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.removeFolderButton);
            this.Controls.Add(this.addFolderButton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.foldersListBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Settings";
            this.Load += new System.EventHandler(this.SettingsForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ListBox foldersListBox;
        private Label label1;
        private Button addFolderButton;
        private Button removeFolderButton;
        private Button saveButton;
        private Button cancelButton;
        private CheckBox showHiddenCheckBox;
    }
}