namespace AstroModLoader
{
    partial class Form1
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
            components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            headerLabel = new System.Windows.Forms.Label();
            modInfo = new System.Windows.Forms.LinkLabel();
            footerPanel = new System.Windows.Forms.Panel();
            integratingLabel = new System.Windows.Forms.LinkLabel();
            exitButton = new CoolButton();
            settingsButton = new CoolButton();
            playButton = new CoolButton();
            modPanel = new System.Windows.Forms.Panel();
            dataGridView1 = new CoolDataGridView();
            refresh = new CoolButton();
            loadButton = new CoolButton();
            syncButton = new CoolButton();
            PeriodicCheckTimer = new System.Windows.Forms.Timer(components);
            CheckAllDirty = new System.Windows.Forms.Timer(components);
            ForceAutoUpdateRefresh = new System.Windows.Forms.Timer(components);
            contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(components);
            footerPanel.SuspendLayout();
            modPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // headerLabel
            // 
            headerLabel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            headerLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            headerLabel.Location = new System.Drawing.Point(26, 0);
            headerLabel.Margin = new System.Windows.Forms.Padding(0);
            headerLabel.Name = "headerLabel";
            headerLabel.Padding = new System.Windows.Forms.Padding(10, 10, 3, 3);
            headerLabel.Size = new System.Drawing.Size(416, 30);
            headerLabel.TabIndex = 0;
            headerLabel.Text = "Mods:";
            headerLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // modInfo
            // 
            modInfo.ActiveLinkColor = System.Drawing.Color.Red;
            modInfo.AutoSize = true;
            modInfo.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.5F);
            modInfo.LinkArea = new System.Windows.Forms.LinkArea(0, 0);
            modInfo.Location = new System.Drawing.Point(12, 320);
            modInfo.MaximumSize = new System.Drawing.Size(400, 0);
            modInfo.Name = "modInfo";
            modInfo.Size = new System.Drawing.Size(83, 17);
            modInfo.TabIndex = 3;
            modInfo.Text = "Testing 123";
            modInfo.LinkClicked += modInfo_LinkClicked;
            // 
            // footerPanel
            // 
            footerPanel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            footerPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            footerPanel.BackColor = System.Drawing.Color.FromArgb(36, 38, 40);
            footerPanel.Controls.Add(integratingLabel);
            footerPanel.Controls.Add(exitButton);
            footerPanel.Controls.Add(settingsButton);
            footerPanel.Controls.Add(playButton);
            footerPanel.Location = new System.Drawing.Point(0, 441);
            footerPanel.Name = "footerPanel";
            footerPanel.Size = new System.Drawing.Size(468, 50);
            footerPanel.TabIndex = 5;
            // 
            // integratingLabel
            // 
            integratingLabel.ActiveLinkColor = System.Drawing.Color.Red;
            integratingLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 7F);
            integratingLabel.LinkArea = new System.Windows.Forms.LinkArea(0, 0);
            integratingLabel.Location = new System.Drawing.Point(230, 12);
            integratingLabel.MaximumSize = new System.Drawing.Size(400, 0);
            integratingLabel.Name = "integratingLabel";
            integratingLabel.Size = new System.Drawing.Size(145, 26);
            integratingLabel.TabIndex = 6;
            integratingLabel.Text = "Integrating...";
            integratingLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // exitButton
            // 
            exitButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            exitButton.BackColor = System.Drawing.Color.FromArgb(51, 51, 51);
            exitButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            exitButton.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            exitButton.ForeColor = System.Drawing.Color.FromArgb(225, 225, 225);
            exitButton.Location = new System.Drawing.Point(381, 12);
            exitButton.MinimumSize = new System.Drawing.Size(0, 26);
            exitButton.Name = "exitButton";
            exitButton.Size = new System.Drawing.Size(75, 26);
            exitButton.TabIndex = 6;
            exitButton.Text = "Exit";
            exitButton.UseVisualStyleBackColor = false;
            exitButton.Click += exitButton_Click;
            // 
            // settingsButton
            // 
            settingsButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            settingsButton.BackColor = System.Drawing.Color.FromArgb(51, 51, 51);
            settingsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            settingsButton.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            settingsButton.ForeColor = System.Drawing.Color.FromArgb(225, 225, 225);
            settingsButton.Location = new System.Drawing.Point(92, 12);
            settingsButton.MinimumSize = new System.Drawing.Size(0, 26);
            settingsButton.Name = "settingsButton";
            settingsButton.Size = new System.Drawing.Size(82, 26);
            settingsButton.TabIndex = 5;
            settingsButton.Text = "Settings...";
            settingsButton.UseVisualStyleBackColor = false;
            settingsButton.Click += settingsButton_Click;
            // 
            // playButton
            // 
            playButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            playButton.BackColor = System.Drawing.Color.FromArgb(51, 51, 51);
            playButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            playButton.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            playButton.ForeColor = System.Drawing.Color.FromArgb(225, 225, 225);
            playButton.Location = new System.Drawing.Point(12, 12);
            playButton.MinimumSize = new System.Drawing.Size(0, 26);
            playButton.Name = "playButton";
            playButton.Size = new System.Drawing.Size(75, 26);
            playButton.TabIndex = 4;
            playButton.Text = "Play";
            playButton.UseVisualStyleBackColor = false;
            playButton.Click += playButton_Click;
            // 
            // modPanel
            // 
            modPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            modPanel.BackColor = System.Drawing.Color.Transparent;
            modPanel.Controls.Add(dataGridView1);
            modPanel.Controls.Add(refresh);
            modPanel.Controls.Add(loadButton);
            modPanel.Controls.Add(syncButton);
            modPanel.Location = new System.Drawing.Point(0, 35);
            modPanel.Name = "modPanel";
            modPanel.Padding = new System.Windows.Forms.Padding(1, 1, 1, 1);
            modPanel.Size = new System.Drawing.Size(460, 270);
            modPanel.TabIndex = 1;
            // 
            // dataGridView1
            // 
            dataGridView1.AllowDrop = true;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.AllowUserToResizeColumns = false;
            dataGridView1.AllowUserToResizeRows = false;
            dataGridView1.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.BackgroundColor = System.Drawing.Color.FromArgb(40, 42, 45);
            dataGridView1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            dataGridView1.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
            dataGridView1.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(40, 42, 45);
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.FromArgb(225, 225, 225);
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.Color.FromArgb(85, 85, 85);
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            dataGridView1.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.DefaultCellStyle = dataGridViewCellStyle1;
            dataGridView1.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.Location = new System.Drawing.Point(12, 0);
            dataGridView1.MultiSelect = false;
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.RowHeadersWidth = 62;
            dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.Size = new System.Drawing.Size(435, 225);
            dataGridView1.TabIndex = 0;
            dataGridView1.CellPainting += dataGridView1_CellPainting;
            dataGridView1.KeyDown += dataGridView1_KeyDown;
            // 
            // refresh
            // 
            refresh.Anchor = System.Windows.Forms.AnchorStyles.None;
            refresh.BackColor = System.Drawing.Color.FromArgb(51, 51, 51);
            refresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            refresh.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            refresh.ForeColor = System.Drawing.Color.FromArgb(225, 225, 225);
            refresh.Location = new System.Drawing.Point(12, 236);
            refresh.MinimumSize = new System.Drawing.Size(0, 26);
            refresh.Name = "refresh";
            refresh.Size = new System.Drawing.Size(75, 26);
            refresh.TabIndex = 1;
            refresh.Text = "Refresh";
            refresh.UseVisualStyleBackColor = false;
            refresh.Click += refresh_Click;
            // 
            // loadButton
            // 
            loadButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            loadButton.BackColor = System.Drawing.Color.FromArgb(51, 51, 51);
            loadButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            loadButton.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            loadButton.ForeColor = System.Drawing.Color.FromArgb(225, 225, 225);
            loadButton.Location = new System.Drawing.Point(92, 236);
            loadButton.MinimumSize = new System.Drawing.Size(0, 26);
            loadButton.Name = "loadButton";
            loadButton.Size = new System.Drawing.Size(75, 26);
            loadButton.TabIndex = 2;
            loadButton.Text = "Profiles...";
            loadButton.UseVisualStyleBackColor = false;
            loadButton.Click += loadButton_Click;
            // 
            // syncButton
            // 
            syncButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            syncButton.BackColor = System.Drawing.Color.FromArgb(51, 51, 51);
            syncButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            syncButton.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            syncButton.ForeColor = System.Drawing.Color.FromArgb(225, 225, 225);
            syncButton.Location = new System.Drawing.Point(338, 236);
            syncButton.MinimumSize = new System.Drawing.Size(0, 26);
            syncButton.Name = "syncButton";
            syncButton.Size = new System.Drawing.Size(109, 26);
            syncButton.TabIndex = 3;
            syncButton.Text = "Sync from IP";
            syncButton.UseVisualStyleBackColor = false;
            syncButton.Click += syncButton_Click;
            // 
            // PeriodicCheckTimer
            // 
            PeriodicCheckTimer.Interval = 8000;
            PeriodicCheckTimer.Tick += PeriodicCheckTimer_Tick;
            // 
            // CheckAllDirty
            // 
            CheckAllDirty.Interval = 1500;
            CheckAllDirty.Tick += CheckAllDirty_Tick;
            // 
            // ForceAutoUpdateRefresh
            // 
            ForceAutoUpdateRefresh.Interval = 600000;
            ForceAutoUpdateRefresh.Tick += ForceAutoUpdateRefresh_Tick;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            // 
            // Form1
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            BackColor = System.Drawing.Color.FromArgb(40, 42, 45);
            ClientSize = new System.Drawing.Size(469, 491);
            Controls.Add(modPanel);
            Controls.Add(footerPanel);
            Controls.Add(modInfo);
            Controls.Add(headerLabel);
            ForeColor = System.Drawing.Color.FromArgb(225, 225, 225);
            MaximizeBox = false;
            MaximumSize = new System.Drawing.Size(898, 694);
            MinimumSize = new System.Drawing.Size(483, 509);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            Resize += Form1_SizeChanged;
            footerPanel.ResumeLayout(false);
            modPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label headerLabel;
        private AstroModLoader.CoolButton refresh;
        private AstroModLoader.CoolButton playButton;
        private AstroModLoader.CoolButton loadButton;
        private AstroModLoader.CoolButton syncButton;
        private CoolButton settingsButton;
        private CoolButton exitButton;
        private System.Windows.Forms.Panel modPanel;
        private System.Windows.Forms.Timer PeriodicCheckTimer;
        private System.Windows.Forms.Timer CheckAllDirty;
        public System.Windows.Forms.LinkLabel modInfo;
        private System.Windows.Forms.Timer ForceAutoUpdateRefresh;
        public System.Windows.Forms.LinkLabel integratingLabel;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
    }
}

