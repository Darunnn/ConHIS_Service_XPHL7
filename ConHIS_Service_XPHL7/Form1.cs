using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ConHIS_Service_XPHL7.Configuration;
using ConHIS_Service_XPHL7.Services;
using ConHIS_Service_XPHL7.Utils;
using static ConHIS_Service_XPHL7.Services.SimpleHL7FileProcessor;
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
                testHL7Button.Enabled = false;
                manualCheckButton.Enabled = false;
            }
            else
            {
                StopBackgroundService();
                testHL7Button.Enabled = true;
                manualCheckButton.Enabled = true;
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

                    // หาโฟลเดอร์เริ่มต้น
                    var searchFolders = new[]
                    {
                Path.Combine(Application.StartupPath, "TestData"),
                Application.StartupPath,
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

                    string initialDirectory = Application.StartupPath;
                    foreach (var folder in searchFolders)
                    {
                        if (Directory.Exists(folder))
                        {
                            initialDirectory = folder;
                            break;
                        }
                    }
                    openFileDialog.InitialDirectory = initialDirectory;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        var filePath = openFileDialog.FileName;
                        var fileName = Path.GetFileName(filePath);

                        // ตรวจสอบว่ามี API Endpoint หรือไม่
                        if (string.IsNullOrEmpty(AppConfig.ApiEndpoint))
                        {
                            _logger.LogError("API Endpoint is not configured!");
                            UpdateStatus("Error: API Endpoint not configured");
                            return;
                        }

                        // ส่ง API เสมอ
                        var sendToApi = true;

                        UpdateStatus($"Check Testing HL7 file: {fileName}...");
                        testHL7Button.Enabled = false;
                        manualCheckButton.Enabled = false;
                        startStopButton.Enabled = false;
                        // ประมวลผลในพื้นหลัง
                        HL7TestResult result = null;
                        await Task.Run(() =>
                        {
                            result = _hl7FileProcessor.ProcessAndSendHL7File(filePath, sendToApi);
                        });

                        // Log ผลลัพธ์
                        if (result != null)
                        {
                            if (result.Success)
                            {                      
                                UpdateStatus($"HL7 test completed - {fileName} (Check log for details)");
                            }
                            else
                            {
                                UpdateStatus($"HL7 test failed - {fileName} (Check log for details)");
                            }
                        }
                        else
                        {
                            UpdateStatus("HL7 test failed - Check log for details");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("HL7 file test error", ex);
                UpdateStatus($"HL7 test error: {ex.Message} (Check log for details)");
            }
            finally
            {
                testHL7Button.Enabled = true;
                manualCheckButton.Enabled = true;
                startStopButton.Enabled = true;
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
            testHL7Button.Enabled = false;
            startStopButton.Enabled = false;

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
                testHL7Button.Enabled = true;
                startStopButton.Enabled = true;
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