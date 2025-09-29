using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ConHIS_Service_XPHL7.Configuration;
using ConHIS_Service_XPHL7.Services;
using ConHIS_Service_XPHL7.Utils;
using Timer = System.Threading.Timer;

namespace ConHIS_Service_XPHL7
{
    public partial class Form1 : Form
    {
        private AppConfig _appConfig;
        private DatabaseService _databaseService;
        private LogManager _logger;
        private DrugDispenseProcessor _processor;
        private SimpleHL7FileProcessor _hl7FileProcessor; // เพิ่มบรรทัดนี้

        // Background service components
        private Timer _backgroundTimer;
        private bool _isProcessing = false;
        private readonly int _intervalSeconds = 60; // ฟิกทุก 60 วินาที

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;
            _logger = new LogManager();
            _hl7FileProcessor = new SimpleHL7FileProcessor(); // เพิ่มบรรทัดนี้
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _logger.LogInfo("Start Interface");
            UpdateStatus("Initializing...");

            try
            {
                _logger.LogInfo("Loading configuration");
                _appConfig = new AppConfig();
                _appConfig.LoadConfiguration();
                _logger.LogInfo("Configuration loaded");

                _logger.LogInfo("Connecting to database");
                _databaseService = new DatabaseService(_appConfig.ConnectionString);
                _logger.LogInfo("DatabaseService initialized");

                var apiService = new ApiService(AppConfig.ApiEndpoint);
                var hl7Service = new HL7Service();
                _processor = new DrugDispenseProcessor(_databaseService, hl7Service, apiService);

                UpdateStatus("Ready - Service Stopped");
                startStopButton.Enabled = true;
                testHL7Button.Enabled = true; // เพิ่มบรรทัดนี้
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to initialize", ex);
                UpdateStatus($"Error: {ex.Message}");
                MessageBox.Show($"Failed to initialize: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StartStopButton_Click(object sender, EventArgs e)
        {
            if (_backgroundTimer == null)
            {
                StartBackgroundService();
            }
            else
            {
                StopBackgroundService();
            }
        }

        private async void ManualCheckButton_Click(object sender, EventArgs e)
        {
            if (!_isProcessing)
            {
                await CheckPendingOrders();
            }
        }

        // เพิ่มฟังก์ชันใหม่ทั้งหมดนี้
        private async void TestHL7Button_Click(object sender, EventArgs e)
        {
            try
            {
                using (var openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Title = "Select HL7 File to Test";
                    openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                    openFileDialog.InitialDirectory = Application.StartupPath;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        var filePath = openFileDialog.FileName;
                        var fileName = Path.GetFileName(filePath);

                        UpdateStatus($"Testing HL7 file: {fileName}...");
                        testHL7Button.Enabled = false;

                        // ประมวลผลไฟล์ในพื้นหลัง
                        await Task.Run(() =>
                        {
                            _logger.LogInfo($"Starting HL7 file test: {filePath}");

                            // ประมวลผลไฟล์
                            var message = _hl7FileProcessor.ProcessHL7File(filePath);

                            if (message != null)
                            {
                                _logger.LogInfo($"HL7 file test successful: {fileName}");

                                this.Invoke(new Action(() =>
                                {
                                    try
                                    {
                                        // แสดงข้อมูลสำคัญใน MessageBox
                                        var patientInfo = message.PatientIdentification;
                                        var drugCount = message.PharmacyDispense?.Count ?? 0;
                                        var allergyCount = message.Allergies?.Count ?? 0;

                                        var summary = $"HL7 File Test Result:\n\n" +
                                                    $"File: {fileName}\n" +
                                                    $"Patient HN: {patientInfo?.PatientIDInternal ?? "N/A"}\n" +
                                                    $"Patient Name: {patientInfo?.OfficialName?.FirstName ?? ""} {patientInfo?.OfficialName?.LastName ?? ""}\n" +
                                                    $"DOB: {patientInfo?.DateOfBirth?.ToString("yyyy-MM-dd") ?? "N/A"}\n" +
                                                    $"Sex: {patientInfo?.Sex ?? "N/A"}\n" +
                                                    $"Total Drugs: {drugCount}\n" +
                                                    $"Total Allergies: {allergyCount}\n\n" +
                                                    $"✅ Processing successful!\n" +
                                                    $"Check log files for detailed information.";

                                        MessageBox.Show(summary, "HL7 Test Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                    catch (Exception displayEx)
                                    {
                                        _logger.LogError($"Error displaying results: {displayEx.Message}", displayEx);
                                        MessageBox.Show($"✅ HL7 file processed successfully: {fileName}\n\nCheck log files for details.",
                                                      "HL7 Test Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                }));
                            }
                            else
                            {
                                _logger.LogError($"HL7 file test failed: {fileName}");

                                this.Invoke(new Action(() =>
                                {
                                    MessageBox.Show($"❌ Failed to process HL7 file: {fileName}\n\nCheck error logs for details.",
                                                  "HL7 Test Result", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }));
                            }
                        });

                        UpdateStatus("HL7 file test completed");
                        testHL7Button.Enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("HL7 file test error", ex);
                UpdateStatus($"HL7 test error: {ex.Message}");
                MessageBox.Show($"Error testing HL7 file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                testHL7Button.Enabled = true;
            }
        }

        private void StartBackgroundService()
        {
            var intervalMs = _intervalSeconds * 1000; // 60 วินาที
            _backgroundTimer = new Timer(BackgroundTimerCallback, null, 0, intervalMs);

            startStopButton.Text = "Stop Service";
            UpdateStatus($"Service Running - Checking every {_intervalSeconds} seconds");
            _logger.LogInfo($"Background service started with {_intervalSeconds}s interval");
        }

        private void StopBackgroundService()
        {
            _backgroundTimer?.Dispose();
            _backgroundTimer = null;

            startStopButton.Text = "Start Service";
            UpdateStatus("Service Stopped");
            _logger.LogInfo("Background service stopped");
        }

        private async void BackgroundTimerCallback(object state)
        {
            if (!_isProcessing)
            {
                await CheckPendingOrders();
            }
        }

        private async Task CheckPendingOrders()
        {
            if (_isProcessing) return;

            _isProcessing = true;

            try
            {
                UpdateLastCheck();
                _logger.LogInfo("Background check: Starting pending orders check");

                var pending = await Task.Run(() => _databaseService.GetPendingDispenseData());
                _logger.LogInfo($"Background check: Found {pending.Count} pending orders");

                this.Invoke(new Action(() =>
                {
                    this.Text = $"ConHIS Service - Pending: {pending.Count}";
                    if (pending.Count > 0)
                    {
                        UpdateStatus($"Processing {pending.Count} pending orders...");
                    }
                }));

                if (pending.Count > 0)
                {
                    await Task.Run(() =>
                    {
                        _processor.ProcessPendingOrders(msg =>
                        {
                            _logger.LogInfo($"Background processing: {msg}");
                        });
                    });

                    _logger.LogInfo("Background check: Completed processing pending orders");

                    this.Invoke(new Action(() =>
                    {
                        if (_backgroundTimer != null)
                        {
                            UpdateStatus($"Service Running - Last processed {pending.Count} orders");
                        }
                        else
                        {
                            UpdateStatus($"Manual check completed - Processed {pending.Count} orders");
                        }
                    }));
                }
                else
                {
                    this.Invoke(new Action(() =>
                    {
                        if (_backgroundTimer != null)
                        {
                            UpdateStatus("Service Running - No pending orders");
                        }
                        else
                        {
                            UpdateStatus("Manual check completed - No pending orders");
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Background check error", ex);

                this.Invoke(new Action(() =>
                {
                    UpdateStatus($"Error: {ex.Message}");
                }));
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private void UpdateStatus(string status)
        {
            if (statusLabel.InvokeRequired)
            {
                statusLabel.Invoke(new Action<string>(UpdateStatus), status);
                return;
            }

            statusLabel.Text = $"Status: {status}";
            _logger.LogInfo($"Status: {status}");
        }

        private void UpdateLastCheck()
        {
            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            if (lastCheckLabel.InvokeRequired)
            {
                lastCheckLabel.Invoke(new Action(() =>
                {
                    lastCheckLabel.Text = $"Last Check: {now}";
                }));
                return;
            }

            lastCheckLabel.Text = $"Last Check: {now}";
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopBackgroundService();
            _logger.LogInfo("Application closing - Background service stopped");
        }
    }
}