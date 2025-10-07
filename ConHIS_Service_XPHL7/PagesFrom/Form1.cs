using ConHIS_Service_XPHL7.Configuration;
using ConHIS_Service_XPHL7.Models;
using ConHIS_Service_XPHL7.Services;
using ConHIS_Service_XPHL7.Utils;
using Org.BouncyCastle.Utilities.Encoders;
using System;
using System.Data;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
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
        private SimpleHL7FileProcessor _hl7FileProcessor;

        // Background service components
        private Timer _backgroundTimer;
        private bool _isProcessing = false;
        private readonly int _intervalSeconds = 60;

        // DataTable for DataGridView
        private DataTable _processedDataTable;

        // เก็บ HL7Message ที่เชื่อมกับแต่ละแถว
        private System.Collections.Generic.Dictionary<int, HL7Message> _rowHL7Data = new System.Collections.Generic.Dictionary<int, HL7Message>();

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;
            _logger = new LogManager();
            _hl7FileProcessor = new SimpleHL7FileProcessor();
        }
    
        private void InitializeDataTable()
        {
            _processedDataTable = new DataTable();
            _processedDataTable.Columns.Add("Time Check", typeof(string));
            _processedDataTable.Columns.Add("Transaction DateTime", typeof(string));
            _processedDataTable.Columns.Add("Order No", typeof(string));
            _processedDataTable.Columns.Add("HN", typeof(string));
            _processedDataTable.Columns.Add("Patient Name", typeof(string));
            _processedDataTable.Columns.Add("Sex", typeof(string));
            _processedDataTable.Columns.Add("DateOfBirth", typeof(string));
            _processedDataTable.Columns.Add("FinancialClass", typeof(string));
            _processedDataTable.Columns.Add("OrderControl", typeof(string));
            _processedDataTable.Columns.Add("Status", typeof(string));
            _processedDataTable.Columns.Add("API Response", typeof(string));

            dataGridView.DataSource = _processedDataTable;
            dataGridView.CellDoubleClick += DataGridView_CellDoubleClick;

           

            dataGridView.Refresh();

            try
            {
                if (dataGridView.Columns.Count >= 9)
                {
                    dataGridView.Columns["Time Check"].Width = 80;
                    dataGridView.Columns["Transaction DateTime"].Width = 150;
                    dataGridView.Columns["Order No"].Width = 100;
                    dataGridView.Columns["HN"].Width = 80;
                    dataGridView.Columns["Patient Name"].Width = 150;
                    dataGridView.Columns["Sex"].Width = 100;
                    dataGridView.Columns["DateOfBirth"].Width = 200;
                    dataGridView.Columns["FinancialClass"].Width = 150;
                    dataGridView.Columns["OrderControl"].Width = 80;
                    dataGridView.Columns["Status"].Width = 100;
                    dataGridView.Columns["API Response"].Width = 300; // แค่กำหนด Width เท่านั้น
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError("Error setting column widths", ex);
            }

            UpdateRecordCount();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _logger.LogInfo("Start Interface");
            UpdateStatus("Initializing...");

            // Initialize DataTable first
            InitializeDataTable();

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
                testHL7Button.Enabled = true;
                manualCheckButton.Enabled = true;
                exportButton.Enabled = true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to initialize", ex);
                UpdateStatus($"Error: {ex.Message}");
                MessageBox.Show($"Failed to initialize: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #region Test HL7 File
        private async void TestHL7Button_Click(object sender, EventArgs e)
        {
            try
            {
                using (var openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Title = "Select HL7 File to Test";
                    openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";

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

                        if (string.IsNullOrEmpty(AppConfig.ApiEndpoint))
                        {
                            _logger.LogError("API Endpoint is not configured!");
                            UpdateStatus("Error: API Endpoint not configured");
                            return;
                        }

                        var sendToApi = true;

                        UpdateStatus($"Testing HL7 file: {fileName}...");
                        testHL7Button.Enabled = false;
                        manualCheckButton.Enabled = false;
                        startStopButton.Enabled = false;
                        exportButton.Enabled = false;

                        HL7TestResult result = null;
                        await Task.Run(() =>
                        {
                            result = _hl7FileProcessor.ProcessAndSendHL7File(filePath, sendToApi);
                        });

                        if (result != null)
                        {
                            // ดึงข้อมูลจาก HL7Message
                            string TransactionDateTime = result.ParsedMessage?.CommonOrder?.TransactionDateTime != null
                                    ? ((DateTime)result.ParsedMessage?.CommonOrder?.TransactionDateTime)
                                        .ToString("yyyy-MM-dd HH:mm:ss")
                                    : null;
                            string orderNo = result.ParsedMessage?.CommonOrder?.PlacerOrderNumber ?? "N/A";
                            string hn = result.ParsedMessage?.PatientIdentification?.PatientIDExternal ??
                                       result.ParsedMessage?.PatientIdentification?.PatientIDInternal ?? "N/A";

                            // สร้างชื่อผู้ป่วย
                            string patientName = "N/A";
                            if (result.ParsedMessage?.PatientIdentification?.OfficialName != null)
                            {
                                var name = result.ParsedMessage.PatientIdentification.OfficialName;
                                patientName = $"{name.Prefix ?? ""} {name.FirstName ?? ""} {name.LastName ?? ""}".Trim();
                                if (string.IsNullOrWhiteSpace(patientName)) patientName = "N/A";
                            }

                            // ดึงข้อมูลยาจาก RXD แรก (ถ้ามี)
                            string sex = result.ParsedMessage?.PatientIdentification?.Sex ?? "N/A";
                            string DateOfBirth = result.ParsedMessage?.PatientIdentification?.DateOfBirth != null
                                ? ((DateTime)result.ParsedMessage.PatientIdentification.DateOfBirth)
                                    .ToString("yyyy-MM-dd")
                                : null;
                            string FinancialClass = "N/A";
                            if (result.ParsedMessage?.PatientVisit?.FinancialClass != null)
                            {
                                var financialclass = result.ParsedMessage.PatientVisit.FinancialClass;
                                FinancialClass = $"{financialclass.ID ?? ""} {financialclass.Name ?? ""}".Trim();
                                if (string.IsNullOrWhiteSpace(FinancialClass)) FinancialClass = "N/A";
                            }
                            
                            string OrderControl = result.ParsedMessage?.CommonOrder?.OrderControl ?? "N/A";

                            // เพิ่มข้อมูลลงตาราง
                            AddRowToGrid(
                                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                TransactionDateTime,
                                orderNo,
                                hn,
                                patientName,
                                sex,
                                DateOfBirth,
                                FinancialClass,
                                OrderControl,
                                result.Success ? "Success" : "Failed",
                                result.ApiResponse ?? result.ErrorMessage ?? "N/A",
                                result.ParsedMessage  // ส่ง HL7Message ไปด้วย
                            );
                           



                            if (result.Success)
                            {
                                UpdateStatus($"HL7 test completed - {fileName}");
                            }
                            else
                            {
                                UpdateStatus($"HL7 test failed - {fileName}");
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
                UpdateStatus($"HL7 test error: {ex.Message}");
            }
            finally
            {
                testHL7Button.Enabled = true;
                manualCheckButton.Enabled = true;
                startStopButton.Enabled = true;
                exportButton.Enabled = true;
            }
        }
        #endregion

        #region Export
        private void ExportButton_Click(object sender, EventArgs e)
        {
            if (_processedDataTable.Rows.Count == 0)
            {
                MessageBox.Show("No data to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                using (var saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                    saveFileDialog.FileName = $"DrugDispense_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                    saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        ExportToCSV(saveFileDialog.FileName);
                        MessageBox.Show($"Data exported successfully to:\n{saveFileDialog.FileName}",
                            "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        _logger.LogInfo($"Data exported to: {saveFileDialog.FileName}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Export error", ex);
                MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportToCSV(string filePath)
        {
            var csv = new StringBuilder();

            // Headers
            var headers = new string[_processedDataTable.Columns.Count];
            for (int i = 0; i < _processedDataTable.Columns.Count; i++)
            {
                headers[i] = _processedDataTable.Columns[i].ColumnName;
            }
            csv.AppendLine(string.Join(",", headers));

            // Data rows
            foreach (DataRow row in _processedDataTable.Rows)
            {
                var fields = new string[_processedDataTable.Columns.Count];
                for (int i = 0; i < _processedDataTable.Columns.Count; i++)
                {
                    var value = row[i].ToString();
                    // Escape commas and quotes in CSV
                    if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                    {
                        value = $"\"{value.Replace("\"", "\"\"")}\"";
                    }
                    fields[i] = value;
                }
                csv.AppendLine(string.Join(",", fields));
            }

            File.WriteAllText(filePath, csv.ToString(), Encoding.UTF8);
        }
        #endregion

        #region GridView
        private void AddRowToGrid(string time, string TransactionDateTime, string orderNo, string hn, string patientName,
            string sex, string DateOfBirth,string FinancialClass, string OrderControl, string status, string apiResponse, HL7Message hl7Data)
        {
            if (dataGridView.InvokeRequired)
            {
                dataGridView.Invoke(new Action(() =>
                {
                    int rowIndex = _processedDataTable.Rows.Count;
                    _processedDataTable.Rows.Add(time, TransactionDateTime, orderNo, hn, patientName, sex, DateOfBirth, FinancialClass, OrderControl, status, apiResponse);

                    // เก็บ HL7Message ที่เชื่อมกับแถวนี้
                    if (hl7Data != null)
                    {
                        _rowHL7Data[rowIndex] = hl7Data;
                    }

                    UpdateRecordCount();

                    // Scroll ไปแถวล่างสุด
                    if (dataGridView.Rows.Count > 0)
                    {
                        dataGridView.FirstDisplayedScrollingRowIndex = dataGridView.Rows.Count - 1;
                    }

                    // สีตามสถานะ
                    var lastRow = dataGridView.Rows[dataGridView.Rows.Count - 1];
                    if (status == "Success")
                    {
                        lastRow.DefaultCellStyle.BackColor = System.Drawing.Color.LightGreen;
                    }
                    else if (status == "Failed")
                    {
                        lastRow.DefaultCellStyle.BackColor = System.Drawing.Color.LightCoral;
                    }
                }));
            }
            else
            {
                int rowIndex = _processedDataTable.Rows.Count;
                _processedDataTable.Rows.Add(time, TransactionDateTime, orderNo, hn, patientName, sex, DateOfBirth, FinancialClass, OrderControl, status, apiResponse);

                // เก็บ HL7Message ที่เชื่อมกับแถวนี้
                if (hl7Data != null)
                {
                    _rowHL7Data[rowIndex] = hl7Data;
                }

                UpdateRecordCount();

                if (dataGridView.Rows.Count > 0)
                {
                    dataGridView.FirstDisplayedScrollingRowIndex = dataGridView.Rows.Count - 1;
                }

                var lastRow = dataGridView.Rows[dataGridView.Rows.Count - 1];
                if (status == "Success")
                {
                    lastRow.DefaultCellStyle.BackColor = System.Drawing.Color.LightGreen;
                }
                else if (status == "Failed")
                {
                    lastRow.DefaultCellStyle.BackColor = System.Drawing.Color.LightCoral;
                }
            }
        }
        
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopBackgroundService();
            _logger.LogInfo("Application closing - Background service stopped");
        }

        private void DataGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < dataGridView.Rows.Count)
            {
                try
                {
                    // ดึง Order Number จากคอลัมน์
                    string orderNo = dataGridView.Rows[e.RowIndex].Cells["Order No"].Value?.ToString() ?? "N/A";

                    // ตรวจสอบว่ามี HL7Message สำหรับแถวนี้หรือไม่
                    if (_rowHL7Data.ContainsKey(e.RowIndex))
                    {
                        var hl7Message = _rowHL7Data[e.RowIndex];

                        // เปิดฟอร์มแสดงรายละเอียด
                        var detailForm = new HL7DetailForm(hl7Message, orderNo);
                        detailForm.ShowDialog();
                    }
                    else
                    {
                        MessageBox.Show("No HL7 data available for this record.", "Information",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error showing HL7 detail", ex);
                    MessageBox.Show($"Error displaying details: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        #endregion

        #region Start/Stop Service Manual and Auto
        private void StartStopButton_Click(object sender, EventArgs e)
        {
            if (_backgroundTimer == null)
            {
                StartBackgroundService();
                testHL7Button.Enabled = false;
                manualCheckButton.Enabled = false;
                exportButton.Enabled = false;
            }
            else
            {
                StopBackgroundService();
                testHL7Button.Enabled = true;
                manualCheckButton.Enabled = true;
                exportButton.Enabled = true;
            }
        }

        private async void ManualCheckButton_Click(object sender, EventArgs e)
        {
            if (!_isProcessing)
            {
                await CheckPendingOrders(isManual: true);
            }
        }

        private void StartBackgroundService()
        {
            var intervalMs = _intervalSeconds * 1000;
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

        private async Task CheckPendingOrders(bool isManual)
        {
            if (_isProcessing) return;

            _isProcessing = true;

            try
            {

                if (isManual)
                {
                    testHL7Button.Enabled = false;
                    startStopButton.Enabled = false;
                    exportButton.Enabled = false;
                }

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
                        _processor.ProcessPendingOrders(
                            msg =>
                            {
                                _logger.LogInfo($"Background processing: {msg}");
                            },
                            result =>
                            {
                                var hl7Message = result.ParsedMessage;
                                
                                string orderNo = hl7Message?.CommonOrder?.PlacerOrderNumber ?? "N/A";
                                string hn = hl7Message?.PatientIdentification?.PatientIDExternal ??
                                           hl7Message?.PatientIdentification?.PatientIDInternal ?? "N/A";

                                string TransactionDateTime = hl7Message?.CommonOrder?.TransactionDateTime != null
                                    ? ((DateTime)hl7Message.CommonOrder.TransactionDateTime)
                                        .ToString("yyyy-MM-dd HH:mm:ss")
                                    : null;

                                string patientName = "N/A";
                                if (hl7Message?.PatientIdentification?.OfficialName != null)
                                {
                                    var name = hl7Message.PatientIdentification.OfficialName;
                                    patientName = $"{name.Prefix ?? ""} {name.FirstName ?? ""} {name.LastName ?? ""}".Trim();
                                }

                                string sex = hl7Message?.PatientIdentification?.Sex ?? "N/A";
                                string DateOfBirth = hl7Message?.PatientIdentification?.DateOfBirth != null
                                    ? ((DateTime)hl7Message.PatientIdentification.DateOfBirth)
                                        .ToString("yyyy-MM-dd")
                                    : null;
                                string FinancialClass = "N/A";
                                if (hl7Message?.PatientVisit?.FinancialClass != null)
                                {
                                    var financialclass = hl7Message.PatientVisit.FinancialClass;
                                    FinancialClass = $"{financialclass.ID ?? ""} {financialclass.Name ?? ""}".Trim();
                                    if (string.IsNullOrWhiteSpace(FinancialClass)) FinancialClass = "N/A";
                                }
                                string OrderControl = hl7Message?.CommonOrder?.OrderControl ?? "N/A";



                                AddRowToGrid(
                                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                    TransactionDateTime,
                                    orderNo,
                                    hn,
                                    patientName,
                                    sex,
                                    DateOfBirth,
                                    FinancialClass,
                                    OrderControl,
                                    result.Success ? "Success" : "Failed",
                                    result.ApiResponse ?? result.Message ?? "N/A",
                                    hl7Message
                                );
                            }
                        );
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

                // ✅ เปิดปุ่มกลับเมื่อทำงานเสร็จ (เฉพาะ manual)
                if (isManual)
                {
                    this.Invoke(new Action(() =>
                    {
                        testHL7Button.Enabled = true;
                        startStopButton.Enabled = true;
                        exportButton.Enabled = true;
                    }));
                }
            }
        }

        private async void BackgroundTimerCallback(object state)
        {
            if (!_isProcessing)
            {
                await CheckPendingOrders(isManual: false);
            }
        }
        #endregion
       
        #region update
        private void UpdateRecordCount()
        {
            if (recordCountLabel.InvokeRequired)
            {
                recordCountLabel.Invoke(new Action(UpdateRecordCount));
                return;
            }

            recordCountLabel.Text = $"Total Records: {_processedDataTable.Rows.Count}";
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
        #endregion

    }
}