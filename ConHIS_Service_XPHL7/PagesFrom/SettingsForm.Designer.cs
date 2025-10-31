namespace ConHIS_Service_XPHL7.PagesFrom
{
    partial class SettingsForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabDatabase = new System.Windows.Forms.TabPage();
            this.tabAPI = new System.Windows.Forms.TabPage();
            this.tabLog = new System.Windows.Forms.TabPage();

            // Database Tab Controls
            this.grpDatabase = new System.Windows.Forms.GroupBox();
            this.lblServer = new System.Windows.Forms.Label();
            this.txtServer = new System.Windows.Forms.TextBox();
            this.lblDatabase = new System.Windows.Forms.Label();
            this.txtDatabase = new System.Windows.Forms.TextBox();
            this.lblUserId = new System.Windows.Forms.Label();
            this.txtUserId = new System.Windows.Forms.TextBox();
            this.lblPassword = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();

            // API Tab Controls
            this.grpAPI = new System.Windows.Forms.GroupBox();
            this.lblApiEndpoint = new System.Windows.Forms.Label();
            this.txtApiEndpoint = new System.Windows.Forms.TextBox();
            this.lblApiTimeout = new System.Windows.Forms.Label();
            this.numApiTimeout = new System.Windows.Forms.NumericUpDown();
            this.lblApiRetry = new System.Windows.Forms.Label();
            this.numApiRetry = new System.Windows.Forms.NumericUpDown();
            this.lblApiRetryDelay = new System.Windows.Forms.Label();
            this.numApiRetryDelay = new System.Windows.Forms.NumericUpDown();

            // Log Tab Controls
            this.grpLog = new System.Windows.Forms.GroupBox();
            this.lblLogRetention = new System.Windows.Forms.Label();
            this.numLogRetention = new System.Windows.Forms.NumericUpDown();
            this.lblDays = new System.Windows.Forms.Label();
            this.pnlLogInfo = new System.Windows.Forms.Panel();
            this.lblLogInfo = new System.Windows.Forms.Label();

            // Bottom Buttons
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();

            this.tabControl.SuspendLayout();
            this.tabDatabase.SuspendLayout();
            this.tabAPI.SuspendLayout();
            this.tabLog.SuspendLayout();
            this.grpDatabase.SuspendLayout();
            this.grpAPI.SuspendLayout();
            this.grpLog.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numApiTimeout)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numApiRetry)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numApiRetryDelay)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLogRetention)).BeginInit();
            this.pnlLogInfo.SuspendLayout();
            this.SuspendLayout();

            // tabControl
            this.tabControl.Controls.Add(this.tabDatabase);
            this.tabControl.Controls.Add(this.tabAPI);
            this.tabControl.Controls.Add(this.tabLog);
            this.tabControl.Font = new System.Drawing.Font("Tahoma", 9F);
            this.tabControl.Location = new System.Drawing.Point(12, 12);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(660, 350);
            this.tabControl.TabIndex = 0;

            // tabDatabase
            this.tabDatabase.Controls.Add(this.grpDatabase);
            this.tabDatabase.Location = new System.Drawing.Point(4, 23);
            this.tabDatabase.Name = "tabDatabase";
            this.tabDatabase.Padding = new System.Windows.Forms.Padding(3);
            this.tabDatabase.Size = new System.Drawing.Size(652, 323);
            this.tabDatabase.TabIndex = 0;
            this.tabDatabase.Text = "🗄️ Database Connection";
            this.tabDatabase.UseVisualStyleBackColor = true;

            // grpDatabase
            this.grpDatabase.Controls.Add(this.lblServer);
            this.grpDatabase.Controls.Add(this.txtServer);
            this.grpDatabase.Controls.Add(this.lblDatabase);
            this.grpDatabase.Controls.Add(this.txtDatabase);
            this.grpDatabase.Controls.Add(this.lblUserId);
            this.grpDatabase.Controls.Add(this.txtUserId);
            this.grpDatabase.Controls.Add(this.lblPassword);
            this.grpDatabase.Controls.Add(this.txtPassword);
            this.grpDatabase.Font = new System.Drawing.Font("Tahoma", 9F);
            this.grpDatabase.Location = new System.Drawing.Point(20, 20);
            this.grpDatabase.Name = "grpDatabase";
            this.grpDatabase.Size = new System.Drawing.Size(610, 280);
            this.grpDatabase.TabIndex = 0;
            this.grpDatabase.TabStop = false;
            this.grpDatabase.Text = "การตั้งค่าฐานข้อมูล (connectdatabase.ini)";

            // lblServer
            this.lblServer.AutoSize = true;
            this.lblServer.Location = new System.Drawing.Point(30, 50);
            this.lblServer.Name = "lblServer";
            this.lblServer.Size = new System.Drawing.Size(42, 14);
            this.lblServer.TabIndex = 0;
            this.lblServer.Text = "Server:";

            // txtServer
            this.txtServer.Font = new System.Drawing.Font("Tahoma", 10F);
            this.txtServer.Location = new System.Drawing.Point(150, 47);
            this.txtServer.Name = "txtServer";
            this.txtServer.Size = new System.Drawing.Size(430, 24);
            this.txtServer.TabIndex = 1;

            // lblDatabase
            this.lblDatabase.AutoSize = true;
            this.lblDatabase.Location = new System.Drawing.Point(30, 100);
            this.lblDatabase.Name = "lblDatabase";
            this.lblDatabase.Size = new System.Drawing.Size(60, 14);
            this.lblDatabase.TabIndex = 2;
            this.lblDatabase.Text = "Database:";

            // txtDatabase
            this.txtDatabase.Font = new System.Drawing.Font("Tahoma", 10F);
            this.txtDatabase.Location = new System.Drawing.Point(150, 97);
            this.txtDatabase.Name = "txtDatabase";
            this.txtDatabase.Size = new System.Drawing.Size(430, 24);
            this.txtDatabase.TabIndex = 3;

            // lblUserId
            this.lblUserId.AutoSize = true;
            this.lblUserId.Location = new System.Drawing.Point(30, 150);
            this.lblUserId.Name = "lblUserId";
            this.lblUserId.Size = new System.Drawing.Size(47, 14);
            this.lblUserId.TabIndex = 4;
            this.lblUserId.Text = "User ID:";

            // txtUserId
            this.txtUserId.Font = new System.Drawing.Font("Tahoma", 10F);
            this.txtUserId.Location = new System.Drawing.Point(150, 147);
            this.txtUserId.Name = "txtUserId";
            this.txtUserId.Size = new System.Drawing.Size(430, 24);
            this.txtUserId.TabIndex = 5;

            // lblPassword
            this.lblPassword.AutoSize = true;
            this.lblPassword.Location = new System.Drawing.Point(30, 200);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(61, 14);
            this.lblPassword.TabIndex = 6;
            this.lblPassword.Text = "Password:";

            // txtPassword
            this.txtPassword.Font = new System.Drawing.Font("Tahoma", 10F);
            this.txtPassword.Location = new System.Drawing.Point(150, 197);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(430, 24);
            this.txtPassword.TabIndex = 7;
            this.txtPassword.UseSystemPasswordChar = true;

            // tabAPI
            this.tabAPI.Controls.Add(this.grpAPI);
            this.tabAPI.Location = new System.Drawing.Point(4, 23);
            this.tabAPI.Name = "tabAPI";
            this.tabAPI.Padding = new System.Windows.Forms.Padding(3);
            this.tabAPI.Size = new System.Drawing.Size(652, 323);
            this.tabAPI.TabIndex = 1;
            this.tabAPI.Text = "🌐 API Settings";
            this.tabAPI.UseVisualStyleBackColor = true;

            // grpAPI
            this.grpAPI.Controls.Add(this.lblApiEndpoint);
            this.grpAPI.Controls.Add(this.txtApiEndpoint);
            this.grpAPI.Controls.Add(this.lblApiTimeout);
            this.grpAPI.Controls.Add(this.numApiTimeout);
            this.grpAPI.Controls.Add(this.lblApiRetry);
            this.grpAPI.Controls.Add(this.numApiRetry);
            this.grpAPI.Controls.Add(this.lblApiRetryDelay);
            this.grpAPI.Controls.Add(this.numApiRetryDelay);
            this.grpAPI.Font = new System.Drawing.Font("Tahoma", 9F);
            this.grpAPI.Location = new System.Drawing.Point(20, 20);
            this.grpAPI.Name = "grpAPI";
            this.grpAPI.Size = new System.Drawing.Size(610, 280);
            this.grpAPI.TabIndex = 0;
            this.grpAPI.TabStop = false;
            this.grpAPI.Text = "การตั้งค่า API (appsettings.ini)";

            // lblApiEndpoint
            this.lblApiEndpoint.AutoSize = true;
            this.lblApiEndpoint.Location = new System.Drawing.Point(30, 40);
            this.lblApiEndpoint.Name = "lblApiEndpoint";
            this.lblApiEndpoint.Size = new System.Drawing.Size(76, 14);
            this.lblApiEndpoint.TabIndex = 0;
            this.lblApiEndpoint.Text = "API Endpoint:";

            // txtApiEndpoint
            this.txtApiEndpoint.Font = new System.Drawing.Font("Tahoma", 10F);
            this.txtApiEndpoint.Location = new System.Drawing.Point(30, 60);
            this.txtApiEndpoint.Name = "txtApiEndpoint";
            this.txtApiEndpoint.Size = new System.Drawing.Size(550, 24);
            this.txtApiEndpoint.TabIndex = 1;
            this.txtApiEndpoint.Text = "https://localhost:8080/api/conHIS/insertPrescription";

            // lblApiTimeout
            this.lblApiTimeout.AutoSize = true;
            this.lblApiTimeout.Location = new System.Drawing.Point(30, 110);
            this.lblApiTimeout.Name = "lblApiTimeout";
            this.lblApiTimeout.Size = new System.Drawing.Size(124, 14);
            this.lblApiTimeout.TabIndex = 2;
            this.lblApiTimeout.Text = "API Timeout (seconds):";

            // numApiTimeout
            this.numApiTimeout.Font = new System.Drawing.Font("Tahoma", 10F);
            this.numApiTimeout.Location = new System.Drawing.Point(200, 108);
            this.numApiTimeout.Maximum = new decimal(new int[] { 300, 0, 0, 0 });
            this.numApiTimeout.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
            this.numApiTimeout.Name = "numApiTimeout";
            this.numApiTimeout.Size = new System.Drawing.Size(100, 24);
            this.numApiTimeout.TabIndex = 3;
            this.numApiTimeout.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numApiTimeout.Value = new decimal(new int[] { 30, 0, 0, 0 });

            // lblApiRetry
            this.lblApiRetry.AutoSize = true;
            this.lblApiRetry.Location = new System.Drawing.Point(30, 160);
            this.lblApiRetry.Name = "lblApiRetry";
            this.lblApiRetry.Size = new System.Drawing.Size(84, 14);
            this.lblApiRetry.TabIndex = 4;
            this.lblApiRetry.Text = "Retry Attempts:";

            // numApiRetry
            this.numApiRetry.Font = new System.Drawing.Font("Tahoma", 10F);
            this.numApiRetry.Location = new System.Drawing.Point(200, 158);
            this.numApiRetry.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            this.numApiRetry.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.numApiRetry.Name = "numApiRetry";
            this.numApiRetry.Size = new System.Drawing.Size(100, 24);
            this.numApiRetry.TabIndex = 5;
            this.numApiRetry.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numApiRetry.Value = new decimal(new int[] { 3, 0, 0, 0 });

            // lblApiRetryDelay
            this.lblApiRetryDelay.AutoSize = true;
            this.lblApiRetryDelay.Location = new System.Drawing.Point(30, 210);
            this.lblApiRetryDelay.Name = "lblApiRetryDelay";
            this.lblApiRetryDelay.Size = new System.Drawing.Size(137, 14);
            this.lblApiRetryDelay.TabIndex = 6;
            this.lblApiRetryDelay.Text = "Retry Delay (seconds):";

            // numApiRetryDelay
            this.numApiRetryDelay.Font = new System.Drawing.Font("Tahoma", 10F);
            this.numApiRetryDelay.Location = new System.Drawing.Point(200, 208);
            this.numApiRetryDelay.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
            this.numApiRetryDelay.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numApiRetryDelay.Name = "numApiRetryDelay";
            this.numApiRetryDelay.Size = new System.Drawing.Size(100, 24);
            this.numApiRetryDelay.TabIndex = 7;
            this.numApiRetryDelay.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numApiRetryDelay.Value = new decimal(new int[] { 5, 0, 0, 0 });

            // tabLog
            this.tabLog.Controls.Add(this.grpLog);
            this.tabLog.Location = new System.Drawing.Point(4, 23);
            this.tabLog.Name = "tabLog";
            this.tabLog.Padding = new System.Windows.Forms.Padding(3);
            this.tabLog.Size = new System.Drawing.Size(652, 323);
            this.tabLog.TabIndex = 2;
            this.tabLog.Text = "📝 Log Settings";
            this.tabLog.UseVisualStyleBackColor = true;

            // grpLog
            this.grpLog.Controls.Add(this.lblLogRetention);
            this.grpLog.Controls.Add(this.numLogRetention);
            this.grpLog.Controls.Add(this.lblDays);
            this.grpLog.Controls.Add(this.pnlLogInfo);
            this.grpLog.Font = new System.Drawing.Font("Tahoma", 9F);
            this.grpLog.Location = new System.Drawing.Point(20, 20);
            this.grpLog.Name = "grpLog";
            this.grpLog.Size = new System.Drawing.Size(610, 280);
            this.grpLog.TabIndex = 0;
            this.grpLog.TabStop = false;
            this.grpLog.Text = "การตั้งค่า Log (App.config)";

            // lblLogRetention
            this.lblLogRetention.Location = new System.Drawing.Point(30, 40);
            this.lblLogRetention.Name = "lblLogRetention";
            this.lblLogRetention.Size = new System.Drawing.Size(550, 40);
            this.lblLogRetention.TabIndex = 0;
            this.lblLogRetention.Text = "จำนวนวันที่ต้องการเก็บไฟล์ Log:\r\n(ไฟล์ที่เก่ากว่านี้จะถูกลบอัตโนมัติ)";

            // numLogRetention
            this.numLogRetention.Font = new System.Drawing.Font("Tahoma", 10F);
            this.numLogRetention.Location = new System.Drawing.Point(30, 90);
            this.numLogRetention.Maximum = new decimal(new int[] { 365, 0, 0, 0 });
            this.numLogRetention.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numLogRetention.Name = "numLogRetention";
            this.numLogRetention.Size = new System.Drawing.Size(120, 24);
            this.numLogRetention.TabIndex = 1;
            this.numLogRetention.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numLogRetention.Value = new decimal(new int[] { 30, 0, 0, 0 });

            // lblDays
            this.lblDays.AutoSize = true;
            this.lblDays.ForeColor = System.Drawing.Color.DimGray;
            this.lblDays.Location = new System.Drawing.Point(160, 94);
            this.lblDays.Name = "lblDays";
            this.lblDays.Size = new System.Drawing.Size(25, 14);
            this.lblDays.TabIndex = 2;
            this.lblDays.Text = "วัน";

            // pnlLogInfo
            this.pnlLogInfo.BackColor = System.Drawing.Color.FromArgb(255, 250, 205);
            this.pnlLogInfo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlLogInfo.Controls.Add(this.lblLogInfo);
            this.pnlLogInfo.Location = new System.Drawing.Point(30, 140);
            this.pnlLogInfo.Name = "pnlLogInfo";
            this.pnlLogInfo.Size = new System.Drawing.Size(550, 110);
            this.pnlLogInfo.TabIndex = 3;

            // lblLogInfo
            this.lblLogInfo.Font = new System.Drawing.Font("Tahoma", 8F);
            this.lblLogInfo.ForeColor = System.Drawing.Color.DarkOrange;
            this.lblLogInfo.Location = new System.Drawing.Point(10, 10);
            this.lblLogInfo.Name = "lblLogInfo";
            this.lblLogInfo.Size = new System.Drawing.Size(530, 90);
            this.lblLogInfo.TabIndex = 0;
            this.lblLogInfo.Text = @"ℹ️ หมายเหตุ:
• ระบบจะลบไฟล์ log ที่เก่ากว่าจำนวนวันที่กำหนด
• การเปลี่ยนแปลงจะมีผลทันทีหลังจากกด Save
• แนะนำให้เก็บ log อย่างน้อย 7-30 วัน

📁 ไฟล์ที่จะถูกแก้ไข:
   • Connection\connectdatabase.ini (Database)
   • Config\appsettings.ini (API)
   • App.config (Log Settings)";

            // btnSave
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(34, 139, 34);
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.FlatAppearance.BorderSize = 0;
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("Tahoma", 10F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(440, 375);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(110, 40);
            this.btnSave.TabIndex = 1;
            this.btnSave.Text = "💾 Save";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.BtnSave_Click);

            // btnCancel
            this.btnCancel.BackColor = System.Drawing.Color.LightGray;
            this.btnCancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Tahoma", 10F);
            this.btnCancel.Location = new System.Drawing.Point(560, 375);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(110, 40);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);

            // SettingsForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(684, 430);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.tabControl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "⚙️ System Settings - Configuration Files";
            this.tabControl.ResumeLayout(false);
            this.tabDatabase.ResumeLayout(false);
            this.tabAPI.ResumeLayout(false);
            this.tabLog.ResumeLayout(false);
            this.grpDatabase.ResumeLayout(false);
            this.grpDatabase.PerformLayout();
            this.grpAPI.ResumeLayout(false);
            this.grpAPI.PerformLayout();
            this.grpLog.ResumeLayout(false);
            this.grpLog.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numApiTimeout)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numApiRetry)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numApiRetryDelay)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLogRetention)).EndInit();
            this.pnlLogInfo.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabDatabase;
        private System.Windows.Forms.TabPage tabAPI;
        private System.Windows.Forms.TabPage tabLog;

        // Database Controls
        private System.Windows.Forms.GroupBox grpDatabase;
        private System.Windows.Forms.Label lblServer;
        private System.Windows.Forms.TextBox txtServer;
        private System.Windows.Forms.Label lblDatabase;
        private System.Windows.Forms.TextBox txtDatabase;
        private System.Windows.Forms.Label lblUserId;
        private System.Windows.Forms.TextBox txtUserId;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.TextBox txtPassword;

        // API Controls
        private System.Windows.Forms.GroupBox grpAPI;
        private System.Windows.Forms.Label lblApiEndpoint;
        private System.Windows.Forms.TextBox txtApiEndpoint;
        private System.Windows.Forms.Label lblApiTimeout;
        private System.Windows.Forms.NumericUpDown numApiTimeout;
        private System.Windows.Forms.Label lblApiRetry;
        private System.Windows.Forms.NumericUpDown numApiRetry;
        private System.Windows.Forms.Label lblApiRetryDelay;
        private System.Windows.Forms.NumericUpDown numApiRetryDelay;

        // Log Controls
        private System.Windows.Forms.GroupBox grpLog;
        private System.Windows.Forms.Label lblLogRetention;
        private System.Windows.Forms.NumericUpDown numLogRetention;
        private System.Windows.Forms.Label lblDays;
        private System.Windows.Forms.Panel pnlLogInfo;
        private System.Windows.Forms.Label lblLogInfo;

        // Bottom Buttons
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
    }
}