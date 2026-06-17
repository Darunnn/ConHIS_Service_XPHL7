using ConHIS_Service_XPHL7.Configuration;
using ConHIS_Service_XPHL7.Models;
using ConHIS_Service_XPHL7.Services;
using ConHIS_Service_XPHL7.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows.Forms;
using Timer = System.Threading.Timer;

namespace ConHIS_Service_XPHL7
{
    public partial class Form1 : System.Windows.Forms.Form
    {
        #region ตัวแปร
        private AppConfig _appConfig;
        private DatabaseService _databaseService;
        private LogManager _logger;
        private DrugDispenseProcessor _processor;
        private DateTime? _lastFoundTime = null;
        private DateTime? _lastSuccessTime = null;
        private string _lastSuccessOrderId = null;

        // Windows API สำหรับปิด MessageBox อัตโนมัติ
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
        private const UInt32 WM_CLOSE = 0x0010;

        private int _intervalSeconds = 5;

        // ⭐ Connection Monitor
        private Timer _connectionCheckTimer;
        private DateTime? _lastDatabaseDisconnectionTime = null;
        private readonly int _connectionCheckIntervalSeconds = 5;
        private bool _isCheckingConnection = false;
        private DateTime? _lastDatabaseConnectionTime = null;
        private bool _hasNotifiedDisconnection = false;
        private bool _hasNotifiedReconnection = false;

        // DataTable for DataGridView
        private DataTable _processedDataTable;
        private DataView _filteredDataView;

        // เก็บ HL7Message ที่เชื่อมกับแต่ละแถว
        private System.Collections.Generic.Dictionary<int, HL7Message> _rowHL7Data =
            new System.Collections.Generic.Dictionary<int, HL7Message>();

        // Connection status
        private bool _isDatabaseConnected = false;
        private bool _isInitializing = false;

        // ⭐ IPD/OPD Services
        private CancellationTokenSource _ipdCancellationTokenSource = null;
        private CancellationTokenSource _opdCancellationTokenSource = null;
        private Timer _ipdTimer;
        private Timer _opdTimer;
        private bool _isIPDProcessing = false;
        private bool _isOPDProcessing = false;
        private bool _wasIPDRunningBeforeDisconnection = false;
        private bool _wasOPDRunningBeforeDisconnection = false;

        // ⭐ Auto-start flags (เก็บว่าตอน load ควร auto-start อะไร)
        private bool _autoStartIPD = false;
        private bool _autoStartOPD = false;

        #endregion

        #region Additional Variables for Table Status
        private bool _ipdTableExists = false;
        private bool _opdTableExists = false;
        #endregion

        private EncodingService _encodingService;

        // ⭐ ตรวจสอบว่า Table มีอยู่หรือไม่
        private async Task<bool> CheckTableExists(string tableName)
        {
            try
            {
                return await Task.Run(() => _databaseService?.CheckTableExists(tableName) ?? false);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error checking table '{tableName}'", ex);
                return false;
            }
        }

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;
            _logger = new LogManager();
        }

        private void InitializeDataTable()
        {
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
            _processedDataTable.Columns.Add("Service Type", typeof(string));
            _processedDataTable.Columns.Add("Order No", typeof(string));
            _processedDataTable.Columns.Add("HN", typeof(string));
            _processedDataTable.Columns.Add("Patient Name", typeof(string));
            _processedDataTable.Columns.Add("FinancialClass", typeof(string));
            _processedDataTable.Columns.Add("OrderControl", typeof(string));
            _processedDataTable.Columns.Add("Status", typeof(string));
            _processedDataTable.Columns.Add("API Response", typeof(string));

            _filteredDataView = new DataView(_processedDataTable);
            dataGridView.DataSource = _filteredDataView;

            dataGridView.ColumnHeaderMouseClick += DataGridView_ColumnHeaderMouseClick;
            dataGridView.CellClick += DataGridView_CellClick;

            UpdateStatusSummary();

            try
            {
                if (dataGridView.Columns.Count >= 10)
                {
                    dataGridView.Columns["Time Check"].Width = 165;
                    dataGridView.Columns["Transaction DateTime"].Width = 165;
                    dataGridView.Columns["Service Type"].Width = 80;
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

        private void ResetNotificationFlags()
        {
            _hasNotifiedDisconnection = false;
            _hasNotifiedReconnection = false;
        }

        // ⭐ เริ่ม Connection Monitor
        private void StartConnectionMonitor()
        {
            var intervalMs = _connectionCheckIntervalSeconds * 1000;
            _connectionCheckTimer = new Timer(ConnectionCheckCallback, null, intervalMs, intervalMs);
            ResetNotificationFlags();
            _logger?.LogInfo($"Connection monitor started - checking every {_connectionCheckIntervalSeconds} seconds");
        }

        // ⭐ หยุด Connection Monitor
        private void StopConnectionMonitor()
        {
            _connectionCheckTimer?.Dispose();
            _connectionCheckTimer = null;
            ResetNotificationFlags();
            _logger?.LogInfo("Connection monitor stopped");
        }

        // ⭐ ShowAutoCloseMessageBox - ปิด MessageBox อัตโนมัติ
        // แยก resume service ออกจาก MessageBox โดยสิ้นเชิง
        // เพื่อป้องกัน race condition ระหว่าง WM_CLOSE กับ blocking MessageBox.Show()
        private void ShowAutoCloseMessageBox(string message, string title, int timeoutMs,
            bool shouldResumeIPD, bool shouldResumeOPD)
        {
            // ⭐ ถ้าต้อง resume service → schedule ไว้ก่อนเปิด MessageBox เลย
            // ใช้ Task.Delay แยก thread เพื่อไม่ให้ถูก block โดย MessageBox.Show()
            if (shouldResumeIPD || shouldResumeOPD)
            {
                // รอให้ MessageBox ปิดก่อน (timeout + buffer 500ms) แล้วค่อย resume
                int resumeDelayMs = timeoutMs + 800;
                Task.Run(async () =>
                {
                    await Task.Delay(resumeDelayMs);

                    if (!this.IsHandleCreated || this.IsDisposed) return;

                    try
                    {
                        this.Invoke(new Action(() =>
                        {
                            try
                            {
                                if (shouldResumeIPD && _ipdTimer == null && _ipdTableExists)
                                {
                                    _logger?.LogInfo("Auto-resuming IPD Service after reconnect");
                                    StartIPDService();
                                }
                                if (shouldResumeOPD && _opdTimer == null && _opdTableExists)
                                {
                                    _logger?.LogInfo("Auto-resuming OPD Service after reconnect");
                                    StartOPDService();
                                }

                                // ⭐ อัปเดตสถานะปุ่ม manual check
                                bool anyRunning = (_ipdTimer != null) || (_opdTimer != null);
                                manualCheckButton.Enabled = !anyRunning;
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogError("Error resuming services after reconnect", ex);
                            }
                        }));
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError("Error invoking resume on UI thread", ex);
                    }
                });
            }

            // ⭐ ปิด MessageBox อัตโนมัติด้วย WM_CLOSE
            System.Threading.Timer closeTimer = null;
            closeTimer = new System.Threading.Timer((obj) =>
            {
                try
                {
                    IntPtr hwnd = FindWindow(null, title);
                    if (hwnd != IntPtr.Zero)
                    {
                        SendMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError("Error closing MessageBox", ex);
                }
                finally
                {
                    closeTimer?.Dispose();
                }
            }, null, timeoutMs, System.Threading.Timeout.Infinite);

            // Blocking call — แต่ resume service ถูก schedule ไว้แล้วใน Task.Run ข้างบน
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ⭐ ConnectionCheckCallback
        private async void ConnectionCheckCallback(object state)
        {
            if (_isCheckingConnection) return;
            _isCheckingConnection = true;

            try
            {
                bool opdConnected = await Task.Run(() => _databaseService?.TestConnection() ?? false);
                bool ipdConnected = await Task.Run(() => _databaseService?.TestIPDConnection() ?? false);
                bool isConnected = opdConnected || ipdConnected;

                _logger?.LogConnectDatabase(isConnected, _lastDatabaseConnectionTime, _lastDatabaseDisconnectionTime);

                if (isConnected != _isDatabaseConnected)
                {
                    if (isConnected)
                    {
                        // ✅ Reconnected
                        this.Invoke(new Action(async () =>
                        {
                            try
                            {
                                UpdateConnectionStatus(true, opdConnected, ipdConnected);

                                _logger?.LogInfo("Rechecking database tables after reconnection...");

                                if (ipdConnected)
                                    _ipdTableExists = await CheckTableExists("drug_dispense_ipd");
                                else
                                    _ipdTableExists = false;

                                if (opdConnected)
                                    _opdTableExists = await CheckTableExists("drug_dispense_opd");
                                else
                                    _opdTableExists = false;

                                _logger?.LogInfo(
                                    $"Table Status - IPD: {(_ipdTableExists ? "EXISTS" : "NOT FOUND")}, " +
                                    $"OPD: {(_opdTableExists ? "EXISTS" : "NOT FOUND")}");

                                UpdateServiceButtonStates();
                                await LoadDataBySelectedDate();
                                UpdateStatus("✓ Database reconnected - Data refreshed");

                                if (!_hasNotifiedReconnection)
                                {
                                    _hasNotifiedReconnection = true;
                                    _hasNotifiedDisconnection = false;

                                    // ⭐ Auto-resume เฉพาะถ้าก่อน disconnect กำลัง running อยู่
                                    bool shouldResumeIPD = _wasIPDRunningBeforeDisconnection && _ipdTableExists;
                                    bool shouldResumeOPD = _wasOPDRunningBeforeDisconnection && _opdTableExists;

                                    string serviceMessage = "";
                                    if (shouldResumeIPD && shouldResumeOPD)
                                        serviceMessage = "\n\n⚡ Both IPD & OPD Services will resume in 3 seconds...";
                                    else if (shouldResumeIPD)
                                        serviceMessage = "\n\n⚡ IPD Service will resume in 3 seconds...";
                                    else if (shouldResumeOPD)
                                        serviceMessage = "\n\n⚡ OPD Service will resume in 3 seconds...";

                                    string tableWarning = "";
                                    if (!ipdConnected)
                                        tableWarning += "\n⚠️ IPD Database still disconnected - IPD Service disabled";
                                    else if (_wasIPDRunningBeforeDisconnection && !_ipdTableExists)
                                        tableWarning += "\n⚠️ IPD table not found - IPD Service disabled";

                                    if (!opdConnected)
                                        tableWarning += "\n⚠️ OPD Database still disconnected - OPD Service disabled";
                                    else if (_wasOPDRunningBeforeDisconnection && !_opdTableExists)
                                        tableWarning += "\n⚠️ OPD table not found - OPD Service disabled";

                                    string dbStatus = "";
                                    if (opdConnected && ipdConnected)
                                        dbStatus = "✅ OPD & IPD Databases restored!";
                                    else if (opdConnected)
                                        dbStatus = "✅ OPD Database restored!\n⚠️ IPD Database still disconnected";
                                    else
                                        dbStatus = "✅ IPD Database restored!\n⚠️ OPD Database still disconnected";

                                    // ⭐ shouldResume ถูกส่งเข้า ShowAutoCloseMessageBox
                                    // ซึ่งจะ schedule Task.Run ไว้รอ timeout+800ms แล้วค่อย start service
                                    // (แยกออกจาก blocking MessageBox.Show() โดยสิ้นเชิง)
                                    this.BeginInvoke(new Action(() =>
                                    {
                                        ShowAutoCloseMessageBox(
                                            $"{dbStatus}\n\n" +
                                            $"📅 Reconnected at: {_lastDatabaseConnectionTime.Value:yyyy-MM-dd HH:mm:ss}\n" +
                                            $"🔄 Data refreshed automatically." +
                                            tableWarning +
                                            serviceMessage,
                                            "Connection Restored",
                                            3000,
                                            shouldResumeIPD,
                                            shouldResumeOPD
                                        );
                                    }));
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogError("Error after reconnection", ex);
                            }
                        }));
                    }
                    else
                    {
                        // ❌ Disconnected
                        this.Invoke(new Action(() =>
                        {
                            UpdateConnectionStatus(false, false, false);

                            _ipdTableExists = false;
                            _opdTableExists = false;
                            UpdateServiceButtonStates();

                            // ⭐ บันทึกสถานะก่อน disconnect
                            _wasIPDRunningBeforeDisconnection = (_ipdTimer != null);
                            _wasOPDRunningBeforeDisconnection = (_opdTimer != null);

                            if (_wasIPDRunningBeforeDisconnection)
                            {
                                _logger?.LogWarning("IPD Service running - Auto-stopping due to DB disconnect");
                                StopIPDService();
                            }
                            if (_wasOPDRunningBeforeDisconnection)
                            {
                                _logger?.LogWarning("OPD Service running - Auto-stopping due to DB disconnect");
                                StopOPDService();
                            }

                            UpdateStatus("✗ Database connection lost - Reconnecting...");

                            if (!_hasNotifiedDisconnection)
                            {
                                _hasNotifiedDisconnection = true;
                                _hasNotifiedReconnection = false;

                                string serviceMessage = "";
                                if (_wasIPDRunningBeforeDisconnection && _wasOPDRunningBeforeDisconnection)
                                    serviceMessage = "\n\n⏸️ Both services stopped and will auto-resume when reconnected.";
                                else if (_wasIPDRunningBeforeDisconnection)
                                    serviceMessage = "\n\n⏸️ IPD Service stopped and will auto-resume when reconnected.";
                                else if (_wasOPDRunningBeforeDisconnection)
                                    serviceMessage = "\n\n⏸️ OPD Service stopped and will auto-resume when reconnected.";

                                this.BeginInvoke(new Action(() =>
                                {
                                    ShowAutoCloseMessageBox(
                                        $"❌ Database connection lost!\n\n" +
                                        $"📅 Lost at: {_lastDatabaseDisconnectionTime.Value:yyyy-MM-dd HH:mm:ss}\n" +
                                        $"🔄 Reconnecting every {_connectionCheckIntervalSeconds} seconds." +
                                        serviceMessage,
                                        "Connection Lost",
                                        3000,
                                        false,
                                        false
                                    );
                                }));
                            }
                        }));
                    }
                }
                else if (isConnected)
                {
                    // สถานะไม่เปลี่ยน แต่ OPD/IPD อาจเปลี่ยน — อัปเดต UI เงียบๆ
                    this.Invoke(new Action(() =>
                    {
                        UpdateConnectionStatus(true, opdConnected, ipdConnected);

                        if (!ipdConnected && _ipdTimer != null)
                        {
                            _logger?.LogWarning("IPD DB lost during operation - Auto-stopping IPD");
                            _wasIPDRunningBeforeDisconnection = true;
                            StopIPDService();
                        }
                        if (!opdConnected && _opdTimer != null)
                        {
                            _logger?.LogWarning("OPD DB lost during operation - Auto-stopping OPD");
                            _wasOPDRunningBeforeDisconnection = true;
                            StopOPDService();
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError("Connection check error", ex);
                this.Invoke(new Action(() =>
                {
                    UpdateConnectionStatus(false, false, false);
                    _ipdTableExists = false;
                    _opdTableExists = false;
                    UpdateServiceButtonStates();
                }));
            }
            finally
            {
                _isCheckingConnection = false;
            }
        }

        private void InitializePanelPaintEvents()
        {
            if (string.IsNullOrEmpty(_currentStatusFilter))
            {
                _currentStatusFilter = "All";
            }

            totalPanel.Paint += (s, e) => DrawPanelTopBar(totalPanel, e, System.Drawing.Color.Gray, "All");
            successPanel.Paint += (s, e) => DrawPanelTopBar(successPanel, e, System.Drawing.Color.Green, "Success");
            failedPanel.Paint += (s, e) => DrawPanelTopBar(failedPanel, e, System.Drawing.Color.Red, "Failed");
            ipdPanel.Paint += (s, e) => DrawPanelTopBar(ipdPanel, e, System.Drawing.Color.FromArgb(52, 152, 219), "IPD");
            opdPanel.Paint += (s, e) => DrawPanelTopBar(opdPanel, e, System.Drawing.Color.FromArgb(46, 204, 113), "OPD");

            totalPanel.Click += TotalPanel_Click;
            successPanel.Click += SuccessPanel_Click;
            failedPanel.Click += FailedPanel_Click;
            ipdPanel.Click += IPDPanel_Click;
            opdPanel.Click += OPDPanel_Click;

            foreach (Control ctrl in totalPanel.Controls)
                if (ctrl is Label) { ctrl.Click += TotalPanel_Click; ctrl.Cursor = System.Windows.Forms.Cursors.Hand; }
            foreach (Control ctrl in successPanel.Controls)
                if (ctrl is Label) { ctrl.Click += SuccessPanel_Click; ctrl.Cursor = System.Windows.Forms.Cursors.Hand; }
            foreach (Control ctrl in failedPanel.Controls)
                if (ctrl is Label) { ctrl.Click += FailedPanel_Click; ctrl.Cursor = System.Windows.Forms.Cursors.Hand; }
            foreach (Control ctrl in ipdPanel.Controls)
                if (ctrl is Label) { ctrl.Click += IPDPanel_Click; ctrl.Cursor = System.Windows.Forms.Cursors.Hand; }
            foreach (Control ctrl in opdPanel.Controls)
                if (ctrl is Label) { ctrl.Click += OPDPanel_Click; ctrl.Cursor = System.Windows.Forms.Cursors.Hand; }

            totalPanel.Cursor = System.Windows.Forms.Cursors.Hand;
            successPanel.Cursor = System.Windows.Forms.Cursors.Hand;
            failedPanel.Cursor = System.Windows.Forms.Cursors.Hand;
            ipdPanel.Cursor = System.Windows.Forms.Cursors.Hand;
            opdPanel.Cursor = System.Windows.Forms.Cursors.Hand;
        }

        private async Task LoadDataBySelectedDate()
        {
            try
            {
                DateTime selectedDate = dateTimePicker.Value.Date;
                string searchText = searchTextBox.Text.Trim();

                UpdateStatus($"Loading data for {selectedDate:yyyy-MM-dd}...");

                _processedDataTable.Rows.Clear();
                _rowHL7Data.Clear();

                if (_filteredDataView != null)
                {
                    _filteredDataView.RowFilter = string.Empty;
                }

                List<DrugDispenseipd> dispenseData = null;

                if (!_ipdTableExists && !_opdTableExists)
                {
                    UpdateStatus("✗ No database tables available");
                    return;
                }

                dispenseData = await Task.Run(() =>
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(searchText))
                        {
                            _logger.LogInfo($"Search: {searchText} on {selectedDate:yyyy-MM-dd} (Tables: IPD={_ipdTableExists}, OPD={_opdTableExists})");
                            return _databaseService.GetAllDispenseDataByDateAndSearch(selectedDate, searchText, _ipdTableExists, _opdTableExists);
                        }
                        else
                        {
                            _logger.LogInfo($"Load: {selectedDate:yyyy-MM-dd} (Tables: IPD={_ipdTableExists}, OPD={_opdTableExists})");
                            return _databaseService.GetAllDispenseDataByDate(selectedDate, _ipdTableExists, _opdTableExists);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error querying database", ex);
                        return new List<DrugDispenseipd>();
                    }
                });

                if (dispenseData == null)
                    dispenseData = new List<DrugDispenseipd>();

                _logger.LogInfo($"[LoadDataBySelectedDate] Database returned {dispenseData.Count} records");

                var hl7Service = new HL7Service();
                int loadedCount = 0;
                int skippedCount = 0;
                int ipdRecordCount = 0;
                int opdRecordCount = 0;

                foreach (var data in dispenseData)
                {
                    string dispenseId = data.DrugDispenseipdId.ToString();

                    try
                    {
                        if (data.Hl7Data == null || data.Hl7Data.Length == 0)
                        {
                            skippedCount++;
                            continue;
                        }

                        string hl7String = _encodingService.DecodeHl7Data(data.Hl7Data, data.RecieveOrderType);

                        if (string.IsNullOrWhiteSpace(hl7String))
                        {
                            skippedCount++;
                            continue;
                        }

                        string serviceType = "N/A";
                        if (!string.IsNullOrEmpty(data.RecieveOrderType))
                        {
                            if (data.RecieveOrderType.Contains("IPD"))
                                serviceType = "IPD";
                            else if (data.RecieveOrderType.Contains("OPD"))
                                serviceType = "OPD";
                            else
                                serviceType = data.RecieveOrderType;
                        }

                        if (serviceType == "IPD") ipdRecordCount++;
                        else if (serviceType == "OPD") opdRecordCount++;

                        HL7Message hl7Message = null;

                        try
                        {
                            hl7Message = hl7Service.ParseHL7Message(hl7String);
                        }
                        catch (Exception parseEx)
                        {
                            _logger?.LogError($"✗ Failed to parse HL7 for {dispenseId}: {parseEx.Message}", parseEx);
                            _logger?.LogReadError(dispenseId, $"Parse Error: {parseEx.Message}\n{parseEx.StackTrace}");
                            skippedCount++;
                            continue;
                        }

                        DateTime timeCheckDate = DateTime.Now;
                        if (data.RecieveStatusDatetime.HasValue && data.RecieveStatusDatetime.Value != DateTime.MinValue)
                            timeCheckDate = data.RecieveStatusDatetime.Value;
                        else if (data.DrugDispenseDatetime != DateTime.MinValue)
                            timeCheckDate = data.DrugDispenseDatetime;
                        else if (hl7Message?.CommonOrder?.TransactionDateTime.HasValue == true)
                            timeCheckDate = hl7Message.CommonOrder.TransactionDateTime.Value;

                        string timeCheck = timeCheckDate.ToString("yyyy-MM-dd HH:mm:ss");
                        string transactionDateTime = data.DrugDispenseDatetime.ToString("yyyy-MM-dd HH:mm:ss");
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

                        string orderControl = hl7Message?.CommonOrder?.OrderControl ?? "N/A";

                        string status = "N/A";
                        if (data.RecieveStatus == 'Y')
                        {
                            status = "Success";
                            if (!_lastSuccessTime.HasValue || timeCheckDate > _lastSuccessTime.Value)
                            {
                                _lastSuccessTime = timeCheckDate;
                                UpdateLastSuccess(orderNo);
                            }
                        }
                        else if (data.RecieveStatus == 'F')
                        {
                            status = "Failed";
                        }
                        else if (data.RecieveStatus == 'N')
                        {
                            skippedCount++;
                            continue;
                        }

                        AddRowToGrid(timeCheck, transactionDateTime, serviceType, orderNo, hn, patientName,
                                     financialClass, orderControl, status, "Database Record", hl7Message);
                        loadedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError($"❌ Error loading record {dispenseId}: {ex.Message}", ex);
                        _logger?.LogReadError(dispenseId,
                            $"Failed to process record: {ex.Message}\nStackTrace: {ex.StackTrace}");
                        skippedCount++;
                    }
                }

                _currentStatusFilter = "All";
                _filteredDataView.RowFilter = string.Empty;
                _filteredDataView.Sort = "[Time Check] DESC";
                ApplyRowColors();
                UpdateStatusSummary();
                UpdateStatusFilterButtons();

                string tableInfo = "";
                if (_ipdTableExists && _opdTableExists) tableInfo = "IPD+OPD";
                else if (_ipdTableExists) tableInfo = "IPD only";
                else if (_opdTableExists) tableInfo = "OPD only";

                if (loadedCount > 0)
                    UpdateStatus($"✓ Loaded {loadedCount} records ({tableInfo}) | IPD={ipdRecordCount}, OPD={opdRecordCount}");
                else
                    UpdateStatus($"✗ No records found ({tableInfo})");
            }
            catch (Exception ex)
            {
                _logger?.LogError("Error loading data by date", ex);
                UpdateStatus($"✗ Error: {ex.Message}");
            }
        }

        // ==========================================
        // ⭐⭐⭐ Form1_Load - AUTO START เมื่อเปิดโปรแกรม
        // ==========================================
        private async void Form1_Load(object sender, EventArgs e)
        {
            _logger.LogInfo("Start Interface");
            UpdateStatus("Initializing...");
            _currentStatusFilter = "All";
            InitializeDataTable();
            UpdateConnectionStatus(false);

            try
            {
                _appConfig = new AppConfig();
                _appConfig.LoadConfiguration();

                _intervalSeconds = _appConfig.ProcessingIntervalSeconds;

                if (_intervalSeconds < 5)
                    _logger.LogWarning($"⚠️ Very fast interval ({_intervalSeconds}s) may cause high load!");
                else if (_intervalSeconds <= 10)
                    _logger.LogInfo($"ℹ️ Using fast interval ({_intervalSeconds}s) - Monitor system load");

                _encodingService = EncodingService.FromConnectionConfig(_logger.LogInfo);
                _logger.LogInfo($"OPD Encoding: {_encodingService.CurrentEncoding}");
                _logger.LogInfo($"IPD Encoding: {_encodingService.CurrentIPDEncoding}");

                if (int.TryParse(System.Configuration.ConfigurationManager.AppSettings["LogRetentionDays"],
                        out int retentionDays))
                {
                    _logger.LogRetentionDays = retentionDays;
                    _logger.LogInfo($"Log retention days loaded: {retentionDays} days");
                }

                _logger.LogInfo("Connecting to database");
                _databaseService = new DatabaseService(
                    _appConfig.ConnectionString,
                    _appConfig.IpdConnectionString
                );

                bool opdConnected = await Task.Run(() => _databaseService.TestConnection());
                bool ipdConnected = await Task.Run(() => _databaseService.TestIPDConnection());
                bool anyConnected = opdConnected || ipdConnected;

                UpdateConnectionStatus(anyConnected, opdConnected, ipdConnected);

                if (anyConnected)
                {
                    _logger.LogInfo($"DatabaseService initialized - OPD: {(opdConnected ? "✓" : "✗")}, IPD: {(ipdConnected ? "✓" : "✗")}");

                    _logger.LogInfo("Checking database tables...");

                    if (ipdConnected)
                        _ipdTableExists = await CheckTableExists("drug_dispense_ipd");
                    else
                    {
                        _ipdTableExists = false;
                        _logger.LogWarning("Skipping IPD table check - IPD DB not connected");
                    }

                    if (opdConnected)
                        _opdTableExists = await CheckTableExists("drug_dispense_opd");
                    else
                    {
                        _opdTableExists = false;
                        _logger.LogWarning("Skipping OPD table check - OPD DB not connected");
                    }

                    _logger.LogInfo($"Table Status - IPD: {(_ipdTableExists ? "EXISTS" : "NOT FOUND")}, OPD: {(_opdTableExists ? "EXISTS" : "NOT FOUND")}");

                    // ⭐ แสดง warning ถ้า DB connect ได้แต่ table ไม่มี
                    var warnings = new System.Text.StringBuilder();
                    if (ipdConnected && !_ipdTableExists)
                        warnings.AppendLine("• drug_dispense_ipd (IPD DB connected but table missing)");
                    if (opdConnected && !_opdTableExists)
                        warnings.AppendLine("• drug_dispense_opd (OPD DB connected but table missing)");
                    if (!ipdConnected)
                        warnings.AppendLine("• IPD Database - Connection failed");
                    if (!opdConnected)
                        warnings.AppendLine("• OPD Database - Connection failed");

                    if (warnings.Length > 0)
                    {
                        string warningText = warnings.ToString();
                        _logger.LogWarning($"⚠️ Issues detected:\n{warningText}");

                        this.BeginInvoke(new Action(() =>
                        {
                            ShowAutoCloseMessageBox(
                                $"⚠️ Database Warning\n\n" +
                                $"Issues detected:\n{warningText}\n" +
                                $"IPD Service: {(_ipdTableExists ? "Available" : "Disabled")}\n" +
                                $"OPD Service: {(_opdTableExists ? "Available" : "Disabled")}\n\n" +
                                $"Please check connection and tables.",
                                "Database Warning",
                                3000,
                                false,
                                false
                            );
                        }));
                    }

                    UpdateServiceButtonStates();

                    _isInitializing = true;
                    dateTimePicker.Value = DateTime.Today;
                    _isInitializing = false;

                    await LoadDataBySelectedDate();
                }
                else
                {
                    _logger.LogWarning("Both OPD and IPD database connections failed");
                }

                var apiService = new ApiService(AppConfig.ApiEndpoint);
                var hl7Service = new HL7Service();
                _processor = new DrugDispenseProcessor(_databaseService, hl7Service, apiService);

                InitializePanelPaintEvents();
                UpdateStatusFilterButtons();
                StartConnectionMonitor();

                manualCheckButton.Enabled = true;
                exportButton.Enabled = true;
                settingsButton.Enabled = true;
                UpdateServiceButtonStates();
                InitializeExportButton();

                // ⭐⭐⭐ AUTO START เมื่อเปิดโปรแกรม
                // start service ก่อน แล้วค่อยแสดง popup แยกต่างหาก
                _logger.LogInfo("Checking auto-start conditions...");

                bool autoStartedAny = false;

                if (_ipdTableExists)
                {
                    _logger.LogInfo("Auto-starting IPD Service on program load");
                    StartIPDService();
                    autoStartedAny = true;
                }
                else
                {
                    _logger.LogInfo("IPD auto-start skipped - table not available");
                }

                if (_opdTableExists)
                {
                    _logger.LogInfo("Auto-starting OPD Service on program load");
                    StartOPDService();
                    autoStartedAny = true;
                }
                else
                {
                    _logger.LogInfo("OPD auto-start skipped - table not available");
                }

                if (autoStartedAny)
                {
                    // ⭐ ปิด manual check ขณะ service กำลัง run
                    manualCheckButton.Enabled = false;

                    string autoStartMsg = "";
                    if (_ipdTableExists && _opdTableExists)
                        autoStartMsg = "✅ IPD & OPD Services started automatically";
                    else if (_ipdTableExists)
                        autoStartMsg = "✅ IPD Service started automatically";
                    else if (_opdTableExists)
                        autoStartMsg = "✅ OPD Service started automatically";

                    // ⭐ แสดง popup แบบ non-blocking — service เริ่มแล้ว ไม่ต้อง resume
                    // ใช้ BeginInvoke เพื่อให้ Form แสดงผลก่อน แล้ว popup ค่อยขึ้น
                    this.BeginInvoke(new Action(() =>
                    {
                        ShowAutoCloseMessageBox(
                            $"🚀 Auto Start\n\n{autoStartMsg}\n\nInterval: {_intervalSeconds} seconds",
                            "Service Auto Started",
                            3000,
                            false,   // ไม่ต้อง resume — start ไปแล้วก่อนแสดง popup
                            false
                        );
                    }));
                }
                else
                {
                    UpdateStatus("Ready - No tables available for auto-start");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to initialize", ex);
                UpdateStatus($"Error: {ex.Message}");
                UpdateConnectionStatus(false);
            }
        }

        #region Export HL7 Data

        private string _selectedOrderNo = null;
        private string _selectedServiceType = null;
        private int _selectedRowIndex = -1;

        private void InitializeExportButton()
        {
            dataGridView.SelectionChanged += DataGridView_SelectionChanged;

            if (!groupBox2.Controls.Contains(exportButton))
            {
                exportButton.Location = new System.Drawing.Point(575, 20);
                exportButton.Size = new System.Drawing.Size(120, 32);
                exportButton.Text = "📥 Export HL7";
                exportButton.Enabled = false;
                exportButton.Visible = true;
                groupBox2.Controls.Add(exportButton);
            }
        }

        private void DataGridView_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView.SelectedRows.Count > 0)
                {
                    var selectedRow = dataGridView.SelectedRows[0];
                    _selectedOrderNo = selectedRow.Cells["Order No"]?.Value?.ToString();
                    _selectedServiceType = selectedRow.Cells["Service Type"]?.Value?.ToString();

                    if (_filteredDataView.Count > 0 && selectedRow.Index < _filteredDataView.Count)
                    {
                        DataRowView rowView = _filteredDataView[selectedRow.Index];
                        _selectedRowIndex = _processedDataTable.Rows.IndexOf(rowView.Row);
                    }

                    exportButton.Enabled = !string.IsNullOrEmpty(_selectedOrderNo) &&
                                           !string.IsNullOrEmpty(_selectedServiceType);

                    _logger?.LogInfo($"Row selected - OrderNo: {_selectedOrderNo}, ServiceType: {_selectedServiceType}");
                }
                else
                {
                    _selectedOrderNo = null;
                    _selectedServiceType = null;
                    _selectedRowIndex = -1;
                    exportButton.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError("Error in SelectionChanged event", ex);
                exportButton.Enabled = false;
            }
        }

        private void ExportButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_selectedOrderNo) || string.IsNullOrEmpty(_selectedServiceType))
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        ShowAutoCloseMessageBox(
                            "กรุณาเลือกแถวข้อมูลที่ต้องการ Export",
                            "No Selection",
                            2000,
                            false,
                            false
                        );
                    }));
                    return;
                }

                _logger?.LogInfo($"[Export] Start - OrderNo: {_selectedOrderNo}, ServiceType: {_selectedServiceType}");

                byte[] hl7Data = _databaseService.GetHL7DataByOrderNoAndType(_selectedOrderNo, _selectedServiceType);

                if (hl7Data == null || hl7Data.Length == 0)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        ShowAutoCloseMessageBox(
                            $"ไม่พบข้อมูล HL7 สำหรับ:\n" +
                            $"Order No: {_selectedOrderNo}\n" +
                            $"Service Type: {_selectedServiceType}",
                            "Data Not Found",
                            2000,
                            false,
                            false
                        );
                    }));
                    _logger?.LogWarning($"[Export] No HL7 data found");
                    return;
                }

                _logger?.LogInfo($"[Export] Retrieved HL7 data - Size: {hl7Data.Length} bytes");

                using (var saveFileDialog = new SaveFileDialog())
                {
                    string safeOrderNo = SanitizeFileName(_selectedOrderNo);
                    string safeServiceType = SanitizeFileName(_selectedServiceType);

                    saveFileDialog.Filter = "HL7 Binary Files (*.bin)|*.bin|HL7 Text Files (*.hl7)|*.hl7|All Files (*.*)|*.*";
                    saveFileDialog.FilterIndex = 1;
                    saveFileDialog.FileName = $"HL7_{safeServiceType}_{safeOrderNo}_{DateTime.Now:yyyyMMdd_HHmmss}";
                    saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    saveFileDialog.Title = "Export HL7 Data";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string filePath = saveFileDialog.FileName;
                        File.WriteAllBytes(filePath, hl7Data);
                        _logger?.LogInfo($"[Export] Success - File saved to: {filePath}");

                        this.BeginInvoke(new Action(() =>
                        {
                            ShowAutoCloseMessageBox(
                                $"✓ Export สำเร็จ!\n\n" +
                                $"Order No: {_selectedOrderNo}\n" +
                                $"Service Type: {_selectedServiceType}\n" +
                                $"File Size: {FormatFileSize(hl7Data.Length)}\n" +
                                $"Location: {filePath}",
                                "Export Success",
                                2000,
                                false,
                                false
                            );
                        }));

                        if (MessageBox.Show(
                            "ต้องการเปิด Folder ที่เก็บไฟล์หรือไม่?",
                            "Open Folder",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError("[Export] Error exporting HL7 data", ex);
                this.BeginInvoke(new Action(() =>
                {
                    ShowAutoCloseMessageBox(
                        $"เกิดข้อผิดพลาดในการ Export:\n\n{ex.Message}",
                        "Export Error",
                        2000,
                        false,
                        false
                    );
                }));
            }
        }

        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return "unknown";
            var invalidChars = Path.GetInvalidFileNameChars();
            var result = new StringBuilder(fileName.Length);
            foreach (var c in fileName)
            {
                if (Array.IndexOf(invalidChars, c) >= 0) result.Append('_');
                else result.Append(c);
            }
            return result.ToString();
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        #endregion

        #region Search and Filter
        private string _currentStatusFilter = "All";

        private async void DateTimePicker_ValueChanged(object sender, EventArgs e)
        {
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

        private void IPDPanel_Click(object sender, EventArgs e)
        {
            _currentStatusFilter = "IPD";
            ApplyOrderTypeFilter("IPD");
            UpdateStatusFilterButtons();
        }

        private void OPDPanel_Click(object sender, EventArgs e)
        {
            _currentStatusFilter = "OPD";
            ApplyOrderTypeFilter("OPD");
            UpdateStatusFilterButtons();
        }

        private void UpdateStatusFilterButtons()
        {
            totalPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            successPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            failedPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            ipdPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            opdPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

            if (_currentStatusFilter == "All")
                totalPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            else if (_currentStatusFilter == "Success")
                successPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            else if (_currentStatusFilter == "Failed")
                failedPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            else if (_currentStatusFilter == "IPD")
                ipdPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            else if (_currentStatusFilter == "OPD")
                opdPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;

            totalPanel.Invalidate();
            successPanel.Invalidate();
            failedPanel.Invalidate();
            ipdPanel.Invalidate();
            opdPanel.Invalidate();

            totalPanel.Update();
            successPanel.Update();
            failedPanel.Update();
            ipdPanel.Update();
            opdPanel.Update();
        }

        private void ApplyStatusFilter()
        {
            try
            {
                if (_currentStatusFilter == "All")
                    _filteredDataView.RowFilter = string.Empty;
                else
                    _filteredDataView.RowFilter = $"[Status] = '{_currentStatusFilter}'";

                if (string.IsNullOrEmpty(_filteredDataView.Sort))
                    _filteredDataView.Sort = "[Time Check] DESC";

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

        private void ApplyOrderTypeFilter(string orderType)
        {
            try
            {
                _filteredDataView.RowFilter = $"[Service Type] = '{orderType}'";
                if (string.IsNullOrEmpty(_filteredDataView.Sort))
                    _filteredDataView.Sort = "[Time Check] DESC";

                ApplyRowColors();

                int resultCount = _filteredDataView.Count;
                UpdateStatus($"Showing {resultCount} {orderType} record(s)");
                UpdateStatusSummary();
                _logger.LogInfo($"Order type filter applied: {orderType} - Found {resultCount} records");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error applying {orderType} filter", ex);
            }
        }

        private void ApplyFilter()
        {
            try
            {
                string searchText = searchTextBox.Text.Trim();
                DateTime selectedDate = dateTimePicker.Value.Date;
                var filterParts = new System.Collections.Generic.List<string>();

                if (!string.IsNullOrEmpty(searchText))
                    filterParts.Add($"([Order No] LIKE '%{searchText}%' OR [HN] LIKE '%{searchText}%')");

                string datePattern = selectedDate.ToString("yyyy-MM-dd");
                filterParts.Add($"([Time Check] LIKE '{datePattern}%' OR [Transaction DateTime] LIKE '{datePattern}%')");

                if (_currentStatusFilter != "All")
                    filterParts.Add($"[Status] = '{_currentStatusFilter}'");

                string filterExpression = string.Join(" AND ", filterParts);

                _logger.LogInfo($"=== FILTER ===");
                _logger.LogInfo($"Date: {datePattern} | Search: '{searchText}' | Status: {_currentStatusFilter}");
                _logger.LogInfo($"Expression: {filterExpression}");

                _filteredDataView.RowFilter = filterExpression;
                ApplyRowColors();

                int resultCount = _filteredDataView.Count;
                int totalCount = _processedDataTable.Rows.Count;

                string info = $"Date: {datePattern}";
                if (!string.IsNullOrEmpty(searchText)) info += $" | Search: {searchText}";
                if (_currentStatusFilter != "All") info += $" | Status: {_currentStatusFilter}";

                if (resultCount > 0)
                    UpdateStatus($"✓ {resultCount} record(s) - {info} (Total: {totalCount})");
                else
                    UpdateStatus($"✗ No records - {info} (Total: {totalCount})");

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
        private void AddRowToGrid(string time, string transactionDateTime, string serviceType,
            string orderNo, string hn, string patientName, string financialClass,
            string orderControl, string status, string apiResponse, HL7Message hl7Data)
        {
            try
            {
                if (dataGridView.InvokeRequired)
                {
                    dataGridView.Invoke(new Action(() =>
                    {
                        AddRowToGridDirect(time, transactionDateTime, serviceType, orderNo, hn,
                                           patientName, financialClass, orderControl, status,
                                           apiResponse, hl7Data);
                    }));
                    return;
                }

                AddRowToGridDirect(time, transactionDateTime, serviceType, orderNo, hn,
                                   patientName, financialClass, orderControl, status,
                                   apiResponse, hl7Data);
            }
            catch (Exception ex)
            {
                _logger?.LogError("Error in AddRowToGrid", ex);
            }
        }

        private void AddRowToGridDirect(string time, string transactionDateTime, string serviceType,
            string orderNo, string hn, string patientName, string financialClass,
            string orderControl, string status, string apiResponse, HL7Message hl7Data)
        {
            try
            {
                int rowIndex = _processedDataTable.Rows.Count;
                _processedDataTable.Rows.Add(time, transactionDateTime, serviceType, orderNo, hn,
                                             patientName, financialClass, orderControl, status,
                                             apiResponse);

                if (hl7Data != null)
                    _rowHL7Data[rowIndex] = hl7Data;

                UpdateStatusSummary();

                this.BeginInvoke(new Action(() =>
                {
                    ApplyRowColors();
                }));

                int lastRowIndex = dataGridView.Rows.Count - 1;
                if (lastRowIndex >= 0)
                {
                    var lastRow = dataGridView.Rows[lastRowIndex];
                    if (status == "Success")
                        lastRow.DefaultCellStyle.BackColor = System.Drawing.Color.LightGreen;
                    else if (status == "Failed")
                        lastRow.DefaultCellStyle.BackColor = System.Drawing.Color.LightCoral;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError("Error adding row to grid", ex);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopIPDService();
            StopOPDService();
            StopConnectionMonitor();
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
            this.BeginInvoke(new Action(() =>
            {
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
                dataGridView.SuspendLayout();

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

                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    try
                    {
                        if (!row.IsNewRow && statusColumnIndex < row.Cells.Count)
                        {
                            var cell = row.Cells[statusColumnIndex];
                            if (cell.Value != null)
                            {
                                string status = cell.Value.ToString().Trim();
                                row.DefaultCellStyle.BackColor = System.Drawing.Color.White;
                                row.DefaultCellStyle.SelectionBackColor = System.Drawing.SystemColors.Highlight;

                                if (status == "Success")
                                {
                                    row.DefaultCellStyle.BackColor = System.Drawing.Color.LightGreen;
                                    row.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.Green;
                                }
                                else if (status == "Failed")
                                {
                                    row.DefaultCellStyle.BackColor = System.Drawing.Color.LightCoral;
                                    row.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.Red;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError($"[ApplyRowColors] Error on row {row.Index}: {ex.Message}", ex);
                    }
                }

                dataGridView.ResumeLayout();
                dataGridView.Refresh();
            }
            catch (Exception ex)
            {
                _logger?.LogError("Error applying row colors", ex);
            }
        }
        #endregion

        #region Start/Stop Service Manual and Auto - IPD/OPD

        private void StartStopIPDButton_Click(object sender, EventArgs e)
        {
            if (_ipdTimer == null)
            {
                StartIPDService();
                // ⭐ ปิด manual check ขณะ service กำลัง run
                manualCheckButton.Enabled = false;
                exportButton.Enabled = false;
            }
            else
            {
                StopIPDService();
                // ถ้า OPD ก็หยุดแล้ว ให้เปิด manual check
                if (_opdTimer == null)
                {
                    manualCheckButton.Enabled = true;
                    exportButton.Enabled = true;
                }
            }
        }

        private void StartStopOPDButton_Click(object sender, EventArgs e)
        {
            if (_opdTimer == null)
            {
                StartOPDService();
                manualCheckButton.Enabled = false;
                exportButton.Enabled = false;
            }
            else
            {
                StopOPDService();
                if (_ipdTimer == null)
                {
                    manualCheckButton.Enabled = true;
                    exportButton.Enabled = true;
                }
            }
        }

        private void StartIPDService()
        {
            if (!_ipdTableExists)
            {
                _logger.LogWarning("Attempted to start IPD Service but table doesn't exist");
                return;
            }

            var intervalMs = _intervalSeconds * 1000;
            _ipdCancellationTokenSource = new CancellationTokenSource();
            _ipdTimer = new Timer(IPDTimerCallback, null, 0, intervalMs);

            UpdateButtonState(startStopIPDButton, true, "■ Stop IPD");
            UpdateServiceStatus();
            _logger.LogInfo($"IPD Service started with {_intervalSeconds}s interval");
        }

        private void StartOPDService()
        {
            if (!_opdTableExists)
            {
                _logger.LogWarning("Attempted to start OPD Service but table doesn't exist");
                return;
            }

            var intervalMs = _intervalSeconds * 1000;
            _opdCancellationTokenSource = new CancellationTokenSource();
            _opdTimer = new Timer(OPDTimerCallback, null, 0, intervalMs);

            UpdateButtonState(startStopOPDButton, true, "■ Stop OPD");
            UpdateServiceStatus();
            _logger.LogInfo($"OPD Service started with {_intervalSeconds}s interval");
        }

        private async void StopIPDService()
        {
            _logger.LogInfo("Stopping IPD Service");

            if (_ipdTimer != null)
            {
                _ipdTimer.Dispose();
                _ipdTimer = null;
            }

            if (_ipdCancellationTokenSource != null)
            {
                _ipdCancellationTokenSource.Cancel();

                int timeoutMs = 5000;
                int elapsedMs = 0;
                int checkIntervalMs = 100;

                while (_isIPDProcessing && elapsedMs < timeoutMs)
                {
                    await Task.Delay(checkIntervalMs);
                    elapsedMs += checkIntervalMs;
                }

                _ipdCancellationTokenSource.Dispose();
                _ipdCancellationTokenSource = null;
            }

            _isIPDProcessing = false;

            await LoadDataBySelectedDate();

            UpdateButtonState(startStopIPDButton, false, "▶ Start IPD");
            UpdateServiceStatus();
            _logger.LogInfo("IPD Service stopped");
        }

        private async void StopOPDService()
        {
            _logger.LogInfo("Stopping OPD Service");

            if (_opdTimer != null)
            {
                _opdTimer.Dispose();
                _opdTimer = null;
            }

            if (_opdCancellationTokenSource != null)
            {
                _opdCancellationTokenSource.Cancel();

                int timeoutMs = 5000;
                int elapsedMs = 0;
                int checkIntervalMs = 100;

                while (_isOPDProcessing && elapsedMs < timeoutMs)
                {
                    await Task.Delay(checkIntervalMs);
                    elapsedMs += checkIntervalMs;
                }

                _opdCancellationTokenSource.Dispose();
                _opdCancellationTokenSource = null;
            }

            _isOPDProcessing = false;

            await LoadDataBySelectedDate();

            UpdateButtonState(startStopOPDButton, false, "▶ Start OPD");
            UpdateServiceStatus();
            _logger.LogInfo("OPD Service stopped");
        }

        private async void IPDTimerCallback(object state)
        {
            if (_isIPDProcessing || _ipdTimer == null) return;
            if (_ipdCancellationTokenSource == null || _ipdCancellationTokenSource.IsCancellationRequested) return;

            await CheckPendingOrders("IPD", false, _ipdCancellationTokenSource.Token);
        }

        private async void OPDTimerCallback(object state)
        {
            if (_isOPDProcessing || _opdTimer == null) return;
            if (_opdCancellationTokenSource == null || _opdCancellationTokenSource.IsCancellationRequested) return;

            await CheckPendingOrders("OPD", false, _opdCancellationTokenSource.Token);
        }

        private async void ManualCheckButton_Click(object sender, EventArgs e)
        {
            if (!_isIPDProcessing && !_isOPDProcessing)
            {
                var tasks = new List<Task>();

                if (_ipdTableExists)
                    tasks.Add(CheckPendingOrders("IPD", true));
                else
                    _logger.LogWarning("Manual check skipped IPD - table doesn't exist");

                if (_opdTableExists)
                    tasks.Add(CheckPendingOrders("OPD", true));
                else
                    _logger.LogWarning("Manual check skipped OPD - table doesn't exist");

                await Task.WhenAll(tasks);
            }
        }

        private async Task CheckPendingOrders(string orderType, bool isManual,
            CancellationToken cancellationToken = default)
        {
            bool isIPD = orderType == "IPD";

            if (isIPD && _isIPDProcessing) return;
            if (!isIPD && _isOPDProcessing) return;

            if (isIPD) _isIPDProcessing = true;
            else _isOPDProcessing = true;

            try
            {
                if (isIPD && !_ipdTableExists)
                {
                    _logger?.LogWarning($"[{orderType}] Table does not exist - Aborting");
                    UpdateStatus($"✗ {orderType} table not found");
                    return;
                }

                if (!isIPD && !_opdTableExists)
                {
                    _logger?.LogWarning($"[{orderType}] Table does not exist - Aborting");
                    UpdateStatus($"✗ {orderType} table not found");
                    return;
                }

                if (isManual)
                {
                    this.Invoke(new Action(() =>
                    {
                        startStopIPDButton.Enabled = false;
                        startStopOPDButton.Enabled = false;
                        exportButton.Enabled = false;
                    }));
                }

                UpdateLastCheck();
                _logger.LogInfo($"[{orderType}] Starting pending orders check");

                cancellationToken.ThrowIfCancellationRequested();

                _logger?.LogInfo($"[{orderType}] Querying database for pending orders...");

                List<DrugDispenseipd> pending = null;

                try
                {
                    pending = await Task.Run(() =>
                        _databaseService.GetPendingDispenseDataByOrderType(orderType),
                        cancellationToken);
                }
                catch (Exception dbEx)
                {
                    _logger?.LogError($"[{orderType}] Database query failed", dbEx);
                    UpdateStatus($"✗ {orderType} - Database Error: {dbEx.Message}");
                    return;
                }

                if (pending == null)
                {
                    _logger?.LogWarning($"[{orderType}] Database returned null");
                    pending = new List<DrugDispenseipd>();
                }

                _logger.LogInfo($"[{orderType}] Retrieved {pending.Count} records");

                if (pending.Count > 0)
                    UpdateLastFound(pending.Count);
                else
                {
                    _logger?.LogInfo($"[{orderType}] No pending orders found");
                    UpdateStatus($"✓ {orderType} - No pending orders");
                }

                cancellationToken.ThrowIfCancellationRequested();

                int remainingCount = pending.Count;

                this.Invoke(new Action(() =>
                {
                    this.Text = $"ConHIS Service - {orderType} Pending: {remainingCount}";
                    if (remainingCount > 0)
                        UpdateStatus($"[{orderType}] Processing {remainingCount} pending orders...");
                }));

                if (pending.Count > 0)
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            if (orderType == "IPD")
                            {
                                _processor.ProcessPendingOrders(
                                    msg => { _logger.LogInfo($"[{orderType}] {msg}"); },
                                    result =>
                                    {
                                        if (cancellationToken.IsCancellationRequested)
                                        {
                                            _logger.LogInfo($"[{orderType}] Processing cancelled");
                                            return;
                                        }
                                        ProcessOrderResult(result, ref remainingCount, orderType, cancellationToken);
                                    },
                                    cancellationToken
                                );
                            }
                            else
                            {
                                _processor.ProcessPendingOpdOrders(
                                    msg => { _logger.LogInfo($"[{orderType}] {msg}"); },
                                    result =>
                                    {
                                        if (cancellationToken.IsCancellationRequested)
                                        {
                                            _logger.LogInfo($"[{orderType}] Processing cancelled");
                                            return;
                                        }
                                        ProcessOrderResult(result, ref remainingCount, orderType, cancellationToken);
                                    },
                                    cancellationToken
                                );
                            }
                        }
                        catch (Exception processEx)
                        {
                            _logger?.LogError($"[{orderType}] Processing failed", processEx);
                            throw;
                        }
                    }, cancellationToken);

                    _logger.LogInfo($"[{orderType}] Completed processing");

                    this.Invoke(new Action(() =>
                    {
                        this.Text = "ConHIS Service - Drug Dispense Monitor";
                        UpdateServiceStatus(orderType, pending.Count);
                    }));
                }
                else
                {
                    this.Invoke(new Action(() =>
                    {
                        this.Text = "ConHIS Service - Drug Dispense Monitor";
                        UpdateServiceStatus(orderType, 0);
                    }));
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInfo($"[{orderType}] Operation cancelled");
                this.Invoke(new Action(() =>
                {
                    this.Text = "ConHIS Service - Drug Dispense Monitor";
                    UpdateStatus($"[{orderType}] Service stopped - Operation cancelled");
                }));
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[{orderType}] Critical error in CheckPendingOrders", ex);
                _logger?.LogError($"[{orderType}] StackTrace: {ex.StackTrace}", ex);

                this.Invoke(new Action(() =>
                {
                    this.Text = "ConHIS Service - Drug Dispense Monitor";
                    UpdateStatus($"✗ {orderType} Error: {ex.Message}");

                    MessageBox.Show(
                        $"เกิดข้อผิดพลาดร้ายแรงใน {orderType} Service:\n\n" +
                        $"{ex.Message}\n\n" +
                        $"กรุณาตรวจสอบ Log files สำหรับรายละเอียด",
                        $"{orderType} Service Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }));
            }
            finally
            {
                if (isIPD) _isIPDProcessing = false;
                else _isOPDProcessing = false;

                if (isManual)
                {
                    this.Invoke(new Action(() =>
                    {
                        startStopIPDButton.Enabled = _ipdTableExists;
                        startStopOPDButton.Enabled = _opdTableExists;
                        exportButton.Enabled = true;
                    }));
                }
            }
        }

        private void ProcessOrderResult(Services.ProcessResult result, ref int remainingCount,
            string orderType, CancellationToken cancellationToken)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger?.LogInfo($"[{orderType}] Processing cancelled for order");
                    return;
                }

                var hl7Message = result.ParsedMessage;
                string orderNo = hl7Message?.CommonOrder?.PlacerOrderNumber ?? "N/A";

                int currentRemaining = Interlocked.Decrement(ref remainingCount);

                if (currentRemaining < 0)
                {
                    _logger?.LogWarning($"[{orderType}] Remaining count went negative, resetting to 0");
                    Interlocked.Exchange(ref remainingCount, 0);
                    currentRemaining = 0;
                }

                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            if (!this.IsDisposed)
                            {
                                this.Text = $"ConHIS Service - {orderType} Pending: {currentRemaining}";

                                if (currentRemaining > 0)
                                    UpdateStatus($"[{orderType}] Processing... {currentRemaining} remaining");
                                else
                                    UpdateStatus($"[{orderType}] Completed processing all orders");
                            }
                        }
                        catch (ObjectDisposedException) { }
                        catch (Exception ex)
                        {
                            _logger?.LogError($"[{orderType}] Error updating UI", ex);
                        }
                    }));
                }

                if (result.Success)
                {
                    UpdateLastSuccess(orderNo);
                    _logger?.LogInfo($"[{orderType}] Order {orderNo} processed successfully");
                }
                else
                {
                    _logger?.LogWarning($"[{orderType}] Order {orderNo} failed: {result.Message}");
                }

                string hn = hl7Message?.PatientIdentification?.PatientIDExternal ??
                           hl7Message?.PatientIdentification?.PatientIDInternal ?? "N/A";

                string transactionDateTime = "N/A";
                if (result.DrugDispenseDatetime.HasValue)
                {
                    try
                    {
                        transactionDateTime = result.DrugDispenseDatetime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning($"[{orderType}] Error formatting drug dispense datetime: {ex.Message}");
                    }
                }

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

                string serviceType = orderType;
                string orderControl = hl7Message?.CommonOrder?.OrderControl ?? "N/A";

                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    try
                    {
                        AddRowToGrid(
                            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            transactionDateTime,
                            serviceType,
                            orderNo,
                            hn,
                            patientName,
                            financialClass,
                            orderControl,
                            result.Success ? "Success" : "Failed",
                            result.ApiResponse ?? result.Message ?? "N/A",
                            hl7Message
                        );

                        _logger?.LogInfo($"[{orderType}] Order {orderNo} added to grid - " +
                                        $"ServiceType: {serviceType}, OrderControl: {orderControl}, " +
                                        $"Status: {(result.Success ? "Success" : "Failed")}");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError($"[{orderType}] Error adding order {orderNo} to grid", ex);
                    }
                }
                else
                {
                    _logger?.LogWarning($"[{orderType}] Form disposed, skipping grid update for order {orderNo}");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[{orderType}] Critical error in ProcessOrderResult", ex);

                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        UpdateStatus($"[{orderType}] Error processing order: {ex.Message}");
                    }));
                }
            }
        }

        private void UpdateButtonState(Button button, bool isRunning, string text)
        {
            if (button.InvokeRequired)
            {
                button.Invoke(new Action(() => UpdateButtonState(button, isRunning, text)));
                return;
            }

            button.Text = text;
            if (isRunning)
            {
                button.BackColor = System.Drawing.Color.FromArgb(231, 76, 60); // Red for Stop
            }
            else
            {
                if (button == startStopIPDButton)
                    button.BackColor = System.Drawing.Color.FromArgb(52, 152, 219); // Blue for IPD
                else
                    button.BackColor = System.Drawing.Color.FromArgb(46, 204, 113); // Green for OPD
            }
        }

        private void UpdateServiceStatus(string orderType = null, int processedCount = 0)
        {
            bool ipdRunning = _ipdTimer != null;
            bool opdRunning = _opdTimer != null;

            string status = "";

            if (ipdRunning && opdRunning)
                status = "Both IPD & OPD Services Running";
            else if (ipdRunning)
                status = "IPD Service Running";
            else if (opdRunning)
                status = "OPD Service Running";
            else
                status = "All Services Stopped";

            if (orderType != null && processedCount > 0)
                status += $" - [{orderType}] Processed {processedCount} orders";
            else if (orderType != null && processedCount == 0)
                status += $" - [{orderType}] No pending orders";

            UpdateStatus(status);
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
                _lastSuccessOrderId = orderId;

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

        private void UpdateConnectionStatus(bool anyConnected,
            bool opdConnected = true, bool ipdConnected = true)
        {
            _isDatabaseConnected = anyConnected;

            if (connectionStatusLabel.InvokeRequired)
            {
                connectionStatusLabel.Invoke(
                    new Action(() => UpdateConnectionStatus(anyConnected, opdConnected, ipdConnected)));
                return;
            }

            if (anyConnected)
            {
                _lastDatabaseConnectionTime = DateTime.Now;
                string timeStr = _lastDatabaseConnectionTime.Value.ToString("yyyy-MM-dd HH:mm:ss");

                string opdStatus = opdConnected ? "✓ OPD" : "✗ OPD";
                string ipdStatus = ipdConnected ? "✓ IPD" : "✗ IPD";

                connectionStatusLabel.Text =
                    $"Database: {opdStatus} | {ipdStatus} (Last Connected: {timeStr})";
                connectionStatusLabel.ForeColor = (opdConnected && ipdConnected)
                    ? System.Drawing.Color.Green
                    : System.Drawing.Color.Orange;
            }
            else
            {
                _lastDatabaseDisconnectionTime = DateTime.Now;
                string lastConnectedStr = _lastDatabaseConnectionTime.HasValue
                    ? $"Last Connected: {_lastDatabaseConnectionTime.Value:yyyy-MM-dd HH:mm:ss}"
                    : "Never Connected";

                connectionStatusLabel.Text =
                    $"Database: ✗ OPD | ✗ IPD Disconnected | {lastConnectedStr}";
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
                int ipdCount = 0;
                int opdCount = 0;

                foreach (DataRow row in _processedDataTable.Rows)
                {
                    string status = row["Status"]?.ToString() ?? "";
                    string serviceType = row["Service Type"]?.ToString() ?? "";

                    if (status == "Success") successCount++;
                    else if (status == "Failed") failedCount++;

                    if (serviceType == "IPD") ipdCount++;
                    else if (serviceType == "OPD") opdCount++;
                }

                totalCountLabel.Text = totalRecords.ToString();
                successCountLabel.Text = successCount.ToString();
                failedCountLabel.Text = failedCount.ToString();
                ipdCountLabel.Text = ipdCount.ToString();
                opdCountLabel.Text = opdCount.ToString();

                UpdatePanelStyles(totalPanel, totalRecords, System.Drawing.Color.FromArgb(240, 240, 240));
                UpdatePanelStyles(successPanel, successCount, System.Drawing.Color.FromArgb(220, 255, 220));
                UpdatePanelStyles(failedPanel, failedCount, System.Drawing.Color.FromArgb(255, 220, 220));
                UpdatePanelStyles(ipdPanel, ipdCount, System.Drawing.Color.FromArgb(220, 235, 255));
                UpdatePanelStyles(opdPanel, opdCount, System.Drawing.Color.FromArgb(220, 255, 235));
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

        private void DrawPanelTopBar(Panel panel, PaintEventArgs e, System.Drawing.Color barColor, string filterType)
        {
            if (_currentStatusFilter == filterType)
            {
                using (var brush = new System.Drawing.SolidBrush(barColor))
                {
                    e.Graphics.FillRectangle(brush, 0, 0, e.ClipRectangle.Width, 3);
                }
            }
            else
            {
                using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(220, 220, 220)))
                {
                    e.Graphics.FillRectangle(brush, 0, 0, e.ClipRectangle.Width, 3);
                }
            }
        }

        private void UpdateServiceButtonStates()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(UpdateServiceButtonStates));
                return;
            }

            startStopIPDButton.Enabled = _ipdTableExists;
            startStopOPDButton.Enabled = _opdTableExists;

            if (!_ipdTableExists)
            {
                startStopIPDButton.Text = "⚠️ IPD (No Table)";
                startStopIPDButton.BackColor = System.Drawing.Color.Gray;
            }
            else
            {
                // ⭐ อย่า reset text ถ้า service กำลัง run อยู่
                if (_ipdTimer == null)
                {
                    startStopIPDButton.Text = "▶ Start IPD";
                    startStopIPDButton.BackColor = System.Drawing.Color.FromArgb(52, 152, 219);
                }
            }

            if (!_opdTableExists)
            {
                startStopOPDButton.Text = "⚠️ OPD (No Table)";
                startStopOPDButton.BackColor = System.Drawing.Color.Gray;
            }
            else
            {
                if (_opdTimer == null)
                {
                    startStopOPDButton.Text = "▶ Start OPD";
                    startStopOPDButton.BackColor = System.Drawing.Color.FromArgb(46, 204, 113);
                }
            }
        }
        #endregion

        #region Settings
        private void SettingsButton_Click(object sender, EventArgs e)
        {
            try
            {
                using (var settingsForm = new PagesFrom.SettingsForm())
                {
                    if (settingsForm.ShowDialog() == DialogResult.OK)
                    {
                        if (settingsForm.SettingsChanged)
                        {
                            try
                            {
                                _logger?.LogInfo("Reloading configuration after settings changed");

                                _appConfig?.ReloadConfiguration();
                                _logger?.ReloadLogRetentionDays();
                                _logger?.CleanOldLogs();

                                UpdateStatus("✓ Settings updated successfully");

                                MessageBox.Show(
                                    "การตั้งค่าถูกบันทึกเรียบร้อยแล้ว\n\n" +
                                    "ไฟล์ที่อัพเดท:\n" +
                                    "• Database Connection\n" +
                                    "• API Settings\n" +
                                    "• Log Retention\n\n" +
                                    "หมายเหตุ: การตั้งค่าบางอย่างอาจต้อง Restart โปรแกรม",
                                    "Settings Updated",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information
                                );

                                _logger?.LogInfo("Configuration reloaded successfully");
                            }
                            catch (Exception reloadEx)
                            {
                                _logger?.LogError("Error reloading configuration", reloadEx);

                                MessageBox.Show(
                                    $"บันทึกการตั้งค่าสำเร็จ\n\n" +
                                    $"แต่เกิดข้อผิดพลาดในการโหลดใหม่:\n{reloadEx.Message}\n\n" +
                                    "กรุณา Restart โปรแกรมเพื่อให้การตั้งค่ามีผล",
                                    "Warning",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning
                                );
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError("Error opening settings", ex);
                MessageBox.Show(
                    $"เกิดข้อผิดพลาดในการเปิด Settings:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
        #endregion
    }
}