using System;
using System.Configuration;
using System.Windows.Forms;

namespace ConHIS_Service_XPHL7.PagesFrom
{
    public partial class SettingsForm : Form
    {
        public int LogRetentionDays { get; private set; }
        public bool SaveToConfig { get; private set; }

        public SettingsForm(int currentDays)
        {
            InitializeComponent();
            LogRetentionDays = currentDays;
            logRetentionDaysNumeric.Value = currentDays;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            try
            {
                LogRetentionDays = (int)logRetentionDaysNumeric.Value;

                // ถามว่าต้องการบันทึกถาวรลง App.config หรือไม่
                var result = MessageBox.Show(
                    $"คุณต้องการบันทึกค่านี้อย่างไร?\n\n" +
                    $"✅ Yes = บันทึกถาวรลง App.config\n" +
                    $"   (จะใช้ค่านี้ทุกครั้งที่เปิดโปรแกรม)\n\n" +
                    $"❌ No = ใช้เฉพาะ Session นี้\n" +
                    $"   (เมื่อปิดโปรแกรมจะกลับเป็นค่าเดิม)\n\n" +
                    $"จำนวนวันเก็บ Log: {LogRetentionDays} วัน",
                    "Save Settings",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Cancel)
                {
                    return; // ไม่บันทึก
                }

                SaveToConfig = (result == DialogResult.Yes);

                if (SaveToConfig)
                {
                    // บันทึกลง App.config
                    var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                    if (config.AppSettings.Settings["LogRetentionDays"] == null)
                    {
                        config.AppSettings.Settings.Add("LogRetentionDays", LogRetentionDays.ToString());
                    }
                    else
                    {
                        config.AppSettings.Settings["LogRetentionDays"].Value = LogRetentionDays.ToString();
                    }

                    config.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("appSettings");

                    MessageBox.Show(
                        $"✅ บันทึกการตั้งค่าถาวรสำเร็จ!\n\n" +
                        $"จำนวนวันเก็บ Log: {LogRetentionDays} วัน\n" +
                        $"บันทึกใน: App.config\n\n" +
                        $"📌 ค่านี้จะถูกใช้ทุกครั้งที่เปิดโปรแกรม",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                else
                {
                    MessageBox.Show(
                        $"✅ ปรับการตั้งค่าชั่วคราวสำเร็จ!\n\n" +
                        $"จำนวนวันเก็บ Log: {LogRetentionDays} วัน\n" +
                        $"ใช้ได้เฉพาะ: Session นี้เท่านั้น\n\n" +
                        $"⚠️ เมื่อปิดโปรแกรมแล้วเปิดใหม่\n" +
                        $"   จะกลับไปใช้ค่าเดิมจาก App.config",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"เกิดข้อผิดพลาดในการบันทึกการตั้งค่า:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}