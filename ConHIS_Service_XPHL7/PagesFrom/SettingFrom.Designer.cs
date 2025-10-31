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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabDatabase = new System.Windows.Forms.TabPage();
            this.grpDatabase = new System.Windows.Forms.GroupBox();
            this.pnlDbInfo = new System.Windows.Forms.Panel();
            this.lblDbInfo = new System.Windows.Forms.Label();
            this.lblConnectionStatus = new System.Windows.Forms.Label();
            this.btnTestConnection = new System.Windows.Forms.Button();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.lblPassword = new System.Windows.Forms.Label();
            this.txtUserId = new System.Windows.Forms.TextBox();
            this.lblUserId = new System.Windows.Forms.Label();
            this.txtDatabase = new System.Windows.Forms.TextBox();
            this.lblDatabase = new System.Windows.Forms.Label();
            this.txtServer = new System.Windows.Forms.TextBox();
            this.lblServer = new System.Windows.Forms.Label();
            this.tabAPI = new System.Windows.Forms.TabPage();
            this.grpAPI = new System.Windows.Forms.GroupBox();
            this.pnlApiInfo = new System.Windows.Forms.Panel();
            this.lblApiInfo = new System.Windows.Forms.Label();
            this.lblApiRetryDelayUnit = new System.Windows.Forms.Label();
            this.numApiRetryDelay = new System.Windows.Forms.NumericUpDown();
            this.lblApiRetryDelay = new System.Windows.Forms.Label();
            this.lblApiRetryUnit = new System.Windows.Forms.Label();
            this.numApiRetry = new System.Windows.Forms.NumericUpDown();
            this.lblApiRetry = new System.Windows.Forms.Label();
            this.lblApiTimeoutUnit = new System.Windows.Forms.Label();
            this.numApiTimeout = new System.Windows.Forms.NumericUpDown();
            this.lblApiTimeout = new System.Windows.Forms.Label();
            this.lblApiStatus = new System.Windows.Forms.Label();
            this.btnTestApi = new System.Windows.Forms.Button();
            this.txtApiEndpoint = new System.Windows.Forms.TextBox();
            this.lblApiEndpoint = new System.Windows.Forms.Label();
            this.tabLog = new System.Windows.Forms.TabPage();
            this.grpLog = new System.Windows.Forms.GroupBox();
            this.pnlLogInfo = new System.Windows.Forms.Panel();
            this.lblLogInfo = new System.Windows.Forms.Label();
            this.lblDays = new System.Windows.Forms.Label();
            this.numLogRetention = new System.Windows.Forms.NumericUpDown();
            this.lblLogRetention = new System.Windows.Forms.Label();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.tabControl.SuspendLayout();
            this.tabDatabase.SuspendLayout();
            this.grpDatabase.SuspendLayout();
            this.pnlDbInfo.SuspendLayout();
            this.tabAPI.SuspendLayout();
            this.grpAPI.SuspendLayout();
            this.pnlApiInfo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numApiRetryDelay)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numApiRetry)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numApiTimeout)).BeginInit();
            this.tabLog.SuspendLayout();
            this.grpLog.SuspendLayout();
            this.pnlLogInfo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numLogRetention)).BeginInit();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.tabDatabase);
            this.tabControl.Controls.Add(this.tabAPI);
            this.tabControl.Controls.Add(this.tabLog);
            this.tabControl.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabControl.Location = new System.Drawing.Point(12, 12);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(760, 450);
            this.tabControl.TabIndex = 0;
            // 
            // tabDatabase
            // 
            this.tabDatabase.BackColor = System.Drawing.Color.White;
            this.tabDatabase.Controls.Add(this.grpDatabase);
            this.tabDatabase.Location = new System.Drawing.Point(4, 25);
            this.tabDatabase.Name = "tabDatabase";
            this.tabDatabase.Padding = new System.Windows.Forms.Padding(3);
            this.tabDatabase.Size = new System.Drawing.Size(752, 421);
            this.tabDatabase.TabIndex = 0;
            this.tabDatabase.Text = "🗄️ Database";
            // 
            // grpDatabase
            // 
            this.grpDatabase.Controls.Add(this.pnlDbInfo);
            this.grpDatabase.Controls.Add(this.lblConnectionStatus);
            this.grpDatabase.Controls.Add(this.btnTestConnection);
            this.grpDatabase.Controls.Add(this.txtPassword);
            this.grpDatabase.Controls.Add(this.lblPassword);
            this.grpDatabase.Controls.Add(this.txtUserId);
            this.grpDatabase.Controls.Add(this.lblUserId);
            this.grpDatabase.Controls.Add(this.txtDatabase);
            this.grpDatabase.Controls.Add(this.lblDatabase);
            this.grpDatabase.Controls.Add(this.txtServer);
            this.grpDatabase.Controls.Add(this.lblServer);
            this.grpDatabase.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.grpDatabase.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.grpDatabase.Location = new System.Drawing.Point(15, 15);
            this.grpDatabase.Name = "grpDatabase";
            this.grpDatabase.Size = new System.Drawing.Size(720, 390);
            this.grpDatabase.TabIndex = 0;
            this.grpDatabase.TabStop = false;
            this.grpDatabase.Text = "Database Connection (connectdatabase.ini)";
            // 
            // pnlDbInfo
            // 
            this.pnlDbInfo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(244)))), ((int)(((byte)(253)))));
            this.pnlDbInfo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlDbInfo.Controls.Add(this.lblDbInfo);
            this.pnlDbInfo.Location = new System.Drawing.Point(30, 250);
            this.pnlDbInfo.Name = "pnlDbInfo";
            this.pnlDbInfo.Size = new System.Drawing.Size(660, 120);
            this.pnlDbInfo.TabIndex = 10;
            // 
            // lblDbInfo
            // 
            this.lblDbInfo.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.lblDbInfo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(102)))), ((int)(((byte)(204)))));
            this.lblDbInfo.Location = new System.Drawing.Point(10, 10);
            this.lblDbInfo.Name = "lblDbInfo";
            this.lblDbInfo.Size = new System.Drawing.Size(640, 100);
            this.lblDbInfo.TabIndex = 0;
            this.lblDbInfo.Text = "ℹ️ หมายเหตุ:\r\n• ข้อมูลจะถูกบันทึกในไฟล์ Connection\\connectdatabase.ini\r\n• Passwor" +
    "d จะถูกเก็บในรูปแบบ Plain Text\r\n• กรุณาทดสอบการเชื่อมต่อก่อนบันทึก\r\n• การเปลี่ยน" +
    "แปลงจะมีผลหลังบันทึกและ Restart";
            // 
            // lblConnectionStatus
            // 
            this.lblConnectionStatus.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.lblConnectionStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblConnectionStatus.Location = new System.Drawing.Point(320, 200);
            this.lblConnectionStatus.Name = "lblConnectionStatus";
            this.lblConnectionStatus.Size = new System.Drawing.Size(370, 35);
            this.lblConnectionStatus.TabIndex = 9;
            this.lblConnectionStatus.Text = "ℹ️ Click Test Connection to verify";
            this.lblConnectionStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnTestConnection
            // 
            this.btnTestConnection.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnTestConnection.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTestConnection.FlatAppearance.BorderSize = 0;
            this.btnTestConnection.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTestConnection.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.btnTestConnection.ForeColor = System.Drawing.Color.White;
            this.btnTestConnection.Location = new System.Drawing.Point(150, 200);
            this.btnTestConnection.Name = "btnTestConnection";
            this.btnTestConnection.Size = new System.Drawing.Size(150, 35);
            this.btnTestConnection.TabIndex = 8;
            this.btnTestConnection.Text = "🔌 Test Connection";
            this.btnTestConnection.UseVisualStyleBackColor = false;
            this.btnTestConnection.Click += new System.EventHandler(this.BtnTestConnection_Click);
            // 
            // txtPassword
            // 
            this.txtPassword.Font = new System.Drawing.Font("Tahoma", 10F);
            this.txtPassword.Location = new System.Drawing.Point(150, 157);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(540, 24);
            this.txtPassword.TabIndex = 7;
            this.txtPassword.UseSystemPasswordChar = true;
            // 
            // lblPassword
            // 
            this.lblPassword.AutoSize = true;
            this.lblPassword.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblPassword.Location = new System.Drawing.Point(30, 160);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(62, 14);
            this.lblPassword.TabIndex = 6;
            this.lblPassword.Text = "Password:";
            // 
            // txtUserId
            // 
            this.txtUserId.Font = new System.Drawing.Font("Tahoma", 10F);
            this.txtUserId.Location = new System.Drawing.Point(150, 117);
            this.txtUserId.Name = "txtUserId";
            this.txtUserId.Size = new System.Drawing.Size(540, 24);
            this.txtUserId.TabIndex = 5;
            // 
            // lblUserId
            // 
            this.lblUserId.AutoSize = true;
            this.lblUserId.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblUserId.Location = new System.Drawing.Point(30, 120);
            this.lblUserId.Name = "lblUserId";
            this.lblUserId.Size = new System.Drawing.Size(51, 14);
            this.lblUserId.TabIndex = 4;
            this.lblUserId.Text = "User ID:";
            // 
            // txtDatabase
            // 
            this.txtDatabase.Font = new System.Drawing.Font("Tahoma", 10F);
            this.txtDatabase.Location = new System.Drawing.Point(150, 77);
            this.txtDatabase.Name = "txtDatabase";
            this.txtDatabase.Size = new System.Drawing.Size(540, 24);
            this.txtDatabase.TabIndex = 3;
            // 
            // lblDatabase
            // 
            this.lblDatabase.AutoSize = true;
            this.lblDatabase.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblDatabase.Location = new System.Drawing.Point(30, 80);
            this.lblDatabase.Name = "lblDatabase";
            this.lblDatabase.Size = new System.Drawing.Size(61, 14);
            this.lblDatabase.TabIndex = 2;
            this.lblDatabase.Text = "Database:";
            // 
            // txtServer
            // 
            this.txtServer.Font = new System.Drawing.Font("Tahoma", 10F);
            this.txtServer.Location = new System.Drawing.Point(150, 37);
            this.txtServer.Name = "txtServer";
            this.txtServer.Size = new System.Drawing.Size(540, 24);
            this.txtServer.TabIndex = 1;
            // 
            // lblServer
            // 
            this.lblServer.AutoSize = true;
            this.lblServer.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblServer.Location = new System.Drawing.Point(30, 40);
            this.lblServer.Name = "lblServer";
            this.lblServer.Size = new System.Drawing.Size(46, 14);
            this.lblServer.TabIndex = 0;
            this.lblServer.Text = "Server:";
            // 
            // tabAPI
            // 
            this.tabAPI.BackColor = System.Drawing.Color.White;
            this.tabAPI.Controls.Add(this.grpAPI);
            this.tabAPI.Location = new System.Drawing.Point(4, 25);
            this.tabAPI.Name = "tabAPI";
            this.tabAPI.Padding = new System.Windows.Forms.Padding(3);
            this.tabAPI.Size = new System.Drawing.Size(752, 421);
            this.tabAPI.TabIndex = 1;
            this.tabAPI.Text = "🌐 API";
            // 
            // grpAPI
            // 
            this.grpAPI.Controls.Add(this.pnlApiInfo);
            this.grpAPI.Controls.Add(this.lblApiRetryDelayUnit);
            this.grpAPI.Controls.Add(this.numApiRetryDelay);
            this.grpAPI.Controls.Add(this.lblApiRetryDelay);
            this.grpAPI.Controls.Add(this.lblApiRetryUnit);
            this.grpAPI.Controls.Add(this.numApiRetry);
            this.grpAPI.Controls.Add(this.lblApiRetry);
            this.grpAPI.Controls.Add(this.lblApiTimeoutUnit);
            this.grpAPI.Controls.Add(this.numApiTimeout);
            this.grpAPI.Controls.Add(this.lblApiTimeout);
            this.grpAPI.Controls.Add(this.lblApiStatus);
            this.grpAPI.Controls.Add(this.btnTestApi);
            this.grpAPI.Controls.Add(this.txtApiEndpoint);
            this.grpAPI.Controls.Add(this.lblApiEndpoint);
            this.grpAPI.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.grpAPI.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.grpAPI.Location = new System.Drawing.Point(15, 15);
            this.grpAPI.Name = "grpAPI";
            this.grpAPI.Size = new System.Drawing.Size(720, 390);
            this.grpAPI.TabIndex = 0;
            this.grpAPI.TabStop = false;
            this.grpAPI.Text = "API Settings (appsettings.ini)";
            // 
            // pnlApiInfo
            // 
            this.pnlApiInfo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(250)))), ((int)(((byte)(205)))));
            this.pnlApiInfo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlApiInfo.Controls.Add(this.lblApiInfo);
            this.pnlApiInfo.Location = new System.Drawing.Point(30, 270);
            this.pnlApiInfo.Name = "pnlApiInfo";
            this.pnlApiInfo.Size = new System.Drawing.Size(660, 100);
            this.pnlApiInfo.TabIndex = 13;
            // 
            // lblApiInfo
            // 
            this.lblApiInfo.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.lblApiInfo.ForeColor = System.Drawing.Color.DarkOrange;
            this.lblApiInfo.Location = new System.Drawing.Point(10, 10);
            this.lblApiInfo.Name = "lblApiInfo";
            this.lblApiInfo.Size = new System.Drawing.Size(640, 80);
            this.lblApiInfo.TabIndex = 0;
            this.lblApiInfo.Text = "ℹ️ คำแนะนำ:\r\n• Timeout: 30-60 วินาที (เหมาะสมสำหรับเครือข่ายทั่วไป)\r\n• Retry: 3 ค" +
    "รั้ง (จะลองส่งใหม่หากล้มเหลว)\r\n• Delay: 5 วินาที (ระยะเวลารอก่อนลองใหม่)";
            // 
            // lblApiRetryDelayUnit
            // 
            this.lblApiRetryDelayUnit.AutoSize = true;
            this.lblApiRetryDelayUnit.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.lblApiRetryDelayUnit.ForeColor = System.Drawing.Color.Gray;
            this.lblApiRetryDelayUnit.Location = new System.Drawing.Point(310, 227);
            this.lblApiRetryDelayUnit.Name = "lblApiRetryDelayUnit";
            this.lblApiRetryDelayUnit.Size = new System.Drawing.Size(32, 13);
            this.lblApiRetryDelayUnit.TabIndex = 12;
            this.lblApiRetryDelayUnit.Text = "วินาที";
            // 
            // numApiRetryDelay
            // 
            this.numApiRetryDelay.Font = new System.Drawing.Font("Tahoma", 10F);
            this.numApiRetryDelay.Location = new System.Drawing.Point(220, 222);
            this.numApiRetryDelay.Maximum = new decimal(new int[] {
            60,
            0,
            0,
            0});
            this.numApiRetryDelay.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numApiRetryDelay.Name = "numApiRetryDelay";
            this.numApiRetryDelay.Size = new System.Drawing.Size(80, 24);
            this.numApiRetryDelay.TabIndex = 11;
            this.numApiRetryDelay.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numApiRetryDelay.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // lblApiRetryDelay
            // 
            this.lblApiRetryDelay.AutoSize = true;
            this.lblApiRetryDelay.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblApiRetryDelay.Location = new System.Drawing.Point(30, 225);
            this.lblApiRetryDelay.Name = "lblApiRetryDelay";
            this.lblApiRetryDelay.Size = new System.Drawing.Size(73, 14);
            this.lblApiRetryDelay.TabIndex = 10;
            this.lblApiRetryDelay.Text = "Retry Delay:";
            // 
            // lblApiRetryUnit
            // 
            this.lblApiRetryUnit.AutoSize = true;
            this.lblApiRetryUnit.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.lblApiRetryUnit.ForeColor = System.Drawing.Color.Gray;
            this.lblApiRetryUnit.Location = new System.Drawing.Point(310, 187);
            this.lblApiRetryUnit.Name = "lblApiRetryUnit";
            this.lblApiRetryUnit.Size = new System.Drawing.Size(24, 13);
            this.lblApiRetryUnit.TabIndex = 9;
            this.lblApiRetryUnit.Text = "ครั้ง";
            // 
            // numApiRetry
            // 
            this.numApiRetry.Font = new System.Drawing.Font("Tahoma", 10F);
            this.numApiRetry.Location = new System.Drawing.Point(220, 182);
            this.numApiRetry.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numApiRetry.Name = "numApiRetry";
            this.numApiRetry.Size = new System.Drawing.Size(80, 24);
            this.numApiRetry.TabIndex = 8;
            this.numApiRetry.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numApiRetry.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            // 
            // lblApiRetry
            // 
            this.lblApiRetry.AutoSize = true;
            this.lblApiRetry.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblApiRetry.Location = new System.Drawing.Point(30, 185);
            this.lblApiRetry.Name = "lblApiRetry";
            this.lblApiRetry.Size = new System.Drawing.Size(96, 14);
            this.lblApiRetry.TabIndex = 7;
            this.lblApiRetry.Text = "Retry Attempts:";
            // 
            // lblApiTimeoutUnit
            // 
            this.lblApiTimeoutUnit.AutoSize = true;
            this.lblApiTimeoutUnit.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.lblApiTimeoutUnit.ForeColor = System.Drawing.Color.Gray;
            this.lblApiTimeoutUnit.Location = new System.Drawing.Point(310, 147);
            this.lblApiTimeoutUnit.Name = "lblApiTimeoutUnit";
            this.lblApiTimeoutUnit.Size = new System.Drawing.Size(32, 13);
            this.lblApiTimeoutUnit.TabIndex = 6;
            this.lblApiTimeoutUnit.Text = "วินาที";
            // 
            // numApiTimeout
            // 
            this.numApiTimeout.Font = new System.Drawing.Font("Tahoma", 10F);
            this.numApiTimeout.Location = new System.Drawing.Point(220, 142);
            this.numApiTimeout.Maximum = new decimal(new int[] {
            300,
            0,
            0,
            0});
            this.numApiTimeout.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numApiTimeout.Name = "numApiTimeout";
            this.numApiTimeout.Size = new System.Drawing.Size(80, 24);
            this.numApiTimeout.TabIndex = 5;
            this.numApiTimeout.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numApiTimeout.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            // 
            // lblApiTimeout
            // 
            this.lblApiTimeout.AutoSize = true;
            this.lblApiTimeout.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblApiTimeout.Location = new System.Drawing.Point(30, 145);
            this.lblApiTimeout.Name = "lblApiTimeout";
            this.lblApiTimeout.Size = new System.Drawing.Size(80, 14);
            this.lblApiTimeout.TabIndex = 4;
            this.lblApiTimeout.Text = "API Timeout:";
            // 
            // lblApiStatus
            // 
            this.lblApiStatus.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.lblApiStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblApiStatus.Location = new System.Drawing.Point(160, 95);
            this.lblApiStatus.Name = "lblApiStatus";
            this.lblApiStatus.Size = new System.Drawing.Size(530, 30);
            this.lblApiStatus.TabIndex = 3;
            this.lblApiStatus.Text = "ℹ️ Click Test API to verify";
            this.lblApiStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnTestApi
            // 
            this.btnTestApi.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(204)))), ((int)(((byte)(113)))));
            this.btnTestApi.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTestApi.FlatAppearance.BorderSize = 0;
            this.btnTestApi.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTestApi.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.btnTestApi.ForeColor = System.Drawing.Color.White;
            this.btnTestApi.Location = new System.Drawing.Point(30, 95);
            this.btnTestApi.Name = "btnTestApi";
            this.btnTestApi.Size = new System.Drawing.Size(120, 30);
            this.btnTestApi.TabIndex = 2;
            this.btnTestApi.Text = "🔍 Test API";
            this.btnTestApi.UseVisualStyleBackColor = false;
            this.btnTestApi.Click += new System.EventHandler(this.BtnTestApi_Click);
            // 
            // txtApiEndpoint
            // 
            this.txtApiEndpoint.Font = new System.Drawing.Font("Consolas", 9.75F);
            this.txtApiEndpoint.Location = new System.Drawing.Point(30, 60);
            this.txtApiEndpoint.Name = "txtApiEndpoint";
            this.txtApiEndpoint.Size = new System.Drawing.Size(660, 23);
            this.txtApiEndpoint.TabIndex = 1;
            // 
            // lblApiEndpoint
            // 
            this.lblApiEndpoint.AutoSize = true;
            this.lblApiEndpoint.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblApiEndpoint.Location = new System.Drawing.Point(30, 40);
            this.lblApiEndpoint.Name = "lblApiEndpoint";
            this.lblApiEndpoint.Size = new System.Drawing.Size(83, 14);
            this.lblApiEndpoint.TabIndex = 0;
            this.lblApiEndpoint.Text = "API Endpoint:";
            // 
            // tabLog
            // 
            this.tabLog.BackColor = System.Drawing.Color.White;
            this.tabLog.Controls.Add(this.grpLog);
            this.tabLog.Location = new System.Drawing.Point(4, 25);
            this.tabLog.Name = "tabLog";
            this.tabLog.Padding = new System.Windows.Forms.Padding(3);
            this.tabLog.Size = new System.Drawing.Size(752, 421);
            this.tabLog.TabIndex = 2;
            this.tabLog.Text = "📝 Log";
            // 
            // grpLog
            // 
            this.grpLog.Controls.Add(this.pnlLogInfo);
            this.grpLog.Controls.Add(this.lblDays);
            this.grpLog.Controls.Add(this.numLogRetention);
            this.grpLog.Controls.Add(this.lblLogRetention);
            this.grpLog.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.grpLog.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(70)))), ((int)(((byte)(70)))));
            this.grpLog.Location = new System.Drawing.Point(15, 15);
            this.grpLog.Name = "grpLog";
            this.grpLog.Size = new System.Drawing.Size(720, 390);
            this.grpLog.TabIndex = 0;
            this.grpLog.TabStop = false;
            this.grpLog.Text = "Log Settings (App.config)";
            // 
            // pnlLogInfo
            // 
            this.pnlLogInfo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(250)))), ((int)(((byte)(205)))));
            this.pnlLogInfo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlLogInfo.Controls.Add(this.lblLogInfo);
            this.pnlLogInfo.Location = new System.Drawing.Point(30, 140);
            this.pnlLogInfo.Name = "pnlLogInfo";
            this.pnlLogInfo.Size = new System.Drawing.Size(660, 230);
            this.pnlLogInfo.TabIndex = 3;
            // 
            // lblLogInfo
            // 
            this.lblLogInfo.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.lblLogInfo.ForeColor = System.Drawing.Color.DarkOrange;
            this.lblLogInfo.Location = new System.Drawing.Point(10, 10);
            this.lblLogInfo.Name = "lblLogInfo";
            this.lblLogInfo.Size = new System.Drawing.Size(640, 210);
            this.lblLogInfo.TabIndex = 0;
            this.lblLogInfo.Text = resources.GetString("lblLogInfo.Text");
            // 
            // lblDays
            // 
            this.lblDays.AutoSize = true;
            this.lblDays.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblDays.ForeColor = System.Drawing.Color.DimGray;
            this.lblDays.Location = new System.Drawing.Point(160, 94);
            this.lblDays.Name = "lblDays";
            this.lblDays.Size = new System.Drawing.Size(20, 14);
            this.lblDays.TabIndex = 2;
            this.lblDays.Text = "วัน";
            // 
            // numLogRetention
            // 
            this.numLogRetention.Font = new System.Drawing.Font("Tahoma", 10F);
            this.numLogRetention.Location = new System.Drawing.Point(30, 90);
            this.numLogRetention.Maximum = new decimal(new int[] {
            365,
            0,
            0,
            0});
            this.numLogRetention.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numLogRetention.Name = "numLogRetention";
            this.numLogRetention.Size = new System.Drawing.Size(120, 24);
            this.numLogRetention.TabIndex = 1;
            this.numLogRetention.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numLogRetention.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            // 
            // lblLogRetention
            // 
            this.lblLogRetention.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblLogRetention.Location = new System.Drawing.Point(30, 40);
            this.lblLogRetention.Name = "lblLogRetention";
            this.lblLogRetention.Size = new System.Drawing.Size(550, 40);
            this.lblLogRetention.TabIndex = 0;
            this.lblLogRetention.Text = "จำนวนวันที่ต้องการเก็บไฟล์ Log:\r\n(ไฟล์ที่เก่ากว่านี้จะถูกลบอัตโนมัติ)";
            // 
            // btnSave
            // 
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(139)))), ((int)(((byte)(34)))));
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.FlatAppearance.BorderSize = 0;
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("Tahoma", 10F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(540, 475);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(110, 40);
            this.btnSave.TabIndex = 1;
            this.btnSave.Text = "💾 Save";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.BtnSave_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.BackColor = System.Drawing.Color.LightGray;
            this.btnCancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Tahoma", 10F);
            this.btnCancel.Location = new System.Drawing.Point(660, 475);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(110, 40);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(811, 531);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.tabControl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "⚙️ System Settings";
            this.tabControl.ResumeLayout(false);
            this.tabDatabase.ResumeLayout(false);
            this.grpDatabase.ResumeLayout(false);
            this.grpDatabase.PerformLayout();
            this.pnlDbInfo.ResumeLayout(false);
            this.tabAPI.ResumeLayout(false);
            this.grpAPI.ResumeLayout(false);
            this.grpAPI.PerformLayout();
            this.pnlApiInfo.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numApiRetryDelay)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numApiRetry)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numApiTimeout)).EndInit();
            this.tabLog.ResumeLayout(false);
            this.grpLog.ResumeLayout(false);
            this.grpLog.PerformLayout();
            this.pnlLogInfo.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numLogRetention)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabDatabase;
        private System.Windows.Forms.TabPage tabAPI;
        private System.Windows.Forms.TabPage tabLog;
        private System.Windows.Forms.GroupBox grpDatabase;
        private System.Windows.Forms.Panel pnlDbInfo;
        private System.Windows.Forms.Label lblDbInfo;
        private System.Windows.Forms.Label lblConnectionStatus;
        private System.Windows.Forms.Button btnTestConnection;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.TextBox txtUserId;
        private System.Windows.Forms.Label lblUserId;
        private System.Windows.Forms.TextBox txtDatabase;
        private System.Windows.Forms.Label lblDatabase;
        private System.Windows.Forms.TextBox txtServer;
        private System.Windows.Forms.Label lblServer;
        private System.Windows.Forms.GroupBox grpAPI;
        private System.Windows.Forms.Panel pnlApiInfo;
        private System.Windows.Forms.Label lblApiInfo;
        private System.Windows.Forms.Label lblApiRetryDelayUnit;
        private System.Windows.Forms.NumericUpDown numApiRetryDelay;
        private System.Windows.Forms.Label lblApiRetryDelay;
        private System.Windows.Forms.Label lblApiRetryUnit;
        private System.Windows.Forms.NumericUpDown numApiRetry;
        private System.Windows.Forms.Label lblApiRetry;
        private System.Windows.Forms.Label lblApiTimeoutUnit;
        private System.Windows.Forms.NumericUpDown numApiTimeout;
        private System.Windows.Forms.Label lblApiTimeout;
        private System.Windows.Forms.Label lblApiStatus;
        private System.Windows.Forms.Button btnTestApi;
        private System.Windows.Forms.TextBox txtApiEndpoint;
        private System.Windows.Forms.Label lblApiEndpoint;
        private System.Windows.Forms.GroupBox grpLog;
        private System.Windows.Forms.Panel pnlLogInfo;
        private System.Windows.Forms.Label lblLogInfo;
        private System.Windows.Forms.Label lblDays;
        private System.Windows.Forms.NumericUpDown numLogRetention;
        private System.Windows.Forms.Label lblLogRetention;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
     
    }
}