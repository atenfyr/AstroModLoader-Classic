namespace AstroModLoader
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
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            gamePathBox = new System.Windows.Forms.TextBox();
            label3 = new System.Windows.Forms.Label();
            accentComboBox = new System.Windows.Forms.ComboBox();
            themeComboBox = new System.Windows.Forms.ComboBox();
            label4 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            platformComboBox = new System.Windows.Forms.ComboBox();
            exitButton = new CoolButton();
            setPathButton = new CoolButton();
            localPathBox = new System.Windows.Forms.TextBox();
            label6 = new System.Windows.Forms.Label();
            setPathButton2 = new CoolButton();
            versionLabel = new System.Windows.Forms.Label();
            refuseMismatchedConnectionsCheckbox = new System.Windows.Forms.CheckBox();
            aboutButton = new CoolButton();
            ue4ssButton = new CoolButton();
            SuspendLayout();
            // 
            // label1
            // 
            label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            label1.Location = new System.Drawing.Point(15, 10);
            label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(453, 24);
            label1.TabIndex = 0;
            label1.Text = "Settings:";
            label1.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            label2.Location = new System.Drawing.Point(15, 81);
            label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(134, 15);
            label2.TabIndex = 1;
            label2.Text = "Game Installation Path:";
            // 
            // gamePathBox
            // 
            gamePathBox.Location = new System.Drawing.Point(178, 80);
            gamePathBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            gamePathBox.Name = "gamePathBox";
            gamePathBox.Size = new System.Drawing.Size(235, 23);
            gamePathBox.TabIndex = 2;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            label3.Location = new System.Drawing.Point(80, 172);
            label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(78, 15);
            label3.TabIndex = 6;
            label3.Text = "Accent Color:";
            // 
            // accentComboBox
            // 
            accentComboBox.FormattingEnabled = true;
            accentComboBox.Location = new System.Drawing.Point(178, 171);
            accentComboBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            accentComboBox.Name = "accentComboBox";
            accentComboBox.Size = new System.Drawing.Size(176, 23);
            accentComboBox.TabIndex = 7;
            accentComboBox.SelectedIndexChanged += accentComboBox_UpdateColor;
            accentComboBox.Leave += accentComboBox_UpdateColor;
            // 
            // themeComboBox
            // 
            themeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            themeComboBox.FormattingEnabled = true;
            themeComboBox.Location = new System.Drawing.Point(178, 140);
            themeComboBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            themeComboBox.Name = "themeComboBox";
            themeComboBox.Size = new System.Drawing.Size(176, 23);
            themeComboBox.TabIndex = 6;
            themeComboBox.SelectedIndexChanged += themeBox_SelectedIndexChanged;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            label4.Location = new System.Drawing.Point(114, 141);
            label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(49, 15);
            label4.TabIndex = 4;
            label4.Text = "Theme:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            label5.Location = new System.Drawing.Point(106, 50);
            label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(56, 15);
            label5.TabIndex = 1;
            label5.Text = "Platform:";
            // 
            // platformComboBox
            // 
            platformComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            platformComboBox.FormattingEnabled = true;
            platformComboBox.Location = new System.Drawing.Point(178, 48);
            platformComboBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            platformComboBox.Name = "platformComboBox";
            platformComboBox.Size = new System.Drawing.Size(176, 23);
            platformComboBox.TabIndex = 1;
            platformComboBox.SelectedIndexChanged += platformComboBox_SelectedIndexChanged;
            // 
            // exitButton
            // 
            exitButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            exitButton.BackColor = System.Drawing.Color.FromArgb(51, 51, 51);
            exitButton.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(255, 231, 149);
            exitButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            exitButton.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            exitButton.ForeColor = System.Drawing.Color.FromArgb(225, 225, 225);
            exitButton.Location = new System.Drawing.Point(14, 243);
            exitButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            exitButton.MinimumSize = new System.Drawing.Size(0, 30);
            exitButton.Name = "exitButton";
            exitButton.Size = new System.Drawing.Size(88, 30);
            exitButton.TabIndex = 9;
            exitButton.Text = "Close";
            exitButton.UseVisualStyleBackColor = false;
            exitButton.Click += exitButton_Click;
            // 
            // setPathButton
            // 
            setPathButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            setPathButton.BackColor = System.Drawing.Color.FromArgb(51, 51, 51);
            setPathButton.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            setPathButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            setPathButton.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            setPathButton.ForeColor = System.Drawing.Color.FromArgb(225, 225, 225);
            setPathButton.Location = new System.Drawing.Point(421, 73);
            setPathButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            setPathButton.MinimumSize = new System.Drawing.Size(0, 30);
            setPathButton.Name = "setPathButton";
            setPathButton.Size = new System.Drawing.Size(47, 30);
            setPathButton.TabIndex = 3;
            setPathButton.Text = "Set";
            setPathButton.UseVisualStyleBackColor = false;
            setPathButton.Click += UpdatePathing;
            // 
            // localPathBox
            // 
            localPathBox.Location = new System.Drawing.Point(178, 110);
            localPathBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            localPathBox.Name = "localPathBox";
            localPathBox.Size = new System.Drawing.Size(235, 23);
            localPathBox.TabIndex = 4;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            label6.Location = new System.Drawing.Point(58, 111);
            label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(97, 15);
            label6.TabIndex = 12;
            label6.Text = "Local Data Path:";
            // 
            // setPathButton2
            // 
            setPathButton2.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            setPathButton2.BackColor = System.Drawing.Color.FromArgb(51, 51, 51);
            setPathButton2.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(255, 231, 149);
            setPathButton2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            setPathButton2.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            setPathButton2.ForeColor = System.Drawing.Color.FromArgb(225, 225, 225);
            setPathButton2.Location = new System.Drawing.Point(421, 106);
            setPathButton2.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            setPathButton2.MinimumSize = new System.Drawing.Size(0, 30);
            setPathButton2.Name = "setPathButton2";
            setPathButton2.Size = new System.Drawing.Size(47, 30);
            setPathButton2.TabIndex = 5;
            setPathButton2.Text = "Set";
            setPathButton2.UseVisualStyleBackColor = false;
            setPathButton2.Click += UpdatePathing;
            // 
            // versionLabel
            // 
            versionLabel.AutoSize = true;
            versionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            versionLabel.Location = new System.Drawing.Point(15, 10);
            versionLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            versionLabel.Name = "versionLabel";
            versionLabel.Size = new System.Drawing.Size(58, 15);
            versionLabel.TabIndex = 14;
            versionLabel.Text = "1.17.89.0";
            // 
            // refuseMismatchedConnectionsCheckbox
            // 
            refuseMismatchedConnectionsCheckbox.AutoSize = true;
            refuseMismatchedConnectionsCheckbox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            refuseMismatchedConnectionsCheckbox.Location = new System.Drawing.Point(178, 202);
            refuseMismatchedConnectionsCheckbox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            refuseMismatchedConnectionsCheckbox.Name = "refuseMismatchedConnectionsCheckbox";
            refuseMismatchedConnectionsCheckbox.Size = new System.Drawing.Size(205, 19);
            refuseMismatchedConnectionsCheckbox.TabIndex = 8;
            refuseMismatchedConnectionsCheckbox.Text = "Refuse mismatched connections";
            refuseMismatchedConnectionsCheckbox.UseVisualStyleBackColor = true;
            refuseMismatchedConnectionsCheckbox.CheckedChanged += refuseMismatchedConnectionsCheckbox_CheckedChanged;
            // 
            // aboutButton
            // 
            aboutButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            aboutButton.BackColor = System.Drawing.Color.FromArgb(51, 51, 51);
            aboutButton.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(255, 231, 149);
            aboutButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            aboutButton.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            aboutButton.ForeColor = System.Drawing.Color.FromArgb(225, 225, 225);
            aboutButton.Location = new System.Drawing.Point(380, 243);
            aboutButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            aboutButton.MinimumSize = new System.Drawing.Size(0, 30);
            aboutButton.Name = "aboutButton";
            aboutButton.Size = new System.Drawing.Size(88, 30);
            aboutButton.TabIndex = 10;
            aboutButton.Text = "About...";
            aboutButton.UseVisualStyleBackColor = false;
            aboutButton.Click += aboutButton_Click;
            // 
            // ue4ssButton
            // 
            ue4ssButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            ue4ssButton.BackColor = System.Drawing.Color.FromArgb(51, 51, 51);
            ue4ssButton.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(255, 231, 149);
            ue4ssButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            ue4ssButton.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            ue4ssButton.ForeColor = System.Drawing.Color.FromArgb(225, 225, 225);
            ue4ssButton.Location = new System.Drawing.Point(244, 243);
            ue4ssButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            ue4ssButton.MinimumSize = new System.Drawing.Size(0, 30);
            ue4ssButton.Name = "ue4ssButton";
            ue4ssButton.Size = new System.Drawing.Size(128, 30);
            ue4ssButton.TabIndex = 15;
            ue4ssButton.Text = "Uninstall UE4SS...";
            ue4ssButton.UseVisualStyleBackColor = false;
            ue4ssButton.Click += ue4ssButton_Click;
            // 
            // SettingsForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.FromArgb(40, 42, 45);
            ClientSize = new System.Drawing.Size(482, 287);
            Controls.Add(ue4ssButton);
            Controls.Add(aboutButton);
            Controls.Add(refuseMismatchedConnectionsCheckbox);
            Controls.Add(versionLabel);
            Controls.Add(setPathButton2);
            Controls.Add(label6);
            Controls.Add(localPathBox);
            Controls.Add(platformComboBox);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(themeComboBox);
            Controls.Add(accentComboBox);
            Controls.Add(exitButton);
            Controls.Add(setPathButton);
            Controls.Add(label3);
            Controls.Add(gamePathBox);
            Controls.Add(label2);
            Controls.Add(label1);
            ForeColor = System.Drawing.Color.FromArgb(225, 225, 225);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            MaximizeBox = false;
            Name = "SettingsForm";
            Text = "Settings";
            Load += SettingsForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox gamePathBox;
        private System.Windows.Forms.Label label3;
        private CoolButton setPathButton;
        private CoolButton exitButton;
        private System.Windows.Forms.ComboBox accentComboBox;
        private System.Windows.Forms.ComboBox themeComboBox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox platformComboBox;
        private System.Windows.Forms.TextBox localPathBox;
        private System.Windows.Forms.Label label6;
        private CoolButton setPathButton2;
        private System.Windows.Forms.Label versionLabel;
        private System.Windows.Forms.CheckBox refuseMismatchedConnectionsCheckbox;
        private CoolButton aboutButton;
        private CoolButton ue4ssButton;
    }
}