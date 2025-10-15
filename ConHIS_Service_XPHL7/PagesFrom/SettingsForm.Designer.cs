namespace ConHIS_Service_XPHL7.PagesFrom
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
            this.titleLabel = new System.Windows.Forms.Label();
            this.descriptionLabel = new System.Windows.Forms.Label();
            this.logRetentionDaysNumeric = new System.Windows.Forms.NumericUpDown();
            this.saveButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.infoPanel = new System.Windows.Forms.Panel();
            this.infoLabel = new System.Windows.Forms.Label();
            this.daysLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.logRetentionDaysNumeric)).BeginInit();
            this.infoPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // titleLabel
            // 
            this.titleLabel.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.titleLabel.Location = new System.Drawing.Point(20, 20);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(360, 25);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "⚙️ ตั้งค่าระบบ";
            // 
            // descriptionLabel
            // 
            this.descriptionLabel.Font = new System.Drawing.Font("Tahoma", 9F);
            this.descriptionLabel.ForeColor = System.Drawing.Color.DimGray;
            this.descriptionLabel.Location = new System.Drawing.Point(20, 60);
            this.descriptionLabel.Name = "descriptionLabel";
            this.descriptionLabel.Size = new System.Drawing.Size(360, 40);
            this.descriptionLabel.TabIndex = 1;
            this.descriptionLabel.Text = "จำนวนวันที่ต้องการเก็บไฟล์ Log:\r\n(ไฟล์ที่เก่ากว่านี้จะถูกลบอัตโนมัติ)";
            // 
            // logRetentionDaysNumeric
            // 
            this.logRetentionDaysNumeric.Font = new System.Drawing.Font("Tahoma", 10F);
            this.logRetentionDaysNumeric.Location = new System.Drawing.Point(20, 110);
            this.logRetentionDaysNumeric.Maximum = new decimal(new int[] {
            365,
            0,
            0,
            0});
            this.logRetentionDaysNumeric.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.logRetentionDaysNumeric.Name = "logRetentionDaysNumeric";
            this.logRetentionDaysNumeric.Size = new System.Drawing.Size(120, 24);
            this.logRetentionDaysNumeric.TabIndex = 2;
            this.logRetentionDaysNumeric.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.logRetentionDaysNumeric.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            // 
            // daysLabel
            // 
            this.daysLabel.AutoSize = true;
            this.daysLabel.Font = new System.Drawing.Font("Tahoma", 9F);
            this.daysLabel.ForeColor = System.Drawing.Color.DimGray;
            this.daysLabel.Location = new System.Drawing.Point(150, 114);
            this.daysLabel.Name = "daysLabel";
            this.daysLabel.Size = new System.Drawing.Size(25, 14);
            this.daysLabel.TabIndex = 3;
            this.daysLabel.Text = "วัน";
            // 
            // infoPanel
            // 
            this.infoPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(250)))), ((int)(((byte)(205)))));
            this.infoPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.infoPanel.Controls.Add(this.infoLabel);
            this.infoPanel.Location = new System.Drawing.Point(20, 150);
            this.infoPanel.Name = "infoPanel";
            this.infoPanel.Size = new System.Drawing.Size(360, 80);
            this.infoPanel.TabIndex = 4;
            // 
            // infoLabel
            // 
            this.infoLabel.Font = new System.Drawing.Font("Tahoma", 8F);
            this.infoLabel.ForeColor = System.Drawing.Color.DarkOrange;
            this.infoLabel.Location = new System.Drawing.Point(10, 10);
            this.infoLabel.Name = "infoLabel";
            this.infoLabel.Size = new System.Drawing.Size(340, 60);
            this.infoLabel.TabIndex = 0;
            this.infoLabel.Text = "ℹ️ หมายเหตุ:\r\n• ระบบจะลบไฟล์ log ที่เก่ากว่าจำนวนวันที่กำหนด\r\n• การเปลี่ยนแปลงจะ" +
    "มีผลทันทีหลังจากกด Save\r\n• แนะนำให้เก็บ log อย่างน้อย 7-30 วัน";
            // 
            // saveButton
            // 
            this.saveButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(130)))), ((int)(((byte)(180)))));
            this.saveButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.saveButton.FlatAppearance.BorderSize = 0;
            this.saveButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.saveButton.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.saveButton.ForeColor = System.Drawing.Color.White;
            this.saveButton.Location = new System.Drawing.Point(180, 250);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(100, 35);
            this.saveButton.TabIndex = 5;
            this.saveButton.Text = "💾 Save";
            this.saveButton.UseVisualStyleBackColor = false;
            this.saveButton.Click += new System.EventHandler(this.SaveButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.BackColor = System.Drawing.Color.LightGray;
            this.cancelButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.cancelButton.FlatAppearance.BorderSize = 0;
            this.cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cancelButton.Font = new System.Drawing.Font("Tahoma", 9F);
            this.cancelButton.Location = new System.Drawing.Point(290, 250);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(90, 35);
            this.cancelButton.TabIndex = 6;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = false;
            this.cancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(400, 310);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.infoPanel);
            this.Controls.Add(this.daysLabel);
            this.Controls.Add(this.logRetentionDaysNumeric);
            this.Controls.Add(this.descriptionLabel);
            this.Controls.Add(this.titleLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Settings - Log Configuration";
            ((System.ComponentModel.ISupportInitialize)(this.logRetentionDaysNumeric)).EndInit();
            this.infoPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label descriptionLabel;
        private System.Windows.Forms.NumericUpDown logRetentionDaysNumeric;
        private System.Windows.Forms.Label daysLabel;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Panel infoPanel;
        private System.Windows.Forms.Label infoLabel;
    }
}