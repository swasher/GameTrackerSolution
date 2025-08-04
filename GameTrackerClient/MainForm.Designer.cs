namespace GameTrackerClient;

partial class MainForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
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
        components = new System.ComponentModel.Container();
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
        columnHeaderProcess = new System.Windows.Forms.ColumnHeader();
        columnHeaderTime = new System.Windows.Forms.ColumnHeader();
        processListView = new System.Windows.Forms.ListView();
        refreshButton = new System.Windows.Forms.Button();
        renameButton = new System.Windows.Forms.Button();
        hideButton = new System.Windows.Forms.Button();
        trayIcon = new System.Windows.Forms.NotifyIcon(components);
        settingsButton = new System.Windows.Forms.Button();
        trayMenu = new System.Windows.Forms.ContextMenuStrip(components);
        SuspendLayout();
        // 
        // columnHeaderProcess
        // 
        columnHeaderProcess.Name = "columnHeaderProcess";
        columnHeaderProcess.Text = "Process";
        columnHeaderProcess.Width = 350;
        // 
        // columnHeaderTime
        // 
        columnHeaderTime.Name = "columnHeaderTime";
        columnHeaderTime.Text = "Time (sec)";
        columnHeaderTime.Width = 100;
        // 
        // processListView
        // 
        processListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { columnHeaderProcess, columnHeaderTime });
        processListView.Location = new System.Drawing.Point(12, 12);
        processListView.Name = "processListView";
        processListView.Size = new System.Drawing.Size(496, 322);
        processListView.TabIndex = 0;
        processListView.UseCompatibleStateImageBehavior = false;
        processListView.View = System.Windows.Forms.View.Details;
        // 
        // refreshButton
        // 
        refreshButton.Location = new System.Drawing.Point(12, 340);
        refreshButton.Name = "refreshButton";
        refreshButton.Size = new System.Drawing.Size(103, 31);
        refreshButton.TabIndex = 1;
        refreshButton.Text = "Refresh";
        refreshButton.UseVisualStyleBackColor = true;
        refreshButton.Click += refreshButton_Click;
        //
        // renameButton
        //
        renameButton.Location = new System.Drawing.Point(121, 340);
        renameButton.Name = "renameButton";
        renameButton.Size = new System.Drawing.Size(103, 31);
        renameButton.TabIndex = 3;
        renameButton.Text = "Rename...";
        renameButton.UseVisualStyleBackColor = true;
        renameButton.Click += new System.EventHandler(this.renameButton_Click);
        //
        // hideButton
        //
        hideButton.Location = new System.Drawing.Point(230, 340);
        hideButton.Name = "hideButton";
        hideButton.Size = new System.Drawing.Size(103, 31);
        hideButton.TabIndex = 4;
        hideButton.Text = "Hide";
        hideButton.UseVisualStyleBackColor = true;
        hideButton.Click += new System.EventHandler(this.hideButton_Click);
        
        // 
        // trayIcon
        // 
        trayIcon.ContextMenuStrip = trayMenu;
        trayIcon.Text = "Game Tracker";
        trayIcon.Visible = true;
        // 
        // settingsButton
        // 
        settingsButton.Location = new System.Drawing.Point(405, 340);
        settingsButton.Name = "settingsButton";
        settingsButton.Size = new System.Drawing.Size(103, 31);
        settingsButton.TabIndex = 2;
        settingsButton.Text = "Settings...";
        settingsButton.UseVisualStyleBackColor = true;
        settingsButton.Click += new System.EventHandler(this.settingsButton_Click);
        // 
        // trayMenu
        // 
        trayMenu.Name = "trayMenu";
        trayMenu.Size = new System.Drawing.Size(61, 4);
        // 
        // MainForm
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(520, 386);
        Controls.Add(refreshButton);
        Controls.Add(settingsButton);
        Controls.Add(processListView);
        Controls.Add(hideButton);
        Controls.Add(renameButton);
        Text = "Game Tracker";
        ResumeLayout(false);
    }

    private System.Windows.Forms.ColumnHeader columnHeaderProcess;
    private System.Windows.Forms.ColumnHeader columnHeaderTime;

    private System.Windows.Forms.NotifyIcon trayIcon;
    private System.Windows.Forms.ContextMenuStrip trayMenu;

    private System.Windows.Forms.ListView processListView;
    private System.Windows.Forms.Button refreshButton;
    private System.Windows.Forms.Button settingsButton;
    
    private System.Windows.Forms.Button renameButton;
    private System.Windows.Forms.Button hideButton;

    #endregion
}