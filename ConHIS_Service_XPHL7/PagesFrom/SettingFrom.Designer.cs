using System;
using System.Windows.Forms;

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
            this.grpDatabase = new System.Windows.Forms.GroupBox();
            this.cmbEncoding = new System.Windows.Forms.ComboBox();
            this.lblEncoding = new System.Windows.Forms.Label();
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
            // ⭐ IPD Tab
            this.tabDatabaseIPD = new System.Windows.Forms.TabPage();
            this.grpDatabaseIPD = new System.Windows.Forms.GroupBox();
            this.cmbEncodingIPD = new System.Windows.Forms.ComboBox();
            this.lblEncodingIPD = new System.Windows.Forms.Label();
            this.lblConnectionStatusIPD = new System.Windows.Forms.Label();
            this.btnTestConnectionIPD = new System.Windows.Forms.Button();
            this.txtPasswordIPD = new System.Windows.Forms.TextBox();
            this.lblPasswordIPD = new System.Windows.Forms.Label();
            this.txtUserIdIPD = new System.Windows.Forms.TextBox();
            this.lblUserIdIPD = new System.Windows.Forms.Label();
            this.txtDatabaseIPD = new System.Windows.Forms.TextBox();
            this.lblDatabaseIPD = new System.Windows.Forms.Label();
            this.txtServerIPD = new System.Windows.Forms.TextBox();
            this.lblServerIPD = new System.Windows.Forms.Label();
            this.chkSameAsOPD = new System.Windows.Forms.CheckBox();
            // API Tab
            this.tabAPI = new System.Windows.Forms.TabPage();
            this.grpAPI = new System.Windows.Forms.GroupBox();
            this.lblApiRetryDelayUnit = new System.Windows.Forms.Label();
            this.numApiRetryDelay = new System.Windows.Forms.NumericUpDown();
            this.lblApiRetryDelay = new System.Windows.Forms.Label();
            this.lblApiRetryUnit = new System.Windows.Forms.Label();
            this.numApiRetry = new System.Windows.Forms.NumericUpDown();
            this.lblApiRetry = new System.Windows.Forms.Label();
            this.lblApiTimeoutUnit = new System.Windows.Forms.Label();
            this.numApiTimeout = new System.Windows.Forms.NumericUpDown();
            this.lblApiTimeout = new System.Windows.Forms.Label();
            this.txtApiEndpoint = new System.Windows.Forms.TextBox();
            this.lblApiEndpoint = new System.Windows.Forms.Label();
            this.tabLog = new System.Windows.Forms.TabPage();
            this.grpLog = new System.Windows.Forms.GroupBox();
            this.lblDays = new System.Windows.Forms.Label();
            this.numLogRetention = new System.Windows.Forms.NumericUpDown();
            this.lblLogRetention = new System.Windows.Forms.Label();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.tabControl.SuspendLayout();
            this.tabDatabase.SuspendLayout();
            this.grpDatabase.SuspendLayout();
            this.tabDatabaseIPD.SuspendLayout();
            this.grpDatabaseIPD.SuspendLayout();
            this.tabAPI.SuspendLayout();
            this.grpAPI.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numApiRetryDelay)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numApiRetry)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numApiTimeout)).BeginInit();
            this.tabLog.SuspendLayout();
            this.grpLog.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numLogRetention)).BeginInit();
            this.SuspendLayout();

            // ── tabControl ──────────────────────────────────────────────────
            this.tabControl.Controls.Add(this.tabDatabase);
            this.tabControl.Controls.Add(this.tabDatabaseIPD);
            this.tabControl.Controls.Add(this.tabAPI);
            this.tabControl.Controls.Add(this.tabLog);
            this.tabControl.Font = new System.Drawing.Font("Tahoma", 9.75F);
            this.tabControl.Location = new System.Drawing.Point(12, 12);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(528, 356);
            this.tabControl.TabIndex = 0;

            // ── tabDatabase (OPD) ───────────────────────────────────────────
            this.tabDatabase.BackColor = System.Drawing.Color.White;
            this.tabDatabase.Controls.Add(this.grpDatabase);
            this.tabDatabase.Location = new System.Drawing.Point(4, 25);
            this.tabDatabase.Name = "tabDatabase";
            this.tabDatabase.Padding = new System.Windows.Forms.Padding(3);
            this.tabDatabase.Size = new System.Drawing.Size(520, 327);
            this.tabDatabase.TabIndex = 0;
            this.tabDatabase.Text = "🗄️ OPD Database";

            // ── grpDatabase ─────────────────────────────────────────────────
            this.grpDatabase.Controls.Add(this.cmbEncoding);
            this.grpDatabase.Controls.Add(this.lblEncoding);
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
            this.grpDatabase.ForeColor = System.Drawing.Color.FromArgb(70, 70, 70);
            this.grpDatabase.Location = new System.Drawing.Point(15, 10);
            this.grpDatabase.Name = "grpDatabase";
            this.grpDatabase.Size = new System.Drawing.Size(489, 306);
            this.grpDatabase.TabIndex = 0;
            this.grpDatabase.TabStop = false;
            this.grpDatabase.Text = "OPD Database Connection (connectdatabase.ini)";

            // OPD controls (same positions as original)
            this.cmbEncoding.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbEncoding.Font = new System.Drawing.Font("Tahoma", 10F);
            this.cmbEncoding.Items.AddRange(new object[] { "UTF-8", "TIS-620" });
            this.cmbEncoding.Location = new System.Drawing.Point(150, 197);
            this.cmbEncoding.Name = "cmbEncoding";
            this.cmbEncoding.Size = new System.Drawing.Size(200, 24);
            this.cmbEncoding.TabIndex = 11;

            this.lblEncoding.AutoSize = true;
            this.lblEncoding.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblEncoding.Location = new System.Drawing.Point(30, 200);
            this.lblEncoding.Name = "lblEncoding";
            this.lblEncoding.Size = new System.Drawing.Size(62, 14);
            this.lblEncoding.TabIndex = 10;
            this.lblEncoding.Text = "Encoding:";

            this.lblConnectionStatus.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.lblConnectionStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblConnectionStatus.Location = new System.Drawing.Point(278, 241);
            this.lblConnectionStatus.Name = "lblConnectionStatus";
            this.lblConnectionStatus.Size = new System.Drawing.Size(205, 35);
            this.lblConnectionStatus.TabIndex = 9;
            this.lblConnectionStatus.Text = "ℹ️ Click Test Connection to verify";
            this.lblConnectionStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            this.btnTestConnection.BackColor = System.Drawing.Color.FromArgb(0, 122, 204);
            this.btnTestConnection.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTestConnection.FlatAppearance.BorderSize = 0;
            this.btnTestConnection.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTestConnection.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.btnTestConnection.ForeColor = System.Drawing.Color.White;
            this.btnTestConnection.Location = new System.Drawing.Point(118, 242);
            this.btnTestConnection.Name = "btnTestConnection";
            this.btnTestConnection.Size = new System.Drawing.Size(150, 35);
            this.btnTestConnection.TabIndex = 8;
            this.btnTestConnection.Text = "🔌 Test Connection";
            this.btnTestConnection.UseVisualStyleBackColor = false;
            this.btnTestConnection.Click += new System.EventHandler(this.BtnTestConnection_Click);

            this.txtPassword.Font = new System.Drawing.Font("Tahoma", 10F);
            this.txtPassword.Location = new System.Drawing.Point(150, 157);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(335, 24);
            this.txtPassword.TabIndex = 7;
            this.txtPassword.UseSystemPasswordChar = true;

            this.lblPassword.AutoSize = true;
            this.lblPassword.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblPassword.Location = new System.Drawing.Point(30, 160);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.TabIndex = 6;
            this.lblPassword.Text = "Password:";

            this.txtUserId.Font = new System.Drawing.Font("Tahoma", 10F);
            this.txtUserId.Location = new System.Drawing.Point(150, 117);
            this.txtUserId.Name = "txtUserId";
            this.txtUserId.Size = new System.Drawing.Size(335, 24);
            this.txtUserId.TabIndex = 5;

            this.lblUserId.AutoSize = true;
            this.lblUserId.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblUserId.Location = new System.Drawing.Point(30, 120);
            this.lblUserId.Name = "lblUserId";
            this.lblUserId.TabIndex = 4;
            this.lblUserId.Text = "User ID:";

            this.txtDatabase.Font = new System.Drawing.Font("Tahoma", 10F);
            this.txtDatabase.Location = new System.Drawing.Point(150, 77);
            this.txtDatabase.Name = "txtDatabase";
            this.txtDatabase.Size = new System.Drawing.Size(335, 24);
            this.txtDatabase.TabIndex = 3;

            this.lblDatabase.AutoSize = true;
            this.lblDatabase.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblDatabase.Location = new System.Drawing.Point(30, 80);
            this.lblDatabase.Name = "lblDatabase";
            this.lblDatabase.TabIndex = 2;
            this.lblDatabase.Text = "Database:";

            this.txtServer.Font = new System.Drawing.Font("Tahoma", 10F);
            this.txtServer.Location = new System.Drawing.Point(150, 37);
            this.txtServer.Name = "txtServer";
            this.txtServer.Size = new System.Drawing.Size(335, 24);
            this.txtServer.TabIndex = 1;

            this.lblServer.AutoSize = true;
            this.lblServer.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblServer.Location = new System.Drawing.Point(30, 40);
            this.lblServer.Name = "lblServer";
            this.lblServer.TabIndex = 0;
            this.lblServer.Text = "Server:";

            // ── tabDatabaseIPD ──────────────────────────────────────────────
            this.tabDatabaseIPD.BackColor = System.Drawing.Color.White;
            this.tabDatabaseIPD.Controls.Add(this.grpDatabaseIPD);
            this.tabDatabaseIPD.Location = new System.Drawing.Point(4, 25);
            this.tabDatabaseIPD.Name = "tabDatabaseIPD";
            this.tabDatabaseIPD.Padding = new System.Windows.Forms.Padding(3);
            this.tabDatabaseIPD.Size = new System.Drawing.Size(520, 327);
            this.tabDatabaseIPD.TabIndex = 5;
            this.tabDatabaseIPD.Text = "🏥 IPD Database";

            // ── grpDatabaseIPD ──────────────────────────────────────────────
            this.grpDatabaseIPD.Controls.Add(this.chkSameAsOPD);
            this.grpDatabaseIPD.Controls.Add(this.cmbEncodingIPD);
            this.grpDatabaseIPD.Controls.Add(this.lblEncodingIPD);
            this.grpDatabaseIPD.Controls.Add(this.lblConnectionStatusIPD);
            this.grpDatabaseIPD.Controls.Add(this.btnTestConnectionIPD);
            this.grpDatabaseIPD.Controls.Add(this.txtPasswordIPD);
            this.grpDatabaseIPD.Controls.Add(this.lblPasswordIPD);
            this.grpDatabaseIPD.Controls.Add(this.txtUserIdIPD);
            this.grpDatabaseIPD.Controls.Add(this.lblUserIdIPD);
            this.grpDatabaseIPD.Controls.Add(this.txtDatabaseIPD);
            this.grpDatabaseIPD.Controls.Add(this.lblDatabaseIPD);
            this.grpDatabaseIPD.Controls.Add(this.txtServerIPD);
            this.grpDatabaseIPD.Controls.Add(this.lblServerIPD);
            this.grpDatabaseIPD.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.grpDatabaseIPD.ForeColor = System.Drawing.Color.FromArgb(70, 70, 70);
            this.grpDatabaseIPD.Location = new System.Drawing.Point(15, 10);
            this.grpDatabaseIPD.Name = "grpDatabaseIPD";
            this.grpDatabaseIPD.Size = new System.Drawing.Size(489, 306);
            this.grpDatabaseIPD.TabIndex = 0;
            this.grpDatabaseIPD.TabStop = false;
            this.grpDatabaseIPD.Text = "IPD Database Connection (connectdatabase_ipd.ini)";

            // chkSameAsOPD
            this.chkSameAsOPD.AutoSize = true;
            this.chkSameAsOPD.Font = new System.Drawing.Font("Tahoma", 9F);
            this.chkSameAsOPD.Location = new System.Drawing.Point(30, 25);
            this.chkSameAsOPD.Name = "chkSameAsOPD";
            this.chkSameAsOPD.Size = new System.Drawing.Size(200, 18);
            this.chkSameAsOPD.TabIndex = 12;
            this.chkSameAsOPD.Text = "ใช้การตั้งค่าเดียวกับ OPD Database";
            this.chkSameAsOPD.CheckedChanged += new System.EventHandler(this.ChkSameAsOPD_CheckedChanged);

            // IPD Server
            this.lblServerIPD.AutoSize = true;
            this.lblServerIPD.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblServerIPD.Location = new System.Drawing.Point(30, 58);
            this.lblServerIPD.Name = "lblServerIPD";
            this.lblServerIPD.TabIndex = 0;
            this.lblServerIPD.Text = "Server:";

            this.txtServerIPD.Font = new System.Drawing.Font("Tahoma", 10F);
            this.txtServerIPD.Location = new System.Drawing.Point(150, 55);
            this.txtServerIPD.Name = "txtServerIPD";
            this.txtServerIPD.Size = new System.Drawing.Size(335, 24);
            this.txtServerIPD.TabIndex = 1;

            // IPD Database
            this.lblDatabaseIPD.AutoSize = true;
            this.lblDatabaseIPD.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblDatabaseIPD.Location = new System.Drawing.Point(30, 98);
            this.lblDatabaseIPD.Name = "lblDatabaseIPD";
            this.lblDatabaseIPD.TabIndex = 2;
            this.lblDatabaseIPD.Text = "Database:";

            this.txtDatabaseIPD.Font = new System.Drawing.Font("Tahoma", 10F);
            this.txtDatabaseIPD.Location = new System.Drawing.Point(150, 95);
            this.txtDatabaseIPD.Name = "txtDatabaseIPD";
            this.txtDatabaseIPD.Size = new System.Drawing.Size(335, 24);
            this.txtDatabaseIPD.TabIndex = 3;

            // IPD User ID
            this.lblUserIdIPD.AutoSize = true;
            this.lblUserIdIPD.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblUserIdIPD.Location = new System.Drawing.Point(30, 138);
            this.lblUserIdIPD.Name = "lblUserIdIPD";
            this.lblUserIdIPD.TabIndex = 4;
            this.lblUserIdIPD.Text = "User ID:";

            this.txtUserIdIPD.Font = new System.Drawing.Font("Tahoma", 10F);
            this.txtUserIdIPD.Location = new System.Drawing.Point(150, 135);
            this.txtUserIdIPD.Name = "txtUserIdIPD";
            this.txtUserIdIPD.Size = new System.Drawing.Size(335, 24);
            this.txtUserIdIPD.TabIndex = 5;

            // IPD Password
            this.lblPasswordIPD.AutoSize = true;
            this.lblPasswordIPD.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblPasswordIPD.Location = new System.Drawing.Point(30, 178);
            this.lblPasswordIPD.Name = "lblPasswordIPD";
            this.lblPasswordIPD.TabIndex = 6;
            this.lblPasswordIPD.Text = "Password:";

            this.txtPasswordIPD.Font = new System.Drawing.Font("Tahoma", 10F);
            this.txtPasswordIPD.Location = new System.Drawing.Point(150, 175);
            this.txtPasswordIPD.Name = "txtPasswordIPD";
            this.txtPasswordIPD.Size = new System.Drawing.Size(335, 24);
            this.txtPasswordIPD.TabIndex = 7;
            this.txtPasswordIPD.UseSystemPasswordChar = true;

            // IPD Encoding
            this.lblEncodingIPD.AutoSize = true;
            this.lblEncodingIPD.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblEncodingIPD.Location = new System.Drawing.Point(30, 218);
            this.lblEncodingIPD.Name = "lblEncodingIPD";
            this.lblEncodingIPD.TabIndex = 10;
            this.lblEncodingIPD.Text = "Encoding:";

            this.cmbEncodingIPD.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbEncodingIPD.Font = new System.Drawing.Font("Tahoma", 10F);
            this.cmbEncodingIPD.Items.AddRange(new object[] { "UTF-8", "TIS-620" });
            this.cmbEncodingIPD.Location = new System.Drawing.Point(150, 215);
            this.cmbEncodingIPD.Name = "cmbEncodingIPD";
            this.cmbEncodingIPD.Size = new System.Drawing.Size(200, 24);
            this.cmbEncodingIPD.TabIndex = 11;

            // IPD Test Connection
            this.btnTestConnectionIPD.BackColor = System.Drawing.Color.FromArgb(52, 152, 219);
            this.btnTestConnectionIPD.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTestConnectionIPD.FlatAppearance.BorderSize = 0;
            this.btnTestConnectionIPD.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTestConnectionIPD.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.btnTestConnectionIPD.ForeColor = System.Drawing.Color.White;
            this.btnTestConnectionIPD.Location = new System.Drawing.Point(118, 258);
            this.btnTestConnectionIPD.Name = "btnTestConnectionIPD";
            this.btnTestConnectionIPD.Size = new System.Drawing.Size(150, 35);
            this.btnTestConnectionIPD.TabIndex = 8;
            this.btnTestConnectionIPD.Text = "🔌 Test IPD Connection";
            this.btnTestConnectionIPD.UseVisualStyleBackColor = false;
            this.btnTestConnectionIPD.Click += new System.EventHandler(this.BtnTestConnectionIPD_Click);

            this.lblConnectionStatusIPD.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.lblConnectionStatusIPD.ForeColor = System.Drawing.Color.Gray;
            this.lblConnectionStatusIPD.Location = new System.Drawing.Point(278, 258);
            this.lblConnectionStatusIPD.Name = "lblConnectionStatusIPD";
            this.lblConnectionStatusIPD.Size = new System.Drawing.Size(205, 35);
            this.lblConnectionStatusIPD.TabIndex = 9;
            this.lblConnectionStatusIPD.Text = "ℹ️ Click Test Connection to verify";
            this.lblConnectionStatusIPD.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // ── tabAPI ──────────────────────────────────────────────────────
            this.tabAPI.BackColor = System.Drawing.Color.White;
            this.tabAPI.Controls.Add(this.grpAPI);
            this.tabAPI.Location = new System.Drawing.Point(4, 25);
            this.tabAPI.Name = "tabAPI";
            this.tabAPI.Padding = new System.Windows.Forms.Padding(3);
            this.tabAPI.Size = new System.Drawing.Size(520, 327);
            this.tabAPI.TabIndex = 1;
            this.tabAPI.Text = "🌐 API";

            this.grpAPI.Controls.Add(this.lblApiRetryDelayUnit);
            this.grpAPI.Controls.Add(this.numApiRetryDelay);
            this.grpAPI.Controls.Add(this.lblApiRetryDelay);
            this.grpAPI.Controls.Add(this.lblApiRetryUnit);
            this.grpAPI.Controls.Add(this.numApiRetry);
            this.grpAPI.Controls.Add(this.lblApiRetry);
            this.grpAPI.Controls.Add(this.lblApiTimeoutUnit);
            this.grpAPI.Controls.Add(this.numApiTimeout);
            this.grpAPI.Controls.Add(this.lblApiTimeout);
            this.grpAPI.Controls.Add(this.txtApiEndpoint);
            this.grpAPI.Controls.Add(this.lblApiEndpoint);
            this.grpAPI.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.grpAPI.ForeColor = System.Drawing.Color.FromArgb(70, 70, 70);
            this.grpAPI.Location = new System.Drawing.Point(12, 9);
            this.grpAPI.Name = "grpAPI";
            this.grpAPI.Size = new System.Drawing.Size(499, 306);
            this.grpAPI.TabIndex = 0;
            this.grpAPI.TabStop = false;
            this.grpAPI.Text = "API Settings (appsettings.ini)";

            this.lblApiRetryDelayUnit.AutoSize = true;
            this.lblApiRetryDelayUnit.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.lblApiRetryDelayUnit.ForeColor = System.Drawing.Color.Gray;
            this.lblApiRetryDelayUnit.Location = new System.Drawing.Point(310, 194);
            this.lblApiRetryDelayUnit.Name = "lblApiRetryDelayUnit";
            this.lblApiRetryDelayUnit.TabIndex = 12;
            this.lblApiRetryDelayUnit.Text = "วินาที";

            this.numApiRetryDelay.Font = new System.Drawing.Font("Tahoma", 10F);
            this.numApiRetryDelay.Location = new System.Drawing.Point(220, 189);
            this.numApiRetryDelay.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
            this.numApiRetryDelay.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numApiRetryDelay.Name = "numApiRetryDelay";
            this.numApiRetryDelay.Size = new System.Drawing.Size(80, 24);
            this.numApiRetryDelay.TabIndex = 11;
            this.numApiRetryDelay.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numApiRetryDelay.Value = new decimal(new int[] { 5, 0, 0, 0 });

            this.lblApiRetryDelay.AutoSize = true;
            this.lblApiRetryDelay.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblApiRetryDelay.Location = new System.Drawing.Point(30, 192);
            this.lblApiRetryDelay.Name = "lblApiRetryDelay";
            this.lblApiRetryDelay.TabIndex = 10;
            this.lblApiRetryDelay.Text = "Retry Delay:";

            this.lblApiRetryUnit.AutoSize = true;
            this.lblApiRetryUnit.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.lblApiRetryUnit.ForeColor = System.Drawing.Color.Gray;
            this.lblApiRetryUnit.Location = new System.Drawing.Point(310, 154);
            this.lblApiRetryUnit.Name = "lblApiRetryUnit";
            this.lblApiRetryUnit.TabIndex = 9;
            this.lblApiRetryUnit.Text = "ครั้ง";

            this.numApiRetry.Font = new System.Drawing.Font("Tahoma", 10F);
            this.numApiRetry.Location = new System.Drawing.Point(220, 149);
            this.numApiRetry.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            this.numApiRetry.Name = "numApiRetry";
            this.numApiRetry.Size = new System.Drawing.Size(80, 24);
            this.numApiRetry.TabIndex = 8;
            this.numApiRetry.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numApiRetry.Value = new decimal(new int[] { 3, 0, 0, 0 });

            this.lblApiRetry.AutoSize = true;
            this.lblApiRetry.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblApiRetry.Location = new System.Drawing.Point(30, 152);
            this.lblApiRetry.Name = "lblApiRetry";
            this.lblApiRetry.TabIndex = 7;
            this.lblApiRetry.Text = "Retry Attempts:";

            this.lblApiTimeoutUnit.AutoSize = true;
            this.lblApiTimeoutUnit.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.lblApiTimeoutUnit.ForeColor = System.Drawing.Color.Gray;
            this.lblApiTimeoutUnit.Location = new System.Drawing.Point(310, 114);
            this.lblApiTimeoutUnit.Name = "lblApiTimeoutUnit";
            this.lblApiTimeoutUnit.TabIndex = 6;
            this.lblApiTimeoutUnit.Text = "วินาที";

            this.numApiTimeout.Font = new System.Drawing.Font("Tahoma", 10F);
            this.numApiTimeout.Location = new System.Drawing.Point(220, 109);
            this.numApiTimeout.Maximum = new decimal(new int[] { 300, 0, 0, 0 });
            this.numApiTimeout.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
            this.numApiTimeout.Name = "numApiTimeout";
            this.numApiTimeout.Size = new System.Drawing.Size(80, 24);
            this.numApiTimeout.TabIndex = 5;
            this.numApiTimeout.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numApiTimeout.Value = new decimal(new int[] { 30, 0, 0, 0 });

            this.lblApiTimeout.AutoSize = true;
            this.lblApiTimeout.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblApiTimeout.Location = new System.Drawing.Point(30, 112);
            this.lblApiTimeout.Name = "lblApiTimeout";
            this.lblApiTimeout.TabIndex = 4;
            this.lblApiTimeout.Text = "API Timeout:";

            this.txtApiEndpoint.Font = new System.Drawing.Font("Consolas", 9.75F);
            this.txtApiEndpoint.Location = new System.Drawing.Point(30, 67);
            this.txtApiEndpoint.Name = "txtApiEndpoint";
            this.txtApiEndpoint.Size = new System.Drawing.Size(456, 23);
            this.txtApiEndpoint.TabIndex = 1;

            this.lblApiEndpoint.AutoSize = true;
            this.lblApiEndpoint.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblApiEndpoint.Location = new System.Drawing.Point(30, 40);
            this.lblApiEndpoint.Name = "lblApiEndpoint";
            this.lblApiEndpoint.TabIndex = 0;
            this.lblApiEndpoint.Text = "API Endpoint:";

            // ── tabLog ──────────────────────────────────────────────────────
            this.tabLog.BackColor = System.Drawing.Color.White;
            this.tabLog.Controls.Add(this.grpLog);
            this.tabLog.Location = new System.Drawing.Point(4, 25);
            this.tabLog.Name = "tabLog";
            this.tabLog.Padding = new System.Windows.Forms.Padding(3);
            this.tabLog.Size = new System.Drawing.Size(520, 327);
            this.tabLog.TabIndex = 2;
            this.tabLog.Text = "📝 Log";

            this.grpLog.Controls.Add(this.lblDays);
            this.grpLog.Controls.Add(this.numLogRetention);
            this.grpLog.Controls.Add(this.lblLogRetention);
            this.grpLog.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.grpLog.ForeColor = System.Drawing.Color.FromArgb(70, 70, 70);
            this.grpLog.Location = new System.Drawing.Point(15, 15);
            this.grpLog.Name = "grpLog";
            this.grpLog.Size = new System.Drawing.Size(493, 299);
            this.grpLog.TabIndex = 0;
            this.grpLog.TabStop = false;
            this.grpLog.Text = "Log Settings (App.config)";

            this.lblDays.AutoSize = true;
            this.lblDays.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblDays.ForeColor = System.Drawing.Color.DimGray;
            this.lblDays.Location = new System.Drawing.Point(160, 94);
            this.lblDays.Name = "lblDays";
            this.lblDays.TabIndex = 2;
            this.lblDays.Text = "วัน";

            this.numLogRetention.Font = new System.Drawing.Font("Tahoma", 10F);
            this.numLogRetention.Location = new System.Drawing.Point(30, 90);
            this.numLogRetention.Maximum = new decimal(new int[] { 365, 0, 0, 0 });
            this.numLogRetention.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numLogRetention.Name = "numLogRetention";
            this.numLogRetention.Size = new System.Drawing.Size(120, 24);
            this.numLogRetention.TabIndex = 1;
            this.numLogRetention.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.numLogRetention.Value = new decimal(new int[] { 30, 0, 0, 0 });

            this.lblLogRetention.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblLogRetention.Location = new System.Drawing.Point(30, 40);
            this.lblLogRetention.Name = "lblLogRetention";
            this.lblLogRetention.Size = new System.Drawing.Size(184, 40);
            this.lblLogRetention.TabIndex = 0;
            this.lblLogRetention.Text = "จำนวนวันที่ต้องการเก็บไฟล์ Log:\r\n(ไฟล์ที่เก่ากว่านี้จะถูกลบอัตโนมัติ)";

            // ── Buttons ─────────────────────────────────────────────────────
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(34, 139, 34);
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.FlatAppearance.BorderSize = 0;
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("Tahoma", 10F, System.Drawing.FontStyle.Bold);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(306, 374);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(110, 40);
            this.btnSave.TabIndex = 1;
            this.btnSave.Text = "💾 Save";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.BtnSave_Click);

            this.btnCancel.BackColor = System.Drawing.Color.LightGray;
            this.btnCancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Tahoma", 10F);
            this.btnCancel.Location = new System.Drawing.Point(422, 374);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(110, 40);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);

            // ── SettingsForm ─────────────────────────────────────────────────
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(611, 430);
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
            this.tabDatabaseIPD.ResumeLayout(false);
            this.grpDatabaseIPD.ResumeLayout(false);
            this.grpDatabaseIPD.PerformLayout();
            this.tabAPI.ResumeLayout(false);
            this.grpAPI.ResumeLayout(false);
            this.grpAPI.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numApiRetryDelay)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numApiRetry)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numApiTimeout)).EndInit();
            this.tabLog.ResumeLayout(false);
            this.grpLog.ResumeLayout(false);
            this.grpLog.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numLogRetention)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        // OPD
        private System.Windows.Forms.TabPage tabDatabase;
        private System.Windows.Forms.GroupBox grpDatabase;
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
        private System.Windows.Forms.Label lblEncoding;
        private System.Windows.Forms.ComboBox cmbEncoding;
        // ⭐ IPD
        private System.Windows.Forms.TabPage tabDatabaseIPD;
        private System.Windows.Forms.GroupBox grpDatabaseIPD;
        private System.Windows.Forms.CheckBox chkSameAsOPD;
        private System.Windows.Forms.Label lblConnectionStatusIPD;
        private System.Windows.Forms.Button btnTestConnectionIPD;
        private System.Windows.Forms.TextBox txtPasswordIPD;
        private System.Windows.Forms.Label lblPasswordIPD;
        private System.Windows.Forms.TextBox txtUserIdIPD;
        private System.Windows.Forms.Label lblUserIdIPD;
        private System.Windows.Forms.TextBox txtDatabaseIPD;
        private System.Windows.Forms.Label lblDatabaseIPD;
        private System.Windows.Forms.TextBox txtServerIPD;
        private System.Windows.Forms.Label lblServerIPD;
        private System.Windows.Forms.Label lblEncodingIPD;
        private System.Windows.Forms.ComboBox cmbEncodingIPD;
        // API
        private System.Windows.Forms.TabPage tabAPI;
        private System.Windows.Forms.GroupBox grpAPI;
        private System.Windows.Forms.Label lblApiRetryDelayUnit;
        private System.Windows.Forms.NumericUpDown numApiRetryDelay;
        private System.Windows.Forms.Label lblApiRetryDelay;
        private System.Windows.Forms.Label lblApiRetryUnit;
        private System.Windows.Forms.NumericUpDown numApiRetry;
        private System.Windows.Forms.Label lblApiRetry;
        private System.Windows.Forms.Label lblApiTimeoutUnit;
        private System.Windows.Forms.NumericUpDown numApiTimeout;
        private System.Windows.Forms.Label lblApiTimeout;
        private System.Windows.Forms.TextBox txtApiEndpoint;
        private System.Windows.Forms.Label lblApiEndpoint;
        // Log
        private System.Windows.Forms.TabPage tabLog;
        private System.Windows.Forms.GroupBox grpLog;
        private System.Windows.Forms.Label lblDays;
        private System.Windows.Forms.NumericUpDown numLogRetention;
        private System.Windows.Forms.Label lblLogRetention;
        // Buttons
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
    }
}