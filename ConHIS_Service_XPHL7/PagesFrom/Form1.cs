using ConHIS_Service_XPHL7.Configuration;
using ConHIS_Service_XPHL7.Models;
using ConHIS_Service_XPHL7.Services;
using ConHIS_Service_XPHL7.Utils;
using Org.BouncyCastle.Utilities.Encoders;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static ConHIS_Service_XPHL7.Services.SimpleHL7FileProcessor;
using Timer = System.Threading.Timer;

namespace ConHIS_Service_XPHL7
{
    public partial class Form : System.Windows.Forms.Form
    {
        #region ตัวแปร
        private AppConfig _appConfig;
        private DatabaseService _databaseService;
        private LogManager _logger;
        private DrugDispenseProcessor _processor;
        private SimpleHL7FileProcessor _hl7FileProcessor;
        private DateTime? _lastFoundTime = null;
        private DateTime? _lastSuccessTime = null;
        private string _lastSuccessOrderId = null;
        // Windows API สำหรับปิด MessageBox อัตโนมัติ
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
        private const UInt32 WM_CLOSE = 0x0010;
        private bool _wasServiceRunningBeforeDisconnection = false;

        // Background service components
        private CancellationTokenSource _backgroundCancellationTokenSource = null;
        private Timer _backgroundTimer;
        private bool _isProcessing = false;
        private readonly int _intervalSeconds = 60;

        // ⭐ Connection Monitor - เพิ่มใหม่
        private Timer _connectionCheckTimer;
        private DateTime? _lastDatabaseDisconnectionTime = null;
        private readonly int _connectionCheckIntervalSeconds = 10;
        private bool _isCheckingConnection = false;
        private DateTime? _lastDatabaseConnectionTime = null;
        private bool _hasNotifiedDisconnection = false;
        private bool _hasNotifiedReconnection = false;

        // DataTable for DataGridView
        private DataTable _processedDataTable;
        private DataView _filteredDataView;

        // เก็บ HL7Message ที่เชื่อมกับแต่ละแถว
        private System.Collections.Generic.Dictionary<int, HL7Message> _rowHL7Data = new System.Collections.Generic.Dictionary<int, HL7Message>();

        // Connection status
        private bool _isDatabaseConnected = false;
        private bool _isInitializing = false;
        #endregion
        public Form()
        {
            InitializeComponent();
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;
            _logger = new LogManager();
            _hl7FileProcessor = new SimpleHL7FileProcessor();
        }

        private void InitializeDataTable()
        {
            // ⭐ ลบการเชื่อม event เก่า (ถ้ามี)
            if (dataGridView != null)
            {
                dataGridView.CellClick -= DataGridView_CellClick;
                dataGridView.DataSource = null;
                dataGridView.Rows.Clear();
                dataGridView.Columns.Clear();
            }

            _processedDataTable = new DataTable();
            _processedDataTable.Columns.Add("Time Check", typeof(string));
            _processedDataTable.Columns.Add("Transaction DateTime", typeof(string));
            _processedDataTable.Columns.Add("Order No", typeof(string));
            _processedDataTable.Columns.Add("HN", typeof(string));
            _processedDataTable.Columns.Add("Patient Name", typeof(string));
            _processedDataTable.Columns.Add("FinancialClass", typeof(string));
            _processedDataTable.Columns.Add("OrderControl", typeof(string));
            _processedDataTable.Columns.Add("Status", typeof(string));
            _processedDataTable.Columns.Add("API Response", typeof(string));

            _filteredDataView = new DataView(_processedDataTable);
            dataGridView.DataSource = _filteredDataView;

            // ⭐ เพิ่ม event handler สำหรับ sort
            dataGridView.ColumnHeaderMouseClick += DataGridView_ColumnHeaderMouseClick;

            // ⭐ เชื่อม event ครั้งเดียวเท่านั้น
            dataGridView.CellClick += DataGridView_CellClick;

            UpdateStatusSummary();

            try
            {
                if (dataGridView.Columns.Count >= 9)
                {
                    dataGridView.Columns["Time Check"].Width = 165;
                    dataGridView.Columns["Transaction DateTime"].Width = 165;
                    dataGridView.Columns["Order No"].Width = 110;
                    dataGridView.Columns["HN"].Width = 90;
                    dataGridView.Columns["Patient Name"].Width = 165;
                    dataGridView.Columns["FinancialClass"].Width = 165;
                    dataGridView.Columns["OrderControl"].Width = 90;
                    dataGridView.Columns["Status"].Width = 110;

                    dataGridView.Columns["API Response"].Visible = false;
                    AddViewButtonColumn();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError("Error setting column widths", ex);
            }
        }
       

        // ⭐ เพิ่ม Method สำหรับ Reset Notification Flags
        private void ResetNotificationFlags()
        {
            _hasNotifiedDisconnection = false;
            _hasNotifiedReconnection = false;
            _wasServiceRunningBeforeDisconnection = false;
        }

        // ⭐ เริ่ม Connection Monitor
        private void StartConnectionMonitor()
        {
            var intervalMs = _connectionCheckIntervalSeconds * 1000;
            _connectionCheckTimer = new Timer(ConnectionCheckCallback, null, intervalMs, intervalMs);
            ResetNotificationFlags(); // Reset flags เมื่อเริ่ม monitor
            _logger?.LogInfo($"Connection monitor started - checking every {_connectionCheckIntervalSeconds} seconds");
        }

        // ⭐ หยุด Connection Monitor
        private void StopConnectionMonitor()
        {
            _connectionCheckTimer?.Dispose();
            _connectionCheckTimer = null;
            ResetNotificationFlags(); // Reset flags เมื่อหยุด monitor
            _logger?.LogInfo("Connection monitor stopped");
        }

        // ⭐ Callback สำหรับตรวจสอบการเชื่อมต่อแบบ Realtime
        private async void ConnectionCheckCallback(object state)
        {
            if (_isCheckingConnection) return;

            _isCheckingConnection = true;

            try
            {
                string checkTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // ทดสอบการเชื่อมต่อ
                bool isConnected = await Task.Run(() => _databaseService?.TestConnection() ?? false);

                // บันทึก log
                _logger?.LogConnectDatabase(isConnected, _lastDatabaseConnectionTime, _lastDatabaseDisconnectionTime);

                // อัพเดทเฉพาะเมื่อสถานะเปลี่ยน
                if (isConnected != _isDatabaseConnected)
                {
                    if (isConnected)
                    {
                        // ✅ เชื่อมต่อกลับมาได้
                        this.Invoke(new Action(async () =>
                        {
                            try
                            {
                                UpdateConnectionStatus(true);

                                // รีเฟรชข้อมูลทันที
                                await LoadDataBySelectedDate();
                                UpdateStatus("✓ Database reconnected - Data refreshed automatically");

                                // แจ้งเตือนครั้งเดียวเมื่อเชื่อมต่อกลับมา
                                if (!_hasNotifiedReconnection)
                                {
                                    _hasNotifiedReconnection = true;
                                    _hasNotifiedDisconnection = false;

                                    // ⭐ เช็คว่า Service กำลังทำงานอยู่ก่อนหน้านี้หรือไม่
                                    bool shouldResumeService = _wasServiceRunningBeforeDisconnection;

                                    string serviceMessage = shouldResumeService
                                        ? "\n\n⚡ Service will resume automatically in 3 seconds..."
                                        : "";

                                    // ⭐ แสดง MessageBox ที่ปิดอัตโนมัติหลัง 3 วินาที
                                    this.BeginInvoke(new Action(() =>
                                    {
                                        ShowAutoCloseMessageBox(
                                            $"✅ Database connection has been restored!\n\n" +
                                            $"📅 Reconnected at: {_lastDatabaseConnectionTime.Value:yyyy-MM-dd HH:mm:ss}\n" +
                                            $"🔄 Data has been refreshed automatically." +
                                            serviceMessage,
                                            "Connection Restored",
                                            3000, // 3 วินาที
                                            shouldResumeService // ⭐ บอกว่าต้อง Resume Service หรือไม่
                                        );
                                    }));
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogError("Error refreshing data after reconnection", ex);
                            }
                        }));
                    }
                    else
                    {
                        // ❌ การเชื่อมต่อขาดหาย
                        this.Invoke(new Action(() =>
                        {
                            UpdateConnectionStatus(false);

                            // ⭐ บันทึกสถานะว่า Service กำลังทำงานอยู่หรือไม่ (เช็คจาก _backgroundTimer)
                            _wasServiceRunningBeforeDisconnection = (_backgroundTimer != null);

                            // ⭐ ถ้า Service กำลังทำงานอยู่ ให้หยุดชั่วคราว
                            if (_wasServiceRunningBeforeDisconnection)
                            {
                                _logger?.LogWarning("Service is running - Auto-stopping due to database disconnection");
                                StopBackgroundService(); // ⭐ เรียก StopBackgroundService
                                _logger?.LogInfo("Service stopped temporarily");
                            }

                            UpdateStatus("✗ Database connection lost - Reconnecting...");

                            // แจ้งเตือนครั้งเดียวเมื่อขาดการเชื่อมต่อ
                            if (!_hasNotifiedDisconnection)
                            {
                                _hasNotifiedDisconnection = true;
                                _hasNotifiedReconnection = false;

                                string serviceMessage = _wasServiceRunningBeforeDisconnection
                                    ? "\n\n⏸️ Service has been stopped temporarily and will auto-resume when reconnected."
                                    : "";

                                this.BeginInvoke(new Action(() =>
                                {
                                    ShowAutoCloseMessageBox(
                                        $"❌ Database connection has been lost!\n\n" +
                                        $"📅 Lost at: {_lastDatabaseDisconnectionTime.Value:yyyy-MM-dd HH:mm:ss}\n" +
                                        $"🔄 System will attempt to reconnect every {_connectionCheckIntervalSeconds} seconds." +
                                        serviceMessage,
                                        "Connection Lost",
                                        3000, // 3 วินาที
                                        false // ไม่ Resume Service เมื่อขาดการเชื่อมต่อ
                                    );
                                }));
                            }
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError("Connection check error", ex);

                this.Invoke(new Action(() =>
                {
                    UpdateConnectionStatus(false);
                }));
            }
            finally
            {
                _isCheckingConnection = false;
            }
        }

        private void InitializePanelPaintEvents()
        {
            totalPanel.Paint += (s, e) => DrawPanelTopBar(e, System.Drawing.Color.Gray);
            successPanel.Paint += (s, e) => DrawPanelTopBar(e, System.Drawing.Color.Green);
            failedPanel.Paint += (s, e) => DrawPanelTopBar(e, System.Drawing.Color.Red);
            pendingPanel.Paint += (s, e) => DrawPanelTopBar(e, System.Drawing.Color.Orange);
            rejectPanel.Paint += (s, e) => DrawPanelTopBar(e, System.Drawing.Color.DarkGray);

            totalPanel.Click += TotalPanel_Click;
            successPanel.Click += SuccessPanel_Click;
            failedPanel.Click += FailedPanel_Click;
            pendingPanel.Click += PendingPanel_Click;
            rejectPanel.Click += RejectPanel_Click;

            foreach (Control ctrl in totalPanel.Controls)
            {
                if (ctrl is Label) { ctrl.Click += TotalPanel_Click; ctrl.Cursor = System.Windows.Forms.Cursors.Hand; }
            }
            foreach (Control ctrl in successPanel.Controls)
            {
                if (ctrl is Label) { ctrl.Click += SuccessPanel_Click; ctrl.Cursor = System.Windows.Forms.Cursors.Hand; }
            }
            foreach (Control ctrl in failedPanel.Controls)
            {
                if (ctrl is Label) { ctrl.Click += FailedPanel_Click; ctrl.Cursor = System.Windows.Forms.Cursors.Hand; }
            }
            foreach (Control ctrl in pendingPanel.Controls)
            {
                if (ctrl is Label) { ctrl.Click += PendingPanel_Click; ctrl.Cursor = System.Windows.Forms.Cursors.Hand; }
            }
            foreach (Control ctrl in rejectPanel.Controls)
            {
                if (ctrl is Label) { ctrl.Click += RejectPanel_Click; ctrl.Cursor = System.Windows.Forms.Cursors.Hand; }
            }

            totalPanel.Cursor = System.Windows.Forms.Cursors.Hand;
            successPanel.Cursor = System.Windows.Forms.Cursors.Hand;
            failedPanel.Cursor = System.Windows.Forms.Cursors.Hand;
            pendingPanel.Cursor = System.Windows.Forms.Cursors.Hand;
            rejectPanel.Cursor = System.Windows.Forms.Cursors.Hand;
        }

        private async Task LoadDataBySelectedDate()
        {
            try
            {
                DateTime selectedDate = dateTimePicker.Value.Date;
                string searchText = searchTextBox.Text.Trim();

                UpdateStatus($"Loading data for {selectedDate:yyyy-MM-dd}...");

                // ⭐ ล้างข้อมูลเก่าทั้งหมด
                _processedDataTable.Rows.Clear();
                _rowHL7Data.Clear();

                if (_filteredDataView != null)
                {
                    _filteredDataView.RowFilter = string.Empty;
                }

                List<DrugDispenseipd> dispenseData = null;

                dispenseData = await Task.Run(() =>
                {
                    if (!string.IsNullOrEmpty(searchText))
                    {
                        _logger.LogInfo($"Search: {searchText} on {selectedDate:yyyy-MM-dd}");
                        return _databaseService.GetDispenseDataByDateAndSearch(selectedDate, searchText);
                    }
                    else
                    {
                        _logger.LogInfo($"Load: {selectedDate:yyyy-MM-dd}");
                        return _databaseService.GetDispenseDataByDate(selectedDate, selectedDate);
                    }
                });

                if (dispenseData == null)
                {
                    dispenseData = new List<DrugDispenseipd>();
                }

                _logger.LogInfo($"[LoadDataBySelectedDate] Database returned {dispenseData.Count} records");

                var hl7Service = new HL7Service();
                int loadedCount = 0;

                foreach (var data in dispenseData)
                {
                    try
                    {
                        string hl7String;
                        try
                        {
                            Encoding tisEncoding = null;
                            try { tisEncoding = Encoding.GetEncoding("TIS-620"); }
                            catch { }
                            if (tisEncoding == null) { try { tisEncoding = Encoding.GetEncoding(874); } catch { } }
                            if (tisEncoding != null) { hl7String = tisEncoding.GetString(data.Hl7Data); }
                            else { hl7String = Encoding.UTF8.GetString(data.Hl7Data); }
                        }
                        catch
                        {
                            hl7String = Encoding.UTF8.GetString(data.Hl7Data);
                        }

                        HL7Message hl7Message = hl7Service.ParseHL7Message(hl7String);

                        DateTime timeCheckDate = DateTime.Now;
                        if (data.RecieveStatusDatetime.HasValue && data.RecieveStatusDatetime.Value != DateTime.MinValue)
                        {
                            timeCheckDate = data.RecieveStatusDatetime.Value;
                        }
                        else if (data.DrugDispenseDatetime != DateTime.MinValue)
                        {
                            timeCheckDate = data.DrugDispenseDatetime;
                        }
                        else if (hl7Message?.CommonOrder?.TransactionDateTime.HasValue == true)
                        {
                            timeCheckDate = hl7Message.CommonOrder.TransactionDateTime.Value;
                        }

                        string timeCheck = timeCheckDate.ToString("yyyy-MM-dd HH:mm:ss");

                        DateTime? transactionDt = hl7Message?.CommonOrder?.TransactionDateTime;
                        string transactionDateTime = (transactionDt.HasValue && transactionDt.Value != DateTime.MinValue)
                            ? transactionDt.Value.ToString("yyyy-MM-dd HH:mm:ss")
                            : "N/A";

                        string orderNo = hl7Message?.CommonOrder?.PlacerOrderNumber ?? "N/A";
                        string hn = hl7Message?.PatientIdentification?.PatientIDExternal ??
                                   hl7Message?.PatientIdentification?.PatientIDInternal ?? "N/A";

                        string patientName = "N/A";
                        if (hl7Message?.PatientIdentification?.OfficialName != null)
                        {
                            var name = hl7Message.PatientIdentification.OfficialName;
                            patientName = $"{name.Prefix ?? ""} {name.FirstName ?? ""} {name.LastName ?? ""}".Trim();
                            if (string.IsNullOrWhiteSpace(patientName)) patientName = "N/A";
                        }

                        string financialClass = "N/A";
                        if (hl7Message?.PatientVisit?.FinancialClass != null)
                        {
                            var fc = hl7Message.PatientVisit.FinancialClass;
                            financialClass = $"{fc.ID ?? ""} {fc.Name ?? ""}".Trim();
                            if (string.IsNullOrWhiteSpace(financialClass)) financialClass = "N/A";
                        }

                        string orderControl = data.RecieveOrderType ?? hl7Message?.CommonOrder?.OrderControl ?? "N/A";

                        string status = "N/A";
                        if (data.RecieveStatus == 'Y')
                        {
                            status = "Success";
                            // ⭐ อัพเดทเวลา Success ล่าสุด
                            if (!_lastSuccessTime.HasValue || timeCheckDate > _lastSuccessTime.Value)
                            {
                                _lastSuccessTime = timeCheckDate;
                                UpdateLastSuccess(orderNo); // ⭐ เพิ่มบรรทัดนี้
                            }
                        }
                        else if (data.RecieveStatus == 'F')
                            status = "Failed";
                        else if (data.RecieveStatus == 'N')
                            continue;

                        AddRowToGrid(timeCheck, transactionDateTime, orderNo, hn, patientName,
                                   financialClass, orderControl, status, "Database Record", hl7Message);
                        loadedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Error loading record {data.DrugDispenseipdId}: {ex.Message}");
                    }
                }

                _logger.LogInfo($"[LoadDataBySelectedDate] Added {loadedCount} rows to DataTable. DataTable.Rows.Count = {_processedDataTable.Rows.Count}");

                _currentStatusFilter = "All";
                _filteredDataView.RowFilter = string.Empty;

                ApplyRowColors();
                UpdateStatusSummary();
                UpdateStatusFilterButtons();

                if (loadedCount > 0)
                {
                    UpdateStatus($"✓ Loaded {loadedCount} records for {selectedDate:yyyy-MM-dd}");
                }
                else
                {
                    UpdateStatus($"✗ No records for {selectedDate:yyyy-MM-dd}");
                }

                _logger.LogInfo($"[LoadDataBySelectedDate] Complete - DataGridView.Rows.Count = {dataGridView.Rows.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error loading data by date", ex);
                UpdateStatus($"✗ Error: {ex.Message}");
            }
        }

        // ⭐ แก้ไข Form1_Load เพื่อเปิด Connection Monitor
        private async void Form1_Load(object sender, EventArgs e)
        {
            _logger.LogInfo("Start Interface");
            UpdateStatus("Initializing...");

            InitializeDataTable();
            UpdateConnectionStatus(false);

            try
            {
                _logger.LogInfo("Loading configuration");
                _appConfig = new AppConfig();
                _appConfig.LoadConfiguration();
                _logger.LogInfo("Configuration loaded");

                if (int.TryParse(System.Configuration.ConfigurationManager.AppSettings["LogRetentionDays"], out int retentionDays))
                {
                    _logger.LogRetentionDays = retentionDays;
                    _logger.LogInfo($"Log retention days loaded: {retentionDays} days");
                }

                _logger.LogInfo("Connecting to database");
                _databaseService = new DatabaseService(_appConfig.ConnectionString);

                // ทดสอบการเชื่อมต่อครั้งแรก
                bool dbConnected = await Task.Run(() => _databaseService.TestConnection());
                UpdateConnectionStatus(dbConnected);

                if (dbConnected)
                {
                    _logger.LogInfo("DatabaseService initialized successfully");

                    // ⭐ ป้องกัน double-load โดยตั้ง flag ก่อน
                    _isInitializing = true;
                    dateTimePicker.Value = DateTime.Today;  // นี่จะ trigger event แต่จะถูก skip
                    _isInitializing = false;

                    // ⭐ เรียกครั้งเดียว
                    await LoadDataBySelectedDate();
                }
                else
                {
                    _logger.LogWarning("Initial database connection failed");
                }

                var apiService = new ApiService(AppConfig.ApiEndpoint);
                var hl7Service = new HL7Service();
                _processor = new DrugDispenseProcessor(_databaseService, hl7Service, apiService);

                UpdateStatusFilterButtons();
                InitializePanelPaintEvents();

                // ⭐ เริ่ม Connection Monitor
                StartConnectionMonitor();

                UpdateStatus("Ready - Service Stopped");
                startStopButton.Enabled = true;
                testHL7Button.Enabled = true;
                manualCheckButton.Enabled = true;
                exportButton.Enabled = true;
                settingsButton.Enabled = true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to initialize", ex);
                UpdateStatus($"Error: {ex.Message}");
                UpdateConnectionStatus(false);
            }
        }

        #region Test HL7 File
        //private async void TestHL7Button_Click(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        using (var openFileDialog = new OpenFileDialog())
        //        {
        //            openFileDialog.Title = "Select HL7 File to Test";
        //            openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";

        //            var searchFolders = new[]
        //            {
        //                Path.Combine(Application.StartupPath, "TestData"),
        //                Application.StartupPath,
        //                Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        //            };

        //            string initialDirectory = Application.StartupPath;
        //            foreach (var folder in searchFolders)
        //            {
        //                if (Directory.Exists(folder))
        //                {
        //                    initialDirectory = folder;
        //                    break;
        //                }
        //            }
        //            openFileDialog.InitialDirectory = initialDirectory;

        //            if (openFileDialog.ShowDialog() == DialogResult.OK)
        //            {
        //                var filePath = openFileDialog.FileName;
        //                var fileName = Path.GetFileName(filePath);

        //                if (string.IsNullOrEmpty(AppConfig.ApiEndpoint))
        //                {
        //                    _logger.LogError("API Endpoint is not configured!");
        //                    UpdateStatus("Error: API Endpoint not configured");
        //                    return;
        //                }

        //                var sendToApi = true;

        //                UpdateStatus($"Testing HL7 file: {fileName}...");
        //                testHL7Button.Enabled = false;
        //                manualCheckButton.Enabled = false;
        //                startStopButton.Enabled = false;
        //                exportButton.Enabled = false;

        //                HL7TestResult result = null;
        //                await Task.Run(() =>
        //                {
        //                    result = _hl7FileProcessor.ProcessAndSendHL7File(filePath, sendToApi);
        //                });

        //                if (result != null)
        //                {
        //                    string TransactionDateTime = result.ParsedMessage?.CommonOrder?.TransactionDateTime != null
        //                            ? ((DateTime)result.ParsedMessage?.CommonOrder?.TransactionDateTime)
        //                                .ToString("yyyy-MM-dd HH:mm:ss")
        //                            : null;
        //                    string orderNo = result.ParsedMessage?.CommonOrder?.PlacerOrderNumber ?? "N/A";
        //                    string hn = result.ParsedMessage?.PatientIdentification?.PatientIDExternal ??
        //                               result.ParsedMessage?.PatientIdentification?.PatientIDInternal ?? "N/A";

        //                    string patientName = "N/A";
        //                    if (result.ParsedMessage?.PatientIdentification?.OfficialName != null)
        //                    {
        //                        var name = result.ParsedMessage.PatientIdentification.OfficialName;
        //                        patientName = $"{name.Prefix ?? ""} {name.FirstName ?? ""} {name.LastName ?? ""}".Trim();
        //                        if (string.IsNullOrWhiteSpace(patientName)) patientName = "N/A";
        //                    }

        //                    string FinancialClass = "N/A";
        //                    if (result.ParsedMessage?.PatientVisit?.FinancialClass != null)
        //                    {
        //                        var financialclass = result.ParsedMessage.PatientVisit.FinancialClass;
        //                        FinancialClass = $"{financialclass.ID ?? ""} {financialclass.Name ?? ""}".Trim();
        //                        if (string.IsNullOrWhiteSpace(FinancialClass)) FinancialClass = "N/A";
        //                    }

        //                    string OrderControl = result.ParsedMessage?.CommonOrder?.OrderControl ?? "N/A";

        //                    AddRowToGrid(
        //                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        //                        TransactionDateTime,
        //                        orderNo,
        //                        hn,
        //                        patientName,
        //                        FinancialClass,
        //                        OrderControl,
        //                        result.Success ? "Success" : "Failed",
        //                        result.ApiResponse ?? result.ErrorMessage ?? "N/A",
        //                        result.ParsedMessage
        //                    );

        //                    UpdateStatus(result.Success ? $"HL7 test completed - {fileName}" : $"HL7 test failed - {fileName}");
        //                }
        //                else
        //                {
        //                    UpdateStatus("HL7 test failed - Check log for details");
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError("HL7 file test error", ex);
        //        UpdateStatus($"HL7 test error: {ex.Message}");
        //    }
        //    finally
        //    {
        //        testHL7Button.Enabled = true;
        //        manualCheckButton.Enabled = true;
        //        startStopButton.Enabled = true;
        //        exportButton.Enabled = true;
        //    }
        //}
        #endregion

        #region Export
        //private void ExportButton_Click(object sender, EventArgs e)
        //{
          

        //    try
        //    {
        //        using (var saveFileDialog = new SaveFileDialog())
        //        {
        //            saveFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
        //            saveFileDialog.FileName = $"DrugDispense_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        //            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        //            if (saveFileDialog.ShowDialog() == DialogResult.OK)
        //            {
        //                ExportToCSV(saveFileDialog.FileName);
                       
        //                _logger.LogInfo($"Data exported to: {saveFileDialog.FileName}");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError("Export error", ex);
                
        //    }
        //}

        private void ExportToCSV(string filePath)
        {
            var csv = new StringBuilder();

            var headers = new string[_processedDataTable.Columns.Count];
            for (int i = 0; i < _processedDataTable.Columns.Count; i++)
            {
                headers[i] = _processedDataTable.Columns[i].ColumnName;
            }
            csv.AppendLine(string.Join(",", headers));

            foreach (DataRow row in _processedDataTable.Rows)
            {
                var fields = new string[_processedDataTable.Columns.Count];
                for (int i = 0; i < _processedDataTable.Columns.Count; i++)
                {
                    var value = row[i].ToString();
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
        private string _currentStatusFilter = "All";

        private async void DateTimePicker_ValueChanged(object sender, EventArgs e)
        {
            // ⭐ ข้ามการโหลดเมื่อกำลังเริ่มต้น
            if (_isInitializing)
            {
                _logger.LogInfo("Skipping load during initialization");
                return;
            }

            if (_processedDataTable != null && _processedDataTable.Columns.Count > 0)
            {
                await LoadDataBySelectedDate();
            }
        }


        private async void SearchButton_Click(object sender, EventArgs e)
        {
            await LoadDataBySelectedDate();
        }

        private async void RefreshButton_Click(object sender, EventArgs e)
        {
            try
            {
                searchTextBox.Text = string.Empty;
                dateTimePicker.Value = DateTime.Today;
                _currentStatusFilter = "All";

                await LoadDataBySelectedDate();

                _logger.LogInfo("Data refreshed");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error refreshing data", ex);
                
            }
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
            totalPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            successPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            failedPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            pendingPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            rejectPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

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

        private void ApplyStatusFilter()
        {
            try
            {
                if (_currentStatusFilter == "All")
                {
                    _filteredDataView.RowFilter = string.Empty;
                }
                else
                {
                    _filteredDataView.RowFilter = $"[Status] = '{_currentStatusFilter}'";
                }

                // ⭐ เรียก ApplyRowColors หลังจาก apply filter
                ApplyRowColors();

                int resultCount = _filteredDataView.Count;
                string statusInfo = _currentStatusFilter == "All" ? "all statuses" : $"status '{_currentStatusFilter}'";
                UpdateStatus($"Showing {resultCount} record(s) with {statusInfo}");
                UpdateStatusSummary();
                _logger.LogInfo($"Status filter applied: {statusInfo} - Found {resultCount} record(s)");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error applying status filter", ex);
            }
        }

        // ⭐ ปรับปรุง ApplyFilter เพื่อ apply สีหลังจาก filter
        private void ApplyFilter()
        {
            try
            {
                string searchText = searchTextBox.Text.Trim();
                DateTime selectedDate = dateTimePicker.Value.Date;

                var filterParts = new System.Collections.Generic.List<string>();

                if (!string.IsNullOrEmpty(searchText))
                {
                    filterParts.Add($"([Order No] LIKE '%{searchText}%' OR [HN] LIKE '%{searchText}%')");
                }

                string datePattern = selectedDate.ToString("yyyy-MM-dd");
                filterParts.Add($"([Time Check] LIKE '{datePattern}%' OR [Transaction DateTime] LIKE '{datePattern}%')");

                if (_currentStatusFilter != "All")
                {
                    filterParts.Add($"[Status] = '{_currentStatusFilter}'");
                }

                string filterExpression = string.Join(" AND ", filterParts);

                _logger.LogInfo($"=== FILTER ===");
                _logger.LogInfo($"Date: {datePattern} | Search: '{searchText}' | Status: {_currentStatusFilter}");
                _logger.LogInfo($"Expression: {filterExpression}");

                _filteredDataView.RowFilter = filterExpression;

                // ⭐ เรียก ApplyRowColors หลังจาก apply filter
                ApplyRowColors();

                int resultCount = _filteredDataView.Count;
                int totalCount = _processedDataTable.Rows.Count;

                string info = $"Date: {datePattern}";
                if (!string.IsNullOrEmpty(searchText)) info += $" | Search: {searchText}";
                if (_currentStatusFilter != "All") info += $" | Status: {_currentStatusFilter}";

                if (resultCount > 0)
                {
                    UpdateStatus($"✓ {resultCount} record(s) - {info} (Total: {totalCount})");
                }
                else
                {
                    UpdateStatus($"✗ No records - {info} (Total: {totalCount})");
                }

                UpdateStatusSummary();
                _logger.LogInfo($"Result: {resultCount} records | Total: {totalCount}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error applying filter", ex);
            }
        }
        #endregion

        #region GridView
        private void AddRowToGrid(string time, string TransactionDateTime, string orderNo, string hn, string patientName,
     string FinancialClass, string OrderControl, string status, string apiResponse, HL7Message hl7Data)
        {
            try
            {
                // ⭐ ตรวจสอบ InvokeRequired ก่อน
                if (dataGridView.InvokeRequired)
                {
                    dataGridView.Invoke(new Action(() =>
                    {
                        AddRowToGridDirect(time, TransactionDateTime, orderNo, hn, patientName, FinancialClass,
                                          OrderControl, status, apiResponse, hl7Data);
                    }));
                    return;  // ⭐ เพิ่ม return เพื่อไม่ให้ทำงานซ้ำ
                }

                AddRowToGridDirect(time, TransactionDateTime, orderNo, hn, patientName, FinancialClass,
                                  OrderControl, status, apiResponse, hl7Data);
            }
            catch (Exception ex)
            {
                _logger?.LogError("Error in AddRowToGrid", ex);
            }
        }

        // ⭐ method ที่ทำงานจริง
        private void AddRowToGridDirect(string time, string TransactionDateTime, string orderNo, string hn, string patientName,
            string FinancialClass, string OrderControl, string status, string apiResponse, HL7Message hl7Data)
        {
            try
            {
                int rowIndex = _processedDataTable.Rows.Count;
                _processedDataTable.Rows.Add(time, TransactionDateTime, orderNo, hn, patientName,
                                             FinancialClass, OrderControl, status, apiResponse);

                if (hl7Data != null)
                {
                    _rowHL7Data[rowIndex] = hl7Data;
                }

                UpdateStatusSummary();

                if (dataGridView.Rows.Count > 0)
                {
                    dataGridView.FirstDisplayedScrollingRowIndex = dataGridView.Rows.Count - 1;
                }

                // ⭐ ใช้ RowCount - 1 แทน LastRow
                int lastRowIndex = dataGridView.Rows.Count - 1;
                if (lastRowIndex >= 0)
                {
                    var lastRow = dataGridView.Rows[lastRowIndex];
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
            catch (Exception ex)
            {
                _logger?.LogError("Error adding row to grid", ex);
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopBackgroundService();
            StopConnectionMonitor(); // ⭐ หยุด Connection Monitor
            _logger.LogInfo("Application closing - All services stopped");
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

                dataGridView.Columns.Add(viewButtonColumn);
            }
        }

        private void DataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                if (dataGridView.Columns[e.ColumnIndex].Name == "ViewButton")
                {
                    try
                    {
                        string time = dataGridView.Rows[e.RowIndex].Cells["Time Check"].Value?.ToString() ?? "N/A";
                        string orderNo = dataGridView.Rows[e.RowIndex].Cells["Order No"].Value?.ToString() ?? "N/A";
                        string status = dataGridView.Rows[e.RowIndex].Cells["Status"].Value?.ToString() ?? "N/A";

                        int actualRowIndex = -1;
                        if (_filteredDataView.Count > 0 && e.RowIndex < _filteredDataView.Count)
                        {
                            DataRowView rowView = _filteredDataView[e.RowIndex];
                            actualRowIndex = _processedDataTable.Rows.IndexOf(rowView.Row);
                        }

                        if (actualRowIndex >= 0 && _rowHL7Data.ContainsKey(actualRowIndex))
                        {
                            var hl7Message = _rowHL7Data[actualRowIndex];
                            DateTime? filterDate = DateTime.TryParse(time, out DateTime parsedDate) ? parsedDate.Date : (DateTime?)null;
                            var detailForm = new HL7DetailForm(hl7Message, filterDate, orderNo, status);
                            detailForm.ShowDialog();
                        }
                       
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error showing HL7 detail", ex);
                       
                    }
                }
            }
        }
        private void DataGridView_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            _logger?.LogInfo($"[Sort] Column '{dataGridView.Columns[e.ColumnIndex].Name}' clicked for sorting");

            // ⭐ เรียก ApplyRowColors หลังจาก sort เสร็จ
            this.BeginInvoke(new Action(() =>
            {
                _logger?.LogInfo("[Sort] Applying colors after sort");
                ApplyRowColors();
            }));
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
               

                // ⭐ หา Status column index
                int statusColumnIndex = -1;
                for (int i = 0; i < dataGridView.Columns.Count; i++)
                {
                    if (dataGridView.Columns[i].Name == "Status")
                    {
                        statusColumnIndex = i;
                        break;
                    }
                }

                if (statusColumnIndex == -1)
                {
                    _logger?.LogWarning("[ApplyRowColors] Status column not found!");
                    return;
                }

               

                // ⭐ Apply colors based on Status column by index
                int successCount = 0;
                int failedCount = 0;

                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    try
                    {
                        if (statusColumnIndex < row.Cells.Count && row.Cells[statusColumnIndex].Value != null)
                        {
                            string status = row.Cells[statusColumnIndex].Value.ToString().Trim();

                           

                            // ⭐ ลบการตั้งค่าเดิม
                            row.DefaultCellStyle.BackColor = System.Drawing.Color.White;

                            if (status == "Success")
                            {
                                row.DefaultCellStyle.BackColor = System.Drawing.Color.LightGreen;
                                row.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.Green;
                                successCount++;
                                
                            }
                            else if (status == "Failed")
                            {
                                row.DefaultCellStyle.BackColor = System.Drawing.Color.LightCoral;
                                row.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.Red;
                                failedCount++;
                                
                            }
                            else
                            {
                                row.DefaultCellStyle.BackColor = System.Drawing.Color.White;
                               
                            }
                        }
                        else
                        {
                           
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError($"[ApplyRowColors] Error on row {row.Index}: {ex.Message}", ex);
                    }
                }

               

                // ⭐ Force refresh
                dataGridView.Invalidate();
                dataGridView.Refresh();
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
                // ⭐ Manual check ใช้ CancellationToken ธรรมชาติ (ไม่สามารถ Cancel ได้)
                await CheckPendingOrders(isManual: true);
            }
        }

        private void StartBackgroundService()
        {
            var intervalMs = _intervalSeconds * 500;

            
            _backgroundCancellationTokenSource = new CancellationTokenSource();

            _backgroundTimer = new Timer(BackgroundTimerCallback, null, 0, intervalMs);

            startStopButton.Text = "Stop Service";
            UpdateStatus($"Service Running - Checking every {_intervalSeconds} seconds");
            _logger.LogInfo($"Background service started with {_intervalSeconds}s interval");
        }
        private async void StopBackgroundService()
        {
            _logger.LogInfo("StopBackgroundService: Starting stop process");

            // ⭐ Step 1: หยุด Timer ทันที
            if (_backgroundTimer != null)
            {
                _backgroundTimer.Dispose();
                _backgroundTimer = null;
                _logger.LogInfo("StopBackgroundService: Timer disposed");
            }

            // ⭐ Step 2: ยกเลิก Background Task ที่กำลังทำงาน
            if (_backgroundCancellationTokenSource != null)
            {
                _backgroundCancellationTokenSource.Cancel();
                _logger.LogInfo("StopBackgroundService: Cancellation requested");

                // รอให้ task รับการ Cancel - สูงสุด 5 วินาที
                int timeoutMs = 5000;
                int elapsedMs = 0;
                int checkIntervalMs = 100;

                while (_isProcessing && elapsedMs < timeoutMs)
                {
                    await Task.Delay(checkIntervalMs);
                    elapsedMs += checkIntervalMs;
                }

                if (_isProcessing)
                {
                    _logger.LogWarning("StopBackgroundService: Task did not stop within timeout, forcing stop");
                }

                // ⭐ Step 3: Dispose CancellationTokenSource
                _backgroundCancellationTokenSource.Dispose();
                _backgroundCancellationTokenSource = null;
            }

            // ⭐ Step 4: บังคับให้ _isProcessing = false
            _isProcessing = false;
            _logger.LogInfo("StopBackgroundService: _isProcessing set to false");

            // ⭐ Step 5: โหลดข้อมูลใหม่จาก Database เพื่อแสดงผลที่อัพเดทแล้ว
            await LoadDataBySelectedDate();

            startStopButton.Text = "Start Service";
            UpdateStatus("Service Stopped");
            _logger.LogInfo("StopBackgroundService: Completed");
        }

        private async Task CheckPendingOrders(bool isManual, CancellationToken cancellationToken = default)
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

                cancellationToken.ThrowIfCancellationRequested();

                var pending = await Task.Run(() => _databaseService.GetPendingDispenseData(), cancellationToken);
                _logger.LogInfo($"Background check: Found {pending.Count} pending orders");

                // ⭐ อัพเดทเวลาเจอไฟล์
                if (pending.Count > 0)
                {
                    UpdateLastFound(pending.Count);
                }

                cancellationToken.ThrowIfCancellationRequested();

                int remainingCount = pending.Count;

                this.Invoke(new Action(() =>
                {
                    this.Text = $"ConHIS Service - Pending: {remainingCount}";
                    if (remainingCount > 0)
                    {
                        UpdateStatus($"Processing {remainingCount} pending orders...");
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
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    _logger.LogInfo("Processing cancelled by user");
                                    return;
                                }

                                var hl7Message = result.ParsedMessage;
                                string orderNo = hl7Message?.CommonOrder?.PlacerOrderNumber ?? "N/A";
                                remainingCount--;

                                this.Invoke(new Action(() =>
                                {
                                    this.Text = $"ConHIS Service - Pending: {remainingCount}";
                                    if (remainingCount > 0)
                                    {
                                        UpdateStatus($"Processing... {remainingCount} orders remaining");
                                    }
                                }));

                                // ⭐ อัพเดทเวลา Success ถ้าส่งสำเร็จ
                                if (result.Success)
                                {
                                    UpdateLastSuccess(orderNo); // ⭐ เพิ่ม orderNo แทน UpdateLastSuccess();
                                }

                                
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
                            },
                            cancellationToken
                        );
                    }, cancellationToken);

                    _logger.LogInfo("Background check: Completed processing pending orders");

                    this.Invoke(new Action(() =>
                    {
                        this.Text = "ConHIS Service - Drug Dispense Monitor";

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
                        this.Text = "ConHIS Service - Drug Dispense Monitor";

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
            catch (OperationCanceledException)
            {
                _logger.LogInfo("Background check: Operation cancelled by user");
                this.Invoke(new Action(() =>
                {
                    this.Text = "ConHIS Service - Drug Dispense Monitor";
                    UpdateStatus("Service stopped - Current operation cancelled");
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError("Background check error", ex);

                this.Invoke(new Action(() =>
                {
                    this.Text = "ConHIS Service - Drug Dispense Monitor";
                    UpdateStatus($"Error: {ex.Message}");
                }));
            }
            finally
            {
                _isProcessing = false;

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
            // ⭐ ถ้ากำลังประมวลผลอยู่ให้ข้าม
            if (_isProcessing || _backgroundTimer == null)
            {
                return;
            }

            // ⭐ ถ้า CancellationTokenSource ถูก Dispose แล้ว ให้หยุด
            if (_backgroundCancellationTokenSource == null || _backgroundCancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            await CheckPendingOrders(isManual: false, _backgroundCancellationTokenSource.Token);
        }

        private void ShowAutoCloseMessageBox(string message, string title, int timeoutMs, bool shouldResumeService)
        {
            System.Threading.Timer timer = null;

            timer = new System.Threading.Timer(async (obj) =>
            {
                try
                {
                    // ปิด MessageBox อัตโนมัติ
                    IntPtr hwnd = FindWindow(null, title);
                    if (hwnd != IntPtr.Zero)
                    {
                        SendMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                    }

                    // ⭐ ถ้า Service ทำงานอยู่ก่อนหน้านี้ ให้เริ่มใหม่อัตโนมัติ
                    if (shouldResumeService)
                    {
                        await Task.Delay(500); // รอให้ MessageBox ปิดสนิท

                        this.Invoke(new Action(() =>
                        {
                            // ตรวจสอบว่า Service ยังไม่ได้เริ่มใหม่
                            if (_backgroundTimer == null)
                            {
                                _logger?.LogInfo("Auto-resuming service after database reconnection...");
                                StartBackgroundService(); // ⭐ เรียก StartBackgroundService
                                _logger?.LogInfo("Service auto-resumed successfully");
                            }
                        }));
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError("Error in auto-close MessageBox timer", ex);
                }
                finally
                {
                    timer?.Dispose();
                }
            }, null, timeoutMs, System.Threading.Timeout.Infinite);

            // แสดง MessageBox (จะถูกปิดโดย Timer หรือถ้าผู้ใช้คลิก OK)
            MessageBox.Show(
                message,
                title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
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
        private void UpdateLastFound(int foundCount)
        {
            if (foundCount > 0)
            {
                _lastFoundTime = DateTime.Now;

                if (lastFoundLabel.InvokeRequired)
                {
                    lastFoundLabel.Invoke(new Action(() =>
                    {
                        lastFoundLabel.Text = $"Last Found: {_lastFoundTime.Value:yyyy-MM-dd HH:mm:ss}";
                    }));
                    return;
                }

                lastFoundLabel.Text = $"Last Found: {_lastFoundTime.Value:yyyy-MM-dd HH:mm:ss}";
            }
        }
        private void UpdateLastSuccess(string orderId = null)
        {
            _lastSuccessTime = DateTime.Now;

            if (!string.IsNullOrEmpty(orderId))
            {
                _lastSuccessOrderId = orderId;
            }

            string orderInfo = !string.IsNullOrEmpty(_lastSuccessOrderId)
                ? $" | Order NO: {_lastSuccessOrderId}"
                : "";

            if (lastSuccessLabel.InvokeRequired)
            {
                lastSuccessLabel.Invoke(new Action(() =>
                {
                    lastSuccessLabel.Text = $"Last Success: {_lastSuccessTime.Value:yyyy-MM-dd HH:mm:ss}{orderInfo}";
                }));
                return;
            }

            lastSuccessLabel.Text = $"Last Success: {_lastSuccessTime.Value:yyyy-MM-dd HH:mm:ss}{orderInfo}";
        }
        private void UpdateConnectionStatus(bool isConnected)
        {
            _isDatabaseConnected = isConnected;

            if (connectionStatusLabel.InvokeRequired)
            {
                connectionStatusLabel.Invoke(new Action(() => UpdateConnectionStatus(isConnected)));
                return;
            }

            if (isConnected)
            {
                _lastDatabaseConnectionTime = DateTime.Now;
                string timeStr = _lastDatabaseConnectionTime.Value.ToString("yyyy-MM-dd HH:mm:ss");

                connectionStatusLabel.Text = $"Database: ✓ Connected (Last Connected: {timeStr})";
                connectionStatusLabel.ForeColor = System.Drawing.Color.Green;


            }
            else
            {
                _lastDatabaseDisconnectionTime = DateTime.Now;

                string lastConnectedStr = _lastDatabaseConnectionTime.HasValue
                    ? $"Last Connected: {_lastDatabaseConnectionTime.Value:yyyy-MM-dd HH:mm:ss}"
                    : "Never Connected";

                string disconnectedStr = _lastDatabaseDisconnectionTime.HasValue
                    ? $"Disconnected at: {_lastDatabaseDisconnectionTime.Value:yyyy-MM-dd HH:mm:ss}"
                    : "";

                connectionStatusLabel.Text = $"Database: ✗ Disconnected ({disconnectedStr}) | {lastConnectedStr}";
                connectionStatusLabel.ForeColor = System.Drawing.Color.Red;


            }
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

                totalCountLabel.Text = totalRecords.ToString();
                successCountLabel.Text = successCount.ToString();
                failedCountLabel.Text = failedCount.ToString();
                pendingCountLabel.Text = pendingCount.ToString();
                rejectCountLabel.Text = rejectCount.ToString();

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

        private void DrawPanelTopBar(PaintEventArgs e, System.Drawing.Color barColor)
        {
            using (var brush = new System.Drawing.SolidBrush(barColor))
            {
                e.Graphics.FillRectangle(brush, 0, 0, e.ClipRectangle.Width, 3);
            }
        }
        #endregion

        #region Settings
        private void SettingsButton_Click(object sender, EventArgs e)
        {
            try
            {
                int currentDays = _logger?.LogRetentionDays ?? 30;

                using (var settingsForm = new PagesFrom.SettingsForm(currentDays))
                {
                    if (settingsForm.ShowDialog() == DialogResult.OK)
                    {
                        if (_logger != null)
                        {
                            if (settingsForm.SaveToConfig)
                            {
                                _logger.ReloadLogRetentionDays();
                                UpdateStatus($"Settings saved permanently - Log retention: {_logger.LogRetentionDays} days");
                            }
                            else
                            {
                                _logger.UpdateLogRetentionDaysTemporary(settingsForm.LogRetentionDays);
                                UpdateStatus($"Settings updated temporarily - Log retention: {_logger.LogRetentionDays} days (session only)");
                            }

                            _logger.CleanOldLogs();

                            string permanentStatus = settingsForm.SaveToConfig ? "ถาวร (App.config)" : "ชั่วคราว (Session only)";

                            MessageBox.Show(
                                $"ระบบได้ทำความสะอาดไฟล์ log เก่าเรียบร้อยแล้ว\n\n" +
                                $"จำนวนวันเก็บ Log: {_logger.LogRetentionDays} วัน\n" +
                                $"สถานะ: {permanentStatus}",
                                "Log Cleanup Completed",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError("Error opening settings", ex);
                MessageBox.Show(
                    $"เกิดข้อผิดพลาดในการเปิด Settings:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
        #endregion
    }
}