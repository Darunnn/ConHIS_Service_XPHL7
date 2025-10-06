using System;
using System.Data;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ConHIS_Service_XPHL7.Configuration;
using ConHIS_Service_XPHL7.Services;
using ConHIS_Service_XPHL7.Utils;
using ConHIS_Service_XPHL7.Models;
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
            _processedDataTable.Columns.Add("Time", typeof(string));
            _processedDataTable.Columns.Add("Order No", typeof(string));
            _processedDataTable.Columns.Add("HN", typeof(string));
            _processedDataTable.Columns.Add("Patient Name", typeof(string));
            _processedDataTable.Columns.Add("Drug Code", typeof(string));
            _processedDataTable.Columns.Add("Drug Name", typeof(string));
            _processedDataTable.Columns.Add("Quantity", typeof(string));
            _processedDataTable.Columns.Add("Status", typeof(string));
            _processedDataTable.Columns.Add("API Response", typeof(string));

            dataGridView.DataSource = _processedDataTable;

            // เพิ่ม event handler สำหรับดับเบิลคลิก
            dataGridView.CellDoubleClick += DataGridView_CellDoubleClick;

            // ปรับความกว้างคอลัมน์ (รอให้ DataGridView โหลดเสร็จก่อน)
            dataGridView.AutoGenerateColumns = true;
            dataGridView.Refresh();

            // ตั้งค่าความกว้างคอลัมน์
            try
            {
                if (dataGridView.Columns.Count >= 9)
                {
                    dataGridView.Columns["Time"].Width = 80;
                    dataGridView.Columns["Order No"].Width = 90;
                    dataGridView.Columns["HN"].Width = 70;
                    dataGridView.Columns["Patient Name"].Width = 120;
                    dataGridView.Columns["Drug Code"].Width = 80;
                    dataGridView.Columns["Drug Name"].Width = 150;
                    dataGridView.Columns["Quantity"].Width = 60;
                    dataGridView.Columns["Status"].Width = 70;
                    dataGridView.Columns["API Response"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
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
                clearButton.Enabled = true;
                exportButton.Enabled = true;
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
                clearButton.Enabled = false;
                exportButton.Enabled = false;
            }
            else
            {
                StopBackgroundService();
                testHL7Button.Enabled = true;
                manualCheckButton.Enabled = true;
                clearButton.Enabled = true;
                exportButton.Enabled = true;
            }
        }

        private async void ManualCheckButton_Click(object sender, EventArgs e)
        {
            if (!_isProcessing)
            {
                await CheckPendingOrdersManual();
            }
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            if (_processedDataTable.Rows.Count > 0)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to clear all {_processedDataTable.Rows.Count} records?",
                    "Confirm Clear",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    _processedDataTable.Clear();
                    _rowHL7Data.Clear();  // ล้าง dictionary ด้วย
                    UpdateRecordCount();
                    _logger.LogInfo("DataGrid cleared by user");
                }
            }
        }

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


        private void AddRowToGrid(string time, string orderNo, string hn, string patientName,
            string drugCode, string drugName, string quantity, string status, string apiResponse, HL7Message hl7Data)
        {
            if (dataGridView.InvokeRequired)
            {
                dataGridView.Invoke(new Action(() =>
                {
                    int rowIndex = _processedDataTable.Rows.Count;
                    _processedDataTable.Rows.Add(time, orderNo, hn, patientName, drugCode, drugName, quantity, status, apiResponse);

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
                _processedDataTable.Rows.Add(time, orderNo, hn, patientName, drugCode, drugName, quantity, status, apiResponse);

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

        private void UpdateRecordCount()
        {
            if (recordCountLabel.InvokeRequired)
            {
                recordCountLabel.Invoke(new Action(UpdateRecordCount));
                return;
            }

            recordCountLabel.Text = $"Total Records: {_processedDataTable.Rows.Count}";
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

        private async void BackgroundTimerCallback(object state)
        {
            if (!_isProcessing)
            {
                await CheckPendingOrdersauto();
            }
        }
        private async Task CheckPendingOrdersauto()
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
                        _processor.ProcessPendingOrders(
                            msg =>
                            {
                                _logger.LogInfo($"Background processing: {msg}");
                            },
                            result =>
                            {
                                // แสดงผลบนตาราง

                                var hl7Message = result.ParsedMessage;

                                string orderNo = hl7Message?.CommonOrder?.PlacerOrderNumber ?? "N/A";
                                string hn = hl7Message?.PatientIdentification?.PatientIDExternal ??
                                           hl7Message?.PatientIdentification?.PatientIDInternal ?? "N/A";

                                string patientName = "N/A";
                                if (hl7Message?.PatientIdentification?.OfficialName != null)
                                {
                                    var name = hl7Message.PatientIdentification.OfficialName;
                                    patientName = $"{name.Prefix ?? ""} {name.FirstName ?? ""} {name.LastName ?? ""}".Trim();
                                }

                                string drugCode = "N/A";
                                string drugName = "N/A";
                                string quantity = "N/A";

                                if (hl7Message?.PharmacyDispense != null && hl7Message.PharmacyDispense.Count > 0)
                                {
                                    var rxd = hl7Message.PharmacyDispense[0];
                                    drugCode = rxd.Dispensegivecode?.Dispense ?? "N/A";
                                    drugName = rxd.Dispensegivecode?.DrugName ??
                                              rxd.Dispensegivecode?.DrugNamePrint ?? "N/A";
                                    quantity = rxd.QTY > 0 ? rxd.QTY.ToString() : "N/A";
                                }


                                AddRowToGrid(
                                    DateTime.Now.ToString("HH:mm:ss"),
                                    orderNo,
                                    hn,
                                    patientName,
                                    drugCode,
                                    drugName,
                                    quantity,
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
              
            }
        }
        private async Task CheckPendingOrdersManual()
        {
            if (_isProcessing) return;

            _isProcessing = true;
            testHL7Button.Enabled = false;
            startStopButton.Enabled = false;
            clearButton.Enabled = false;
            exportButton.Enabled = false;

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
                        _processor.ProcessPendingOrders(
                            msg =>
                            {
                                _logger.LogInfo($"Background processing: {msg}");
                            },
                            result =>
                            {
                                // แสดงผลบนตาราง
                             
                                var hl7Message = result.ParsedMessage;

                                string orderNo = hl7Message?.CommonOrder?.PlacerOrderNumber  ?? "N/A";
                                string hn = hl7Message?.PatientIdentification?.PatientIDExternal ??
                                           hl7Message?.PatientIdentification?.PatientIDInternal ??"N/A";

                                string patientName = "N/A";
                                if (hl7Message?.PatientIdentification?.OfficialName != null)
                                {
                                    var name = hl7Message.PatientIdentification.OfficialName;
                                    patientName = $"{name.Prefix ?? ""} {name.FirstName ?? ""} {name.LastName ?? ""}".Trim();
                                }
                               
                                string drugCode = "N/A";
                                string drugName = "N/A";
                                string quantity = "N/A";

                                if (hl7Message?.PharmacyDispense != null && hl7Message.PharmacyDispense.Count > 0)
                                {
                                    var rxd = hl7Message.PharmacyDispense[0];
                                    drugCode = rxd.Dispensegivecode?.Dispense ??  "N/A";
                                    drugName = rxd.Dispensegivecode?.DrugName ??
                                              rxd.Dispensegivecode?.DrugNamePrint ?? "N/A";
                                    quantity = rxd.QTY > 0 ? rxd.QTY.ToString() : "N/A";
                                }
                               

                                AddRowToGrid(
                                    DateTime.Now.ToString("HH:mm:ss"),
                                    orderNo,
                                    hn,
                                    patientName,
                                    drugCode,
                                    drugName,
                                    quantity,
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
                testHL7Button.Enabled = true;
                startStopButton.Enabled = true;
                clearButton.Enabled = true;
                exportButton.Enabled = true;
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
    }
}