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
        private DataView _filteredDataView;

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
            _processedDataTable.Columns.Add("FinancialClass", typeof(string));
            _processedDataTable.Columns.Add("OrderControl", typeof(string));
            _processedDataTable.Columns.Add("Status", typeof(string));
            _processedDataTable.Columns.Add("API Response", typeof(string)); // เก็บข้อมูลไว้แต่จะไม่แสดง

            _filteredDataView = new DataView(_processedDataTable);
            dataGridView.DataSource = _filteredDataView;

            // ลบ double click event และเพิ่ม cell click event
            dataGridView.CellClick += DataGridView_CellClick;

            dataGridView.Refresh();

            // อัปเดต Status Summary เริ่มต้น
            UpdateStatusSummary();

            try
            {
                if (dataGridView.Columns.Count >= 9)
                {
                    dataGridView.Columns["Time Check"].Width = 150;
                    dataGridView.Columns["Transaction DateTime"].Width = 150;
                    dataGridView.Columns["Order No"].Width = 100;
                    dataGridView.Columns["HN"].Width = 80;
                    dataGridView.Columns["Patient Name"].Width = 150;
                    dataGridView.Columns["FinancialClass"].Width = 150;
                    dataGridView.Columns["OrderControl"].Width = 80;
                    dataGridView.Columns["Status"].Width = 100;

                    // ซ่อนคอลัมน์ API Response เดิม
                    dataGridView.Columns["API Response"].Visible = false;

                    // เพิ่มคอลัมน์ปุ่ม View แทน
                    AddViewButtonColumn();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError("Error setting column widths", ex);
            }

            UpdateRecordCount();
        }

        private void InitializePanelPaintEvents()
        {
            totalPanel.Paint += (s, e) => DrawPanelTopBar(e, System.Drawing.Color.Gray);
            successPanel.Paint += (s, e) => DrawPanelTopBar(e, System.Drawing.Color.Green);
            failedPanel.Paint += (s, e) => DrawPanelTopBar(e, System.Drawing.Color.Red);
            pendingPanel.Paint += (s, e) => DrawPanelTopBar(e, System.Drawing.Color.Orange);
            rejectPanel.Paint += (s, e) => DrawPanelTopBar(e, System.Drawing.Color.DarkGray);

            // เพิ่ม Click Event สำหรับแต่ละ Panel
            totalPanel.Click += TotalPanel_Click;
            successPanel.Click += SuccessPanel_Click;
            failedPanel.Click += FailedPanel_Click;
            pendingPanel.Click += PendingPanel_Click;
            rejectPanel.Click += RejectPanel_Click;

            // เพิ่ม Click Event ให้กับ Label ภายใน Panel ด้วย
            foreach (Control ctrl in totalPanel.Controls)
            {
                if (ctrl is Label)
                {
                    ctrl.Click += TotalPanel_Click;
                    ctrl.Cursor = System.Windows.Forms.Cursors.Hand;
                }
            }
            foreach (Control ctrl in successPanel.Controls)
            {
                if (ctrl is Label)
                {
                    ctrl.Click += SuccessPanel_Click;
                    ctrl.Cursor = System.Windows.Forms.Cursors.Hand;
                }
            }
            foreach (Control ctrl in failedPanel.Controls)
            {
                if (ctrl is Label)
                {
                    ctrl.Click += FailedPanel_Click;
                    ctrl.Cursor = System.Windows.Forms.Cursors.Hand;
                }
            }
            foreach (Control ctrl in pendingPanel.Controls)
            {
                if (ctrl is Label)
                {
                    ctrl.Click += PendingPanel_Click;
                    ctrl.Cursor = System.Windows.Forms.Cursors.Hand;
                }
            }
            foreach (Control ctrl in rejectPanel.Controls)
            {
                if (ctrl is Label)
                {
                    ctrl.Click += RejectPanel_Click;
                    ctrl.Cursor = System.Windows.Forms.Cursors.Hand;
                }
            }

            // เพิ่ม Cursor เป็น Hand เมื่อ Hover
            totalPanel.Cursor = System.Windows.Forms.Cursors.Hand;
            successPanel.Cursor = System.Windows.Forms.Cursors.Hand;
            failedPanel.Cursor = System.Windows.Forms.Cursors.Hand;
            pendingPanel.Cursor = System.Windows.Forms.Cursors.Hand;
            rejectPanel.Cursor = System.Windows.Forms.Cursors.Hand;
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

                // เริ่มต้น Status Filter Buttons
                UpdateStatusFilterButtons();

                // เพิ่มบรรทัดนี้
                InitializePanelPaintEvents();

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

        #region Search and Filter
        // ตัวแปรเก็บสถานะ Status Filter ปัจจุบัน
        private string _currentStatusFilter = "All";

        private void SearchButton_Click(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            ClearFilter();
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ApplyFilter();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        // เพิ่ม Event Handler สำหรับ DateTimePicker
        private void DateTimePicker_ValueChanged(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private void TotalPanel_Click(object sender, EventArgs e)
        {
            _currentStatusFilter = "All";
            ApplyStatusFilter();
            UpdateStatusFilterButtons();
        }

        private void SuccessPanel_Click(object sender, EventArgs e)
        {
            _currentStatusFilter = "Success";
            ApplyStatusFilter();
            UpdateStatusFilterButtons();
        }

        private void FailedPanel_Click(object sender, EventArgs e)
        {
            _currentStatusFilter = "Failed";
            ApplyStatusFilter();
            UpdateStatusFilterButtons();
        }

        private void PendingPanel_Click(object sender, EventArgs e)
        {
            _currentStatusFilter = "Pending";
            ApplyStatusFilter();
            UpdateStatusFilterButtons();
        }

        private void RejectPanel_Click(object sender, EventArgs e)
        {
            _currentStatusFilter = "Rejected";
            ApplyStatusFilter();
            UpdateStatusFilterButtons();
        }
        private void UpdateStatusFilterButtons()
        {
            // ไม่ต้องทำอะไรเพราะไม่มีปุ่มแล้ว
            // หรือถ้าต้องการ highlight Panel ที่เลือก อาจเพิ่ม border หนาขึ้น

            // Reset border ของทุก Panel
            totalPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            successPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            failedPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            pendingPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            rejectPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

            // Highlight Panel ที่เลือกด้วย border หนา (ถ้าต้องการ)
            if (_currentStatusFilter == "All")
                totalPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            else if (_currentStatusFilter == "Success")
                successPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            else if (_currentStatusFilter == "Failed")
                failedPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            else if (_currentStatusFilter == "Pending")
                pendingPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            else if (_currentStatusFilter == "Rejected")
                rejectPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
        }

        // ฟังก์ชันใหม่: กรอง Status เท่านั้น (ไม่ยุ่งกับวันที่และ TextBox)
        private void ApplyStatusFilter()
        {
            try
            {
                // กรอง Status เท่านั้น
                if (_currentStatusFilter == "All")
                {
                    _filteredDataView.RowFilter = string.Empty;
                }
                else
                {
                    _filteredDataView.RowFilter = $"[Status] = '{_currentStatusFilter}'";
                }

                // อัปเดตสีของแถวหลัง filter
                ApplyRowColors();

                // อัปเดตจำนวนผลลัพธ์
                int resultCount = _filteredDataView.Count;

                string statusInfo = _currentStatusFilter == "All" ? "all statuses" : $"status '{_currentStatusFilter}'";
                UpdateStatus($"Showing {resultCount} record(s) with {statusInfo}");

              
                UpdateStatusSummary();
                _logger.LogInfo($"Status filter applied: {statusInfo} - Found {resultCount} record(s)");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error applying status filter", ex);
                MessageBox.Show($"Status filter error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyFilter()
        {
            try
            {
                string searchText = searchTextBox.Text.Trim();
                DateTime selectedDate = dateTimePicker.Value.Date;

                // สร้าง filter expression
                var filterParts = new System.Collections.Generic.List<string>();

                // กรณีค้นหา Order No หรือ HN
                if (!string.IsNullOrEmpty(searchText))
                {
                    filterParts.Add($"([Order No] LIKE '%{searchText}%' OR [HN] LIKE '%{searchText}%')");
                }

                // กรณีค้นหาวันที่ - ค้นหาทั้ง Time Check และ Transaction DateTime
                string datePattern = selectedDate.ToString("yyyy-MM-dd");
                filterParts.Add($"([Time Check] LIKE '{datePattern}%' OR [Transaction DateTime] LIKE '{datePattern}%')");

                // เพิ่ม Status Filter (ถ้ามีการกรองอยู่)
                if (_currentStatusFilter != "All")
                {
                    filterParts.Add($"[Status] = '{_currentStatusFilter}'");
                }

                // รวม filter ทั้งหมดด้วย AND
                string filterExpression = string.Join(" AND ", filterParts);

                _filteredDataView.RowFilter = filterExpression;

                // อัปเดตสีของแถวหลัง filter
                ApplyRowColors();

                // อัปเดตจำนวนผลลัพธ์
                int resultCount = _filteredDataView.Count;

                // สร้างข้อความแสดงผลการค้นหา
                string searchInfo = string.Empty;
                if (!string.IsNullOrEmpty(searchText))
                {
                    searchInfo += $"text '{searchText}' and ";
                }
                searchInfo += $"date '{datePattern}'";

                if (_currentStatusFilter != "All")
                {
                    searchInfo += $" (Status: {_currentStatusFilter})";
                }

                if (resultCount > 0)
                {
                    UpdateStatus($"Found {resultCount} record(s) matching {searchInfo}");
                }
                else
                {
                    UpdateStatus($"No records found matching {searchInfo}");
                }
                UpdateStatusSummary();
                _logger.LogInfo($"Filter applied: {searchInfo} - Found {resultCount} record(s)");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error applying filter", ex);
                MessageBox.Show($"Search error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearFilter()
        {
            try
            {
                searchTextBox.Text = string.Empty;
                dateTimePicker.Value = DateTime.Today;
                _currentStatusFilter = "All";
                _filteredDataView.RowFilter = string.Empty;

                // อัปเดตสีของแถวหลัง clear filter
                ApplyRowColors();
                UpdateStatusSummary();
                UpdateStatusFilterButtons();
                UpdateStatus("Filter cleared - Showing all records");
                _logger.LogInfo("Search filter cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error clearing filter", ex);
                MessageBox.Show($"Error clearing filter: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region GridView
        private void AddRowToGrid(string time, string TransactionDateTime, string orderNo, string hn, string patientName,
            string FinancialClass, string OrderControl, string status, string apiResponse, HL7Message hl7Data)
        {
            if (dataGridView.InvokeRequired)
            {
                dataGridView.Invoke(new Action(() =>
                {
                    int rowIndex = _processedDataTable.Rows.Count;
                    _processedDataTable.Rows.Add(time, TransactionDateTime, orderNo, hn, patientName, FinancialClass, OrderControl, status, apiResponse);

                    if (hl7Data != null)
                    {
                        _rowHL7Data[rowIndex] = hl7Data;
                    }
                    UpdateStatusSummary();

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
                }));
            }
            else
            {
                int rowIndex = _processedDataTable.Rows.Count;
                _processedDataTable.Rows.Add(time, TransactionDateTime, orderNo, hn, patientName, FinancialClass, OrderControl, status, apiResponse);

                if (hl7Data != null)
                {
                    _rowHL7Data[rowIndex] = hl7Data;
                }
                UpdateStatusSummary();

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

        private void AddViewButtonColumn()
        {
            if (!dataGridView.Columns.Contains("ViewButton"))
            {
                DataGridViewButtonColumn viewButtonColumn = new DataGridViewButtonColumn();
                viewButtonColumn.Name = "ViewButton";
                viewButtonColumn.HeaderText = "API Response";
                viewButtonColumn.Text = "View";
                viewButtonColumn.UseColumnTextForButtonValue = true;
                viewButtonColumn.Width = 100;

                // เพิ่มคอลัมน์ไว้ตำแหน่งสุดท้าย
                dataGridView.Columns.Add(viewButtonColumn);
            }
        }

        // แทนที่ DataGridView_CellDoubleClick ด้วย DataGridView_CellClick
        private void DataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // ตรวจสอบว่าคลิกที่คอลัมน์ปุ่ม View
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                if (dataGridView.Columns[e.ColumnIndex].Name == "ViewButton")
                {
                    try
                    {
                        // ดึง Order Number และ Status จากคอลัมน์
                        string time = dataGridView.Rows[e.RowIndex].Cells["Time Check"].Value?.ToString() ?? "N/A";
                        string orderNo = dataGridView.Rows[e.RowIndex].Cells["Order No"].Value?.ToString() ?? "N/A";
                        string status = dataGridView.Rows[e.RowIndex].Cells["Status"].Value?.ToString() ?? "N/A";

                        // หา row index ที่แท้จริงใน DataTable (เพราะอาจมีการกรอง)
                        int actualRowIndex = -1;
                        if (_filteredDataView.Count > 0 && e.RowIndex < _filteredDataView.Count)
                        {
                            DataRowView rowView = _filteredDataView[e.RowIndex];
                            actualRowIndex = _processedDataTable.Rows.IndexOf(rowView.Row);
                        }

                        // ตรวจสอบว่ามี HL7Message สำหรับแถวนี้หรือไม่
                        if (actualRowIndex >= 0 && _rowHL7Data.ContainsKey(actualRowIndex))
                        {
                            var hl7Message = _rowHL7Data[actualRowIndex];

                            // เปิดฟอร์มแสดงรายละเอียด พร้อมส่ง status ไปด้วย
                            DateTime? filterDate = DateTime.TryParse(time, out DateTime parsedDate) ? parsedDate.Date : (DateTime?)null;
                            var detailForm = new HL7DetailForm(hl7Message, filterDate, orderNo, status);
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

        private void ApplyRowColors()
        {
            if (dataGridView.InvokeRequired)
            {
                dataGridView.Invoke(new Action(ApplyRowColors));
                return;
            }

            try
            {
                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    if (row.Cells["Status"].Value != null)
                    {
                        string status = row.Cells["Status"].Value.ToString();

                        if (status == "Success")
                        {
                            row.DefaultCellStyle.BackColor = System.Drawing.Color.LightGreen;
                        }
                        else if (status == "Failed")
                        {
                            row.DefaultCellStyle.BackColor = System.Drawing.Color.LightCoral;
                        }
                        else
                        {
                            row.DefaultCellStyle.BackColor = dataGridView.DefaultCellStyle.BackColor;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError("Error applying row colors", ex);
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

                // เปิดปุ่มกลับเมื่อทำงานเสร็จ (เฉพาะ manual)
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

        #region Update Methods
  

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

        private void UpdateStatusSummary()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(UpdateStatusSummary));
                return;
            }

            try
            {
                int totalRecords = _processedDataTable.Rows.Count;
                int successCount = 0;
                int failedCount = 0;
                int pendingCount = 0;
                int rejectCount = 0;

                foreach (DataRow row in _processedDataTable.Rows)
                {
                    string status = row["Status"]?.ToString() ?? "";

                    if (status == "Success")
                        successCount++;
                    else if (status == "Failed")
                        failedCount++;
                    else if (status == "Pending")
                        pendingCount++;
                    else if (status == "Rejected")
                        rejectCount++;
                }

                // อัปเดตค่าใน Label แต่ละอัน
                totalCountLabel.Text = totalRecords.ToString();
                successCountLabel.Text = successCount.ToString();
                failedCountLabel.Text = failedCount.ToString();
                pendingCountLabel.Text = pendingCount.ToString();
                rejectCountLabel.Text = rejectCount.ToString();

                // เปลี่ยนสี background ของ panel ตามสถานะ
                UpdatePanelStyles(totalPanel, totalRecords, System.Drawing.Color.FromArgb(240, 240, 240));
                UpdatePanelStyles(successPanel, successCount, System.Drawing.Color.FromArgb(220, 255, 220));
                UpdatePanelStyles(failedPanel, failedCount, System.Drawing.Color.FromArgb(255, 220, 220));
                UpdatePanelStyles(pendingPanel, pendingCount, System.Drawing.Color.FromArgb(255, 245, 220));
                UpdatePanelStyles(rejectPanel, rejectCount, System.Drawing.Color.FromArgb(240, 240, 240));
            }
            catch (Exception ex)
            {
                _logger?.LogError("Error updating status summary", ex);
            }
        }

        private void UpdatePanelStyles(Panel panel, int count, System.Drawing.Color highlightColor)
        {
            if (count > 0)
            {
                panel.BackColor = highlightColor;
                panel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            }
            else
            {
                panel.BackColor = System.Drawing.Color.White;
                panel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            }
        }

        // Paint Event Handlers for Panel Top Bars
        private void TotalPanel_Paint(object sender, PaintEventArgs e)
        {
            DrawPanelTopBar(e, System.Drawing.Color.Gray);
        }

        private void SuccessPanel_Paint(object sender, PaintEventArgs e)
        {
            DrawPanelTopBar(e, System.Drawing.Color.Green);
        }

        private void FailedPanel_Paint(object sender, PaintEventArgs e)
        {
            DrawPanelTopBar(e, System.Drawing.Color.Red);
        }

        private void PendingPanel_Paint(object sender, PaintEventArgs e)
        {
            DrawPanelTopBar(e, System.Drawing.Color.Orange);
        }

        private void RejectPanel_Paint(object sender, PaintEventArgs e)
        {
            DrawPanelTopBar(e, System.Drawing.Color.DarkGray);
        }

        private void DrawPanelTopBar(PaintEventArgs e, System.Drawing.Color barColor)
        {
            using (var brush = new System.Drawing.SolidBrush(barColor))
            {
                e.Graphics.FillRectangle(brush, 0, 0, e.ClipRectangle.Width, 3);
            }
        }

        #endregion

    }
}