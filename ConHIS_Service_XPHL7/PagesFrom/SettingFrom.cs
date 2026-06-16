using System;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace ConHIS_Service_XPHL7.PagesFrom
{
    public partial class SettingsForm : System.Windows.Forms.Form
    {
        private const string ConnFolder = "Connection";
        private const string ConnFile = "connectdatabase.ini";
        private const string ConnFileIPD = "connectdatabase_ipd.ini"; // ⭐ IPD
        private const string ConfigFolder = "Config";
        private const string ConfigFile = "appsettings.ini";

        public bool SettingsChanged { get; private set; }

        public SettingsForm()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        // ════════════════════════════════════════════════════════════════════
        //  LOAD
        // ════════════════════════════════════════════════════════════════════

        private void LoadCurrentSettings()
        {
            try
            {
                LoadDatabaseSettings();
                LoadDatabaseIPDSettings(); // ⭐
                LoadAPISettings();
                LoadLogSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"เกิดข้อผิดพลาดในการโหลดการตั้งค่า:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadDatabaseSettings()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConnFolder, ConnFile);
            cmbEncoding.SelectedIndex = 0; // default UTF-8

            if (!File.Exists(path)) return;

            foreach (var line in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.StartsWith("Server="))
                    txtServer.Text = line.Replace("Server=", "").Trim().TrimEnd(';');
                else if (line.StartsWith("Database="))
                    txtDatabase.Text = line.Replace("Database=", "").Trim().TrimEnd(';');
                else if (line.StartsWith("User Id="))
                    txtUserId.Text = line.Replace("User Id=", "").Trim().TrimEnd(';');
                else if (line.StartsWith("Password="))
                    txtPassword.Text = line.Replace("Password=", "").Trim().TrimEnd(';');
                else if (line.StartsWith("Charset="))
                {
                    var charset = line.Replace("Charset=", "").Trim().TrimEnd(';').ToLower();
                    cmbEncoding.SelectedIndex = (charset == "tis620" || charset == "tis-620") ? 1 : 0;
                }
            }
        }

        // ⭐ โหลด IPD settings
        private void LoadDatabaseIPDSettings()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConnFolder, ConnFileIPD);
            cmbEncodingIPD.SelectedIndex = 0; // default UTF-8

            if (!File.Exists(path))
            {
                // ไม่มีไฟล์ IPD → copy ค่า OPD มาให้เป็น default
                CopyOPDToIPDFields();
                return;
            }

            bool hasContent = false;
            foreach (var line in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                hasContent = true;
                if (line.StartsWith("Server="))
                    txtServerIPD.Text = line.Replace("Server=", "").Trim().TrimEnd(';');
                else if (line.StartsWith("Database="))
                    txtDatabaseIPD.Text = line.Replace("Database=", "").Trim().TrimEnd(';');
                else if (line.StartsWith("User Id="))
                    txtUserIdIPD.Text = line.Replace("User Id=", "").Trim().TrimEnd(';');
                else if (line.StartsWith("Password="))
                    txtPasswordIPD.Text = line.Replace("Password=", "").Trim().TrimEnd(';');
                else if (line.StartsWith("Charset="))
                {
                    var charset = line.Replace("Charset=", "").Trim().TrimEnd(';').ToLower();
                    cmbEncodingIPD.SelectedIndex = (charset == "tis620" || charset == "tis-620") ? 1 : 0;
                }
            }

            if (!hasContent)
                CopyOPDToIPDFields();

            // ตรวจว่าค่าเหมือน OPD ไหม → ติ๊ก checkbox
            UpdateSameAsOPDCheckbox();
        }

        private void CopyOPDToIPDFields()
        {
            txtServerIPD.Text = txtServer.Text;
            txtDatabaseIPD.Text = txtDatabase.Text;
            txtUserIdIPD.Text = txtUserId.Text;
            txtPasswordIPD.Text = txtPassword.Text;
            cmbEncodingIPD.SelectedIndex = cmbEncoding.SelectedIndex;
        }

        private void UpdateSameAsOPDCheckbox()
        {
            bool same = txtServerIPD.Text == txtServer.Text
                     && txtUserIdIPD.Text == txtUserId.Text
                     && txtPasswordIPD.Text == txtPassword.Text
                     && cmbEncodingIPD.SelectedIndex == cmbEncoding.SelectedIndex;
            // database อาจต่างกัน (ipd_machine_gateway vs opd_machine_gateway) นั้น ok
            chkSameAsOPD.Checked = same;
        }

        private void LoadAPISettings()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFolder, ConfigFile);
            if (!File.Exists(path)) return;

            foreach (var line in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                var parts = line.Split('=');
                if (parts.Length != 2) continue;

                switch (parts[0].Trim().ToUpper())
                {
                    case "APIENDPOINT":
                        txtApiEndpoint.Text = parts[1].Trim(); break;
                    case "APITIMEOUTSECONDS":
                        if (int.TryParse(parts[1].Trim(), out int t)) numApiTimeout.Value = t; break;
                    case "APIRETRYATTEMPTS":
                        if (int.TryParse(parts[1].Trim(), out int r)) numApiRetry.Value = r; break;
                    case "APIRETRYDELAYSECONDS":
                        if (int.TryParse(parts[1].Trim(), out int d)) numApiRetryDelay.Value = d; break;
                }
            }
        }

        private void LoadLogSettings()
        {
            var logDays = ConfigurationManager.AppSettings["LogRetentionDays"];
            if (!string.IsNullOrEmpty(logDays) && int.TryParse(logDays, out int days))
                numLogRetention.Value = days;
        }

        // ════════════════════════════════════════════════════════════════════
        //  CHECKBOX: Same as OPD
        // ════════════════════════════════════════════════════════════════════

        private void ChkSameAsOPD_CheckedChanged(object sender, EventArgs e)
        {
            if (chkSameAsOPD.Checked)
            {
                // copy OPD → IPD (ยกเว้น Database ให้แก้เองได้)
                txtServerIPD.Text = txtServer.Text;
                txtUserIdIPD.Text = txtUserId.Text;
                txtPasswordIPD.Text = txtPassword.Text;
                cmbEncodingIPD.SelectedIndex = cmbEncoding.SelectedIndex;

                txtServerIPD.Enabled = false;
                txtUserIdIPD.Enabled = false;
                txtPasswordIPD.Enabled = false;
                cmbEncodingIPD.Enabled = false;
            }
            else
            {
                txtServerIPD.Enabled = true;
                txtUserIdIPD.Enabled = true;
                txtPasswordIPD.Enabled = true;
                cmbEncodingIPD.Enabled = true;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  TEST CONNECTION
        // ════════════════════════════════════════════════════════════════════

        private async void BtnTestConnection_Click(object sender, EventArgs e)
        {
            await TestConnection(
                txtServer.Text, txtDatabase.Text, txtUserId.Text, txtPassword.Text,
                cmbEncoding.SelectedIndex == 1 ? "tis620" : "utf8",
                btnTestConnection, lblConnectionStatus, "OPD");
        }

        // ⭐ Test IPD
        private async void BtnTestConnectionIPD_Click(object sender, EventArgs e)
        {
            await TestConnection(
                txtServerIPD.Text, txtDatabaseIPD.Text, txtUserIdIPD.Text, txtPasswordIPD.Text,
                cmbEncodingIPD.SelectedIndex == 1 ? "tis620" : "utf8",
                btnTestConnectionIPD, lblConnectionStatusIPD, "IPD");
        }

        private async Task TestConnection(
            string server, string database, string userId, string password, string charset,
            Button btn, Label statusLabel, string label)
        {
            if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database) ||
                string.IsNullOrWhiteSpace(userId))
            {
                MessageBox.Show("กรุณากรอกข้อมูลให้ครบถ้วน", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btn.Enabled = false;
            statusLabel.Text = "⏳ Testing...";
            statusLabel.ForeColor = Color.Orange;

            var connStr = $"Server={server};Database={database};User Id={userId};Password={password};Charset={charset};";

            try
            {
                await Task.Run(() =>
                {
                    using (var conn = new MySqlConnection(connStr))
                    {
                        conn.Open();
                        using (var cmd = new MySqlCommand("SELECT VERSION()", conn))
                        {
                            var ver = cmd.ExecuteScalar()?.ToString() ?? "Unknown";
                            this.Invoke(new Action(() =>
                            {
                                statusLabel.Text = $"✅ Connected!";
                                statusLabel.ForeColor = Color.Green;
                                MessageBox.Show(
                                    $"✅ {label} Database เชื่อมต่อสำเร็จ!\n\n" +
                                    $"Server: {server}\nDatabase: {database}\nMySQL: {ver}",
                                    $"{label} Connection OK",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }));
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                statusLabel.Text = "❌ Failed";
                statusLabel.ForeColor = Color.Red;
                MessageBox.Show(
                    $"❌ {label} Database เชื่อมต่อล้มเหลว!\n\n{ex.Message}",
                    $"{label} Connection Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btn.Enabled = true;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  SAVE
        // ════════════════════════════════════════════════════════════════════

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ValidateInputs()) return;

                var result = MessageBox.Show(
                    "คุณต้องการบันทึกการตั้งค่าทั้งหมดหรือไม่?\n\n" +
                    "📁 ไฟล์ที่จะถูกแก้ไข:\n" +
                    "   • Connection\\connectdatabase.ini\n" +
                    "   • Connection\\connectdatabase_ipd.ini\n" +
                    "   • Config\\appsettings.ini\n" +
                    "   • App.config\n\n" +
                    "⚠️ หมายเหตุ: การตั้งค่าบางอย่างอาจต้อง Restart โปรแกรม",
                    "Confirm Save", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result != DialogResult.Yes) return;

                SaveDatabaseSettings();
                SaveDatabaseIPDSettings(); // ⭐
                SaveAPISettings();
                SaveLogSettings();

                SettingsChanged = true;

                MessageBox.Show(
                    "✅ บันทึกการตั้งค่าทั้งหมดสำเร็จ!\n\n" +
                    $"✓ {ConnFolder}\\{ConnFile}\n" +
                    $"✓ {ConnFolder}\\{ConnFileIPD}\n" +
                    $"✓ {ConfigFolder}\\{ConfigFile}\n" +
                    "✓ App.config",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ เกิดข้อผิดพลาดในการบันทึก:\n\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveDatabaseSettings()
        {
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConnFolder);
            Directory.CreateDirectory(dir);

            string charset = cmbEncoding.SelectedIndex == 1 ? "TIS-620" : "UTF8";
            var sb = new StringBuilder();
            sb.AppendLine($"Server={txtServer.Text.Trim()};");
            sb.AppendLine($"Database={txtDatabase.Text.Trim()};");
            sb.AppendLine($"User Id={txtUserId.Text.Trim()};");
            sb.AppendLine($"Password={txtPassword.Text.Trim()};");
            sb.AppendLine($"Charset={charset};");
            File.WriteAllText(Path.Combine(dir, ConnFile), sb.ToString());
        }

        // ⭐ บันทึก IPD
        private void SaveDatabaseIPDSettings()
        {
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConnFolder);
            Directory.CreateDirectory(dir);

            string charset = cmbEncodingIPD.SelectedIndex == 1 ? "TIS-620" : "UTF8";
            var sb = new StringBuilder();
            sb.AppendLine($"Server={txtServerIPD.Text.Trim()};");
            sb.AppendLine($"Database={txtDatabaseIPD.Text.Trim()};");
            sb.AppendLine($"User Id={txtUserIdIPD.Text.Trim()};");
            sb.AppendLine($"Password={txtPasswordIPD.Text.Trim()};");
            sb.AppendLine($"Charset={charset};");
            File.WriteAllText(Path.Combine(dir, ConnFileIPD), sb.ToString());
        }

        private void SaveAPISettings()
        {
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFolder);
            Directory.CreateDirectory(dir);

            var sb = new StringBuilder();
            sb.AppendLine("# ===== API SETTINGS =====");
            sb.AppendLine($"ApiEndpoint={txtApiEndpoint.Text.Trim()}");
            sb.AppendLine($"ApiTimeoutSeconds={numApiTimeout.Value}");
            sb.AppendLine($"ApiRetryAttempts={numApiRetry.Value}");
            sb.AppendLine($"ApiRetryDelaySeconds={numApiRetryDelay.Value}");
            sb.AppendLine();
            sb.AppendLine("# ===== PROCESSING SETTINGS =====");
            sb.AppendLine("ProcessingIntervalSeconds=30");
            sb.AppendLine("MaxProcessingBatchSize=50");
            sb.AppendLine("AutoStart=true");
            File.WriteAllText(Path.Combine(dir, ConfigFile), sb.ToString());
        }

        private void SaveLogSettings()
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (config.AppSettings.Settings["LogRetentionDays"] == null)
                config.AppSettings.Settings.Add("LogRetentionDays", numLogRetention.Value.ToString());
            else
                config.AppSettings.Settings["LogRetentionDays"].Value = numLogRetention.Value.ToString();
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        // ════════════════════════════════════════════════════════════════════
        //  VALIDATION
        // ════════════════════════════════════════════════════════════════════

        private bool ValidateDatabaseInputs()
        {
            if (string.IsNullOrWhiteSpace(txtServer.Text))
            {
                Show("กรุณาระบุ OPD Server", tabDatabase, txtServer); return false;
            }
            if (string.IsNullOrWhiteSpace(txtDatabase.Text))
            {
                Show("กรุณาระบุ OPD Database", tabDatabase, txtDatabase); return false;
            }
            if (string.IsNullOrWhiteSpace(txtUserId.Text))
            {
                Show("กรุณาระบุ OPD User ID", tabDatabase, txtUserId); return false;
            }
            if (string.IsNullOrWhiteSpace(txtServerIPD.Text))
            {
                Show("กรุณาระบุ IPD Server", tabDatabaseIPD, txtServerIPD); return false;
            }
            if (string.IsNullOrWhiteSpace(txtDatabaseIPD.Text))
            {
                Show("กรุณาระบุ IPD Database", tabDatabaseIPD, txtDatabaseIPD); return false;
            }
            if (string.IsNullOrWhiteSpace(txtUserIdIPD.Text))
            {
                Show("กรุณาระบุ IPD User ID", tabDatabaseIPD, txtUserIdIPD); return false;
            }
            return true;
        }

        private void Show(string msg, TabPage tab, Control focus)
        {
            MessageBox.Show(msg, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            tabControl.SelectedTab = tab;
            focus.Focus();
        }

        private bool ValidateInputs()
        {
            if (!ValidateDatabaseInputs()) return false;
            if (string.IsNullOrWhiteSpace(txtApiEndpoint.Text))
            {
                Show("กรุณาระบุ API Endpoint", tabAPI, txtApiEndpoint); return false;
            }
            if (!Uri.TryCreate(txtApiEndpoint.Text, UriKind.Absolute, out _))
            {
                Show("API Endpoint ไม่ถูกต้อง", tabAPI, txtApiEndpoint); return false;
            }
            return true;
        }

        // ════════════════════════════════════════════════════════════════════
        //  CANCEL
        // ════════════════════════════════════════════════════════════════════

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                "คุณต้องการยกเลิกการเปลี่ยนแปลงหรือไม่?\nการตั้งค่าที่แก้ไขจะไม่ถูกบันทึก",
                "Confirm Cancel", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }
    }
}