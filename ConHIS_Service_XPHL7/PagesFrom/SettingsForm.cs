﻿using System;
using System.Configuration;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ConHIS_Service_XPHL7.PagesFrom
{
    public partial class SettingsForm : Form
    {
        private const string ConnFolder = "Connection";
        private const string ConnFile = "connectdatabase.ini";
        private const string ConfigFolder = "Config";
        private const string ConfigFile = "appsettings.ini";

        public bool SettingsChanged { get; private set; }

        public SettingsForm()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            try
            {
                // Load Database Settings
                LoadDatabaseSettings();

                // Load API Settings
                LoadAPISettings();

                // Load Log Settings
                LoadLogSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"เกิดข้อผิดพลาดในการโหลดการตั้งค่า:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void LoadDatabaseSettings()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConnFolder, ConnFile);

            if (File.Exists(path))
            {
                var lines = File.ReadAllLines(path);
                foreach (var line in lines)
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
                }
            }
        }

        private void LoadAPISettings()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFolder, ConfigFile);

            if (File.Exists(path))
            {
                var lines = File.ReadAllLines(path);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                    var parts = line.Split('=');
                    if (parts.Length != 2) continue;

                    var key = parts[0].Trim();
                    var value = parts[1].Trim();

                    switch (key.ToUpper())
                    {
                        case "APIENDPOINT":
                            txtApiEndpoint.Text = value;
                            break;
                        case "APITIMEOUTSECONDS":
                            if (int.TryParse(value, out int timeout))
                                numApiTimeout.Value = timeout;
                            break;
                        case "APIRETRYATTEMPTS":
                            if (int.TryParse(value, out int retry))
                                numApiRetry.Value = retry;
                            break;
                        case "APIRETRYDELAYSECONDS":
                            if (int.TryParse(value, out int delay))
                                numApiRetryDelay.Value = delay;
                            break;
                    }
                }
            }
        }

        private void LoadLogSettings()
        {
            // Load from App.config
            var logDays = ConfigurationManager.AppSettings["LogRetentionDays"];
            if (!string.IsNullOrEmpty(logDays) && int.TryParse(logDays, out int days))
            {
                numLogRetention.Value = days;
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                // Validate inputs
                if (!ValidateInputs())
                    return;

                var result = MessageBox.Show(
                    "คุณต้องการบันทึกการตั้งค่าทั้งหมดหรือไม่?\n\n" +
                    "📁 ไฟล์ที่จะถูกแก้ไข:\n" +
                    "   • Connection\\connectdatabase.ini\n" +
                    "   • Config\\appsettings.ini\n" +
                    "   • App.config\n\n" +
                    "⚠️ หมายเหตุ: การตั้งค่าบางอย่างอาจต้อง Restart โปรแกรม",
                    "Confirm Save",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result != DialogResult.Yes)
                    return;

                // Save all settings
                SaveDatabaseSettings();
                SaveAPISettings();
                SaveLogSettings();

                SettingsChanged = true;

                MessageBox.Show(
                    "✅ บันทึกการตั้งค่าทั้งหมดสำเร็จ!\n\n" +
                    "ไฟล์ที่ถูกอัพเดท:\n" +
                    $"✓ {Path.Combine(ConnFolder, ConnFile)}\n" +
                    $"✓ {Path.Combine(ConfigFolder, ConfigFile)}\n" +
                    "✓ App.config\n\n" +
                    "💡 การตั้งค่าจะมีผลในครั้งถัดไปที่โปรแกรมโหลดข้อมูล",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"❌ เกิดข้อผิดพลาดในการบันทึกการตั้งค่า:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private bool ValidateInputs()
        {
            // Validate Database Settings
            if (string.IsNullOrWhiteSpace(txtServer.Text))
            {
                MessageBox.Show("กรุณาระบุ Server", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tabControl.SelectedTab = tabDatabase;
                txtServer.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtDatabase.Text))
            {
                MessageBox.Show("กรุณาระบุ Database", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tabControl.SelectedTab = tabDatabase;
                txtDatabase.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtUserId.Text))
            {
                MessageBox.Show("กรุณาระบุ User ID", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tabControl.SelectedTab = tabDatabase;
                txtUserId.Focus();
                return false;
            }

            // Validate API Settings
            if (string.IsNullOrWhiteSpace(txtApiEndpoint.Text))
            {
                MessageBox.Show("กรุณาระบุ API Endpoint", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tabControl.SelectedTab = tabAPI;
                txtApiEndpoint.Focus();
                return false;
            }

            if (!Uri.TryCreate(txtApiEndpoint.Text, UriKind.Absolute, out _))
            {
                MessageBox.Show("API Endpoint ไม่ถูกต้อง\nกรุณาระบุ URL ที่ถูกต้อง (เช่น https://example.com/api)",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tabControl.SelectedTab = tabAPI;
                txtApiEndpoint.Focus();
                return false;
            }

            return true;
        }

        private void SaveDatabaseSettings()
        {
            var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConnFolder);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var path = Path.Combine(directory, ConnFile);

            var content = new StringBuilder();
            content.AppendLine($"Server={txtServer.Text.Trim()};");
            content.AppendLine($"Database={txtDatabase.Text.Trim()};");
            content.AppendLine($"User Id={txtUserId.Text.Trim()};");
            content.AppendLine($"Password={txtPassword.Text.Trim()};");

            File.WriteAllText(path, content.ToString());
        }

        private void SaveAPISettings()
        {
            var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFolder);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var path = Path.Combine(directory, ConfigFile);

            var content = new StringBuilder();
            content.AppendLine("# ===== API SETTINGS =====");
            content.AppendLine("# URL endpoint สำหรับส่งข้อมูลไป Drug Dispenser API");
            content.AppendLine("# Format: http://{HOST}/api/conHIS/insertPrescription");
            content.AppendLine($"ApiEndpoint={txtApiEndpoint.Text.Trim()}");
            content.AppendLine($"# API timeout in seconds (default: 30)");
            content.AppendLine($"ApiTimeoutSeconds={numApiTimeout.Value}");
            content.AppendLine($"# API retry attempts when failed (default: 3)");
            content.AppendLine($"ApiRetryAttempts={numApiRetry.Value}");
            content.AppendLine($"# API retry delay in seconds (default: 5)");
            content.AppendLine($"ApiRetryDelaySeconds={numApiRetryDelay.Value}");
            content.AppendLine();
            content.AppendLine("# ===== PROCESSING SETTINGS =====");
            content.AppendLine("# ระยะเวลาในการตรวจสอบข้อมูลใหม่ (วินาที)");
            content.AppendLine("# ค่าที่แนะนำ: 15-60 วินาที");
            content.AppendLine("ProcessingIntervalSeconds=30");
            content.AppendLine("# จำนวนสูงสุดของ records ที่ประมวลผลในแต่ละรอบ");
            content.AppendLine("MaxProcessingBatchSize=50");
            content.AppendLine("# เริ่มการประมวลผลอัตโนมัติเมื่อเปิดโปรแกรม (true/false)");
            content.AppendLine("AutoStart=true");

            File.WriteAllText(path, content.ToString());
        }

        private void SaveLogSettings()
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            if (config.AppSettings.Settings["LogRetentionDays"] == null)
            {
                config.AppSettings.Settings.Add("LogRetentionDays", numLogRetention.Value.ToString());
            }
            else
            {
                config.AppSettings.Settings["LogRetentionDays"].Value = numLogRetention.Value.ToString();
            }

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "คุณต้องการยกเลิกการเปลี่ยนแปลงหรือไม่?\n\nการตั้งค่าที่แก้ไขจะไม่ถูกบันทึก",
                "Confirm Cancel",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }
    }
}