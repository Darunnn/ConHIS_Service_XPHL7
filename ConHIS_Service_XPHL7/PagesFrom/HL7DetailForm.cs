using ConHIS_Service_XPHL7.Models;
using ConHIS_Service_XPHL7.Utils;
using Mysqlx.Crud;
using MySqlX.XDevAPI.Relational;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ConHIS_Service_XPHL7
{
    public partial class HL7DetailForm : Form
    {
        private HL7Message _hl7Message;
        private Utils.LogManager _logManager;
        private string _orderNo;
        private string _status;
        private DateTime? _filterDate; // เพิ่มตัวแปรสำหรับกรองวันที่

        // Constructor เดิม (สำหรับ backward compatibility)
        public HL7DetailForm(HL7Message hl7Message, string orderNo, string status = "N/A")
            : this(hl7Message, null, orderNo, status)
        {
        }

        // Constructor ใหม่ที่รับ time parameter
        public HL7DetailForm(HL7Message hl7Message, DateTime? filterDate, string orderNo, string status = "N/A")
        {
            _hl7Message = hl7Message;
            _orderNo = orderNo;
            _status = status;
            _filterDate = filterDate; // เก็บวันที่สำหรับกรอง
            _logManager = new Utils.LogManager();
            InitializeComponent();

            string dateInfo = _filterDate.HasValue ? $" ({_filterDate.Value:yyyy-MM-dd})" : "";
            this.Text = $"HL7 Message Details - Order: {orderNo} (Status: {status}){dateInfo}";
            lblOrderNo.Text = $"Order No: {orderNo} | Status: {status}{dateInfo}";

            LoadData();
        }

        private void LoadData()
        {
            // MSH Tab
            var mshGrid = CreateDataGridView();
            tabMSH.Controls.Add(mshGrid);
            LoadObject(mshGrid, _hl7Message?.MessageHeader);

            // PID Tab
            var pidGrid = CreateDataGridView();
            tabPID.Controls.Add(pidGrid);
            LoadObject(pidGrid, _hl7Message?.PatientIdentification);

            // PV1 Tab
            var pv1Grid = CreateDataGridView();
            tabPV1.Controls.Add(pv1Grid);
            LoadObject(pv1Grid, _hl7Message?.PatientVisit);

            // ORC Tab
            var orcGrid = CreateDataGridView();
            tabORC.Controls.Add(orcGrid);
            LoadObject(orcGrid, _hl7Message?.CommonOrder);

            // AL1 Tab
            var al1Grid = CreateDataGridView();
            tabAL1.Controls.Add(al1Grid);
            LoadCollection(al1Grid, _hl7Message?.Allergies);

            // RXD Tab
            var rxdGrid = CreateDataGridView();
            tabRXD.Controls.Add(rxdGrid);
            LoadCollection(rxdGrid, _hl7Message?.PharmacyDispense);

            // RXR Tab
            var rxrGrid = CreateDataGridView();
            tabRXR.Controls.Add(rxrGrid);
            LoadCollection(rxrGrid, _hl7Message?.RouteInfo);

            // NTE Tab
            var nteGrid = CreateDataGridView();
            tabNTE.Controls.Add(nteGrid);
            LoadCollection(nteGrid, _hl7Message?.Notes);

            // Load Logs initially
            LoadOrderLogs();
        }

        #region Log Management

        private void BtnRefreshLogs_Click(object sender, EventArgs e)
        {
            LoadOrderLogs();
        }

        private void BtnExportLogs_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(logTextBox.Text))
                {
                    MessageBox.Show("No logs to export.", "Export Logs",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // ให้เลือกว่าจะ export แบบสรุป หรือ แบบเต็ม
                var result = MessageBox.Show(
                    "Do you want to export the full detailed logs?\r\n\r\n" +
                    "Yes = Export full raw logs\r\n" +
                    "No = Export summary (current view)",
                    "Export Options",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Cancel)
                    return;

                using (var saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "Text files (*.txt)|*.txt|Log files (*.log)|*.log|All files (*.*)|*.*";
                    string dateStr = _filterDate.HasValue ? $"_{_filterDate.Value:yyyyMMdd}" : "";
                    saveFileDialog.FileName = $"OrderLog_{_orderNo}_{_status}{dateStr}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                    saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string contentToExport;

                        if (result == DialogResult.Yes)
                        {
                            // Export full raw logs
                            var logs = GetLogsForOrder(_orderNo, null, null);
                            var sb = new StringBuilder();
                            string dateInfo = _filterDate.HasValue ? $" on {_filterDate.Value:yyyy-MM-dd}" : "";
                            sb.AppendLine($"=== Full Raw Logs for Order No: {_orderNo} (Status: {_status}){dateInfo} ===");
                            sb.AppendLine($"Exported at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                            sb.AppendLine(new string('=', 80));
                            sb.AppendLine();

                            foreach (var log in logs)
                            {
                                sb.AppendLine(log.RawLog);
                                sb.AppendLine();
                            }

                            contentToExport = sb.ToString();
                        }
                        else
                        {
                            // Export summary (current view)
                            contentToExport = logTextBox.Text;
                        }

                        File.WriteAllText(saveFileDialog.FileName, contentToExport, Encoding.UTF8);
                        MessageBox.Show($"Logs exported successfully to:\n{saveFileDialog.FileName}",
                            "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        string dateLog = _filterDate.HasValue ? $" on {_filterDate.Value:yyyy-MM-dd}" : "";
                        _logManager.LogInfo($"Logs exported for Order No: {_orderNo} (Status: {_status}){dateLog} to {saveFileDialog.FileName}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logManager.LogError("Error exporting logs", ex);
                MessageBox.Show($"Export failed: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadOrderLogs()
        {
            try
            {
                logTextBox.Text = "Loading logs...\r\n";
                logTextBox.Refresh();

                var debugInfo = new StringBuilder();
                debugInfo.AppendLine($"Search Parameters:");
                debugInfo.AppendLine($"   Order No: '{_orderNo}' (Length: {_orderNo?.Length})");
                debugInfo.AppendLine($"   Status: {_status}");
                if (_filterDate.HasValue)
                {
                    debugInfo.AppendLine($"   Filter Date: {_filterDate.Value:yyyy-MM-dd}");
                }

                // Try multiple possible locations for log directories
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

                // Go up from bin\Debug to project root
                string projectRoot = Directory.GetParent(baseDirectory)?.Parent?.Parent?.FullName ?? baseDirectory;

                List<string> possibleLogPaths = new List<string>
        {
            Path.Combine(baseDirectory, "hl7_raw"),
            Path.Combine(projectRoot, "hl7_raw"),
            Path.Combine(Directory.GetParent(baseDirectory).FullName, "hl7_raw"),
        };

                List<string> possibleErrorLogPaths = new List<string>
        {
            Path.Combine(baseDirectory, "logreaderror"),
            Path.Combine(projectRoot, "logreaderror"),
            Path.Combine(Directory.GetParent(baseDirectory).FullName, "logreaderror"),
        };

                // กำหนด directory ที่จะใช้ตาม status
                string logDirectory = null;
                string errorLogDirectory = null;

                if (_status == "Failed")
                {
                    // ถ้า status เป็น Failed ให้ค้นหาใน logreaderror เท่านั้น
                    errorLogDirectory = possibleErrorLogPaths.FirstOrDefault(p => Directory.Exists(p));
                    debugInfo.AppendLine($"   Log Type: Error Logs (logreaderror)");
                }
                else if (_status == "Success")
                {
                    // ถ้า status เป็น Success ให้ค้นหาใน hl7_raw เท่านั้น
                    logDirectory = possibleLogPaths.FirstOrDefault(p => Directory.Exists(p));
                    debugInfo.AppendLine($"   Log Type: Raw Logs (hl7_raw)");
                }
                else
                {
                    // ถ้าไม่ทราบ status ให้ค้นหาทั้งสองที่
                    logDirectory = possibleLogPaths.FirstOrDefault(p => Directory.Exists(p));
                    errorLogDirectory = possibleErrorLogPaths.FirstOrDefault(p => Directory.Exists(p));
                    debugInfo.AppendLine($"   Log Type: All Logs (Both directories)");
                }

                debugInfo.AppendLine($"   Base Directory: {baseDirectory}");
                debugInfo.AppendLine($"   Project Root: {projectRoot}");
                debugInfo.AppendLine();
                debugInfo.AppendLine($"   HL7 Raw Directory: {logDirectory ?? "(not searched)"}");
                debugInfo.AppendLine($"   Error Log Directory: {errorLogDirectory ?? "(not searched)"}");
                debugInfo.AppendLine();

                // Check directories
                bool logsExists = !string.IsNullOrEmpty(logDirectory) && Directory.Exists(logDirectory);
                bool errorLogsExists = !string.IsNullOrEmpty(errorLogDirectory) && Directory.Exists(errorLogDirectory);

                debugInfo.AppendLine("Directory Status:");
                if (_status == "Success" || string.IsNullOrEmpty(_status) || (_status != "Failed"))
                    debugInfo.AppendLine($"   hl7_raw: {(logsExists ? "✓ Found" : "✗ Not found")}");
                if (_status == "Failed" || string.IsNullOrEmpty(_status) || (_status != "Success"))
                    debugInfo.AppendLine($"   logreaderror: {(errorLogsExists ? "✓ Found" : "✗ Not found")}");

                if (logsExists || errorLogsExists)
                {
                    debugInfo.AppendLine();
                    debugInfo.AppendLine("Searching in:");
                    if (logsExists)
                        debugInfo.AppendLine($"   → {logDirectory}");
                    if (errorLogsExists)
                        debugInfo.AppendLine($"   → {errorLogDirectory}");
                }

                debugInfo.AppendLine();

                if (!logsExists && !errorLogsExists)
                {
                    debugInfo.AppendLine("Error: Log directories not found!");
                    debugInfo.AppendLine();
                    debugInfo.AppendLine("Searched locations:");
                    if (_status == "Success" || string.IsNullOrEmpty(_status) || (_status != "Failed"))
                    {
                        debugInfo.AppendLine("  HL7 Raw logs:");
                        foreach (var path in possibleLogPaths)
                            debugInfo.AppendLine($"    - {path}");
                    }
                    if (_status == "Failed" || string.IsNullOrEmpty(_status) || (_status != "Success"))
                    {
                        debugInfo.AppendLine("  Error logs:");
                        foreach (var path in possibleErrorLogPaths)
                            debugInfo.AppendLine($"    - {path}");
                    }

                    logTextBox.Text = debugInfo.ToString();
                    return;
                }

                // Count log files
                int totalLogFiles = 0;
                if (logsExists)
                {
                    var rawFiles = Directory.GetFiles(logDirectory, "hl7_data_raw_*.txt");
                    totalLogFiles += rawFiles.Length;

                    // Debug: แสดงไฟล์ที่พบ
                    debugInfo.AppendLine($"Raw log files found: {rawFiles.Length}");
                    foreach (var file in rawFiles.Take(5))
                    {
                        debugInfo.AppendLine($"  - {Path.GetFileName(file)} (Size: {new FileInfo(file).Length} bytes, Modified: {File.GetLastWriteTime(file):yyyy-MM-dd HH:mm:ss})");
                    }
                    if (rawFiles.Length > 5)
                    {
                        debugInfo.AppendLine($"  ... and {rawFiles.Length - 5} more files");
                    }
                    debugInfo.AppendLine();
                }
                if (errorLogsExists)
                {
                    // ถ้ามีการกรองวันที่ ให้นับเฉพาะไฟล์ที่ตรงกับวันที่
                    if (_filterDate.HasValue)
                    {
                        string datePattern = $"hl7_error_{_filterDate.Value:yyyy-MM-dd}*.txt";
                        totalLogFiles += Directory.GetFiles(errorLogDirectory, datePattern).Length;
                    }
                    else
                    {
                        totalLogFiles += Directory.GetFiles(errorLogDirectory, "hl7_error_*.txt").Length;
                    }
                }

                debugInfo.AppendLine($"Total log files to search: {totalLogFiles}");
                debugInfo.AppendLine(new string('=', 80));
                debugInfo.AppendLine();

                logTextBox.Text = debugInfo.ToString();
                logTextBox.Refresh();

                var logs = GetLogsForOrder(_orderNo, logDirectory, errorLogDirectory);

                if (logs.Count == 0)
                {
                    var sb = new StringBuilder();
                    sb.Append(debugInfo.ToString());
                    string dateInfo = _filterDate.HasValue ? $" on {_filterDate.Value:yyyy-MM-dd}" : "";
                    sb.AppendLine($"No logs found for Order No: {_orderNo}{dateInfo}");
                    sb.AppendLine($"Search completed at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine();
                    sb.AppendLine("Troubleshooting:");
                    sb.AppendLine("• Check if log files exist in the logs folder");
                    sb.AppendLine("• Verify the Order No is correct (check for spaces)");
                    if (_filterDate.HasValue)
                        sb.AppendLine($"• Check if logs exist for date: {_filterDate.Value:yyyy-MM-dd}");
                    sb.AppendLine("• Order No might be in a different format in the log");
                    sb.AppendLine($"• Searched Order No: '{_orderNo}' (trimmed)");

                    logTextBox.Text = sb.ToString();
                }
                else
                {
                    var sb = new StringBuilder();
                    string dateInfo = _filterDate.HasValue ? $" on {_filterDate.Value:yyyy-MM-dd}" : "";
                    sb.AppendLine($"=== Logs for Order No: {_orderNo} (Status: {_status}){dateInfo} ===");
                    sb.AppendLine($"Total Entries: {logs.Count} | Retrieved: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine(new string('=', 80));
                    sb.AppendLine();

                    // แสดง log แบบ raw format
                    foreach (var log in logs)
                    {
                        sb.AppendLine(log.RawLog);
                        sb.AppendLine(); // เว้นบรรทัดระหว่าง log entries
                    }

                    // สรุปสถิติ
                    var errorCount = logs.Count(l => l.LogLevel == "ERROR");
                    var infoCount = logs.Count(l => l.LogLevel == "INFO");
                    var warningCount = logs.Count(l => l.LogLevel == "WARNING");

                    sb.AppendLine();
                    sb.AppendLine(new string('=', 80));
                    sb.AppendLine("Statistics:");
                    sb.AppendLine($"Total: {logs.Count} | Info: {infoCount} | Warnings: {warningCount} | Errors: {errorCount}");
                    sb.AppendLine(new string('=', 80));

                    logTextBox.Text = sb.ToString();
                }

                // Scroll to top
                logTextBox.SelectionStart = 0;
                logTextBox.SelectionLength = 0;
                logTextBox.ScrollToCaret();

                string dateLog = _filterDate.HasValue ? $" on {_filterDate.Value:yyyy-MM-dd}" : "";
                _logManager.LogInfo($"Searched for Order No: {_orderNo} (Status: {_status}){dateLog}, found {logs.Count} log entries");
            }
            catch (Exception ex)
            {
                _logManager.LogError("Error loading order logs", ex);
                logTextBox.Text = $"Error loading logs: {ex.Message}\r\n";
                logTextBox.AppendText($"\r\nStack Trace:\r\n{ex.StackTrace}");
            }
        }

        private List<LogEntry> GetLogsForOrder(string orderNo, string logDirectory = null, string errorLogDirectory = null)
        {
            var matchingLogs = new List<LogEntry>();

            try
            {
                // If directories not provided, try to find them based on status
                if (string.IsNullOrEmpty(logDirectory) && string.IsNullOrEmpty(errorLogDirectory))
                {
                    string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    string projectRoot = Directory.GetParent(baseDirectory)?.Parent?.Parent?.FullName ?? baseDirectory;

                    List<string> possibleLogPaths = new List<string>
            {
                Path.Combine(baseDirectory, "hl7_raw"),
                Path.Combine(projectRoot, "hl7_raw"),
                Path.Combine(Directory.GetParent(baseDirectory).FullName, "hl7_raw"),
            };

                    List<string> possibleErrorLogPaths = new List<string>
            {
                Path.Combine(baseDirectory, "logreaderror"),
                Path.Combine(projectRoot, "logreaderror"),
                Path.Combine(Directory.GetParent(baseDirectory).FullName, "logreaderror"),
            };

                    // กำหนด directory ตาม status
                    if (_status == "Failed")
                    {
                        errorLogDirectory = possibleErrorLogPaths.FirstOrDefault(p => Directory.Exists(p));
                    }
                    else if (_status == "Success")
                    {
                        logDirectory = possibleLogPaths.FirstOrDefault(p => Directory.Exists(p));
                    }
                    else
                    {
                        // Status อื่นๆ ให้ค้นหาทั้งสองที่
                        logDirectory = possibleLogPaths.FirstOrDefault(p => Directory.Exists(p));
                        errorLogDirectory = possibleErrorLogPaths.FirstOrDefault(p => Directory.Exists(p));
                    }
                }

                var allLogFiles = new List<string>();

                // Get hl7_raw log files (hl7_data_raw_*.txt)
                if (!string.IsNullOrEmpty(logDirectory) && Directory.Exists(logDirectory) &&
                    (_status == "Success" || string.IsNullOrEmpty(_status) || (_status != "Failed")))
                {
                    var normalLogs = Directory.GetFiles(logDirectory, "hl7_data_raw_*.txt")
                        .OrderByDescending(f => File.GetLastWriteTime(f))
                        .ToList();

                    // กรองตามวันที่ถ้ามีการระบุ
                    if (_filterDate.HasValue)
                    {
                        normalLogs = normalLogs
                            .Where(f => File.GetLastWriteTime(f).Date == _filterDate.Value.Date)
                            .ToList();
                    }

                    allLogFiles.AddRange(normalLogs);
                    _logManager.LogInfo($"Found {normalLogs.Count} hl7_raw log files in: {logDirectory}");
                }

                // Get error log files (hl7_error_*.txt)
                if (!string.IsNullOrEmpty(errorLogDirectory) && Directory.Exists(errorLogDirectory) &&
                    (_status == "Failed" || string.IsNullOrEmpty(_status) || (_status != "Success")))
                {
                    // ถ้ามีการกรองวันที่ ให้ค้นหาเฉพาะไฟล์ที่ตรงกับวันที่นั้น
                    string searchPattern = _filterDate.HasValue
                        ? $"hl7_error_{_filterDate.Value:yyyy-MM-dd}*.txt"
                        : "hl7_error_*.txt";

                    var allErrorLogs = Directory.GetFiles(errorLogDirectory, searchPattern)
                        .OrderByDescending(f => File.GetLastWriteTime(f))
                        .ToList();

                    var errorLogsWithDates = new Dictionary<string, DateTime>();

                    foreach (var errorLog in allErrorLogs)
                    {
                        var dateMatch = Regex.Match(Path.GetFileName(errorLog), @"hl7_error_(\d{4}-\d{2}-\d{2})");
                        if (dateMatch.Success)
                        {
                            if (DateTime.TryParse(dateMatch.Groups[1].Value, out DateTime fileDate))
                            {
                                // ถ้ามีการกรองวันที่ ให้เช็คว่าตรงกับวันที่ที่ระบุหรือไม่
                                if (!_filterDate.HasValue || fileDate.Date == _filterDate.Value.Date)
                                {
                                    errorLogsWithDates[errorLog] = fileDate;
                                }
                            }
                        }
                    }

                    var errorLogs = errorLogsWithDates
                        .OrderByDescending(kvp => kvp.Value)
                        .Take(30)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    allLogFiles.AddRange(errorLogs);
                    _logManager.LogInfo($"Found {errorLogs.Count} error log files in: {errorLogDirectory}");
                }

                if (allLogFiles.Count == 0)
                {
                    _logManager.LogWarning("No log files found in any directory");
                    return matchingLogs;
                }

                string dateLog = _filterDate.HasValue ? $" on {_filterDate.Value:yyyy-MM-dd}" : "";
                _logManager.LogInfo($"Searching total {allLogFiles.Count} log files for Order No: {orderNo}{dateLog}");

                string searchOrderNo = orderNo?.Trim() ?? "";
                int totalEntriesChecked = 0;
                int filesChecked = 0;

                foreach (var logFile in allLogFiles)
                {
                    try
                    {
                        filesChecked++;

                        bool isErrorLog = Path.GetFileName(logFile).StartsWith("hl7_error_");
                        bool isRawLog = Path.GetFileName(logFile).StartsWith("hl7_data_raw_");
                        DateTime? errorLogFileDate = null;

                        if (isErrorLog)
                        {
                            var dateMatch = Regex.Match(Path.GetFileName(logFile), @"hl7_error_(\d{4}-\d{2}-\d{2})");
                            if (dateMatch.Success)
                            {
                                if (DateTime.TryParse(dateMatch.Groups[1].Value, out DateTime fileDate))
                                {
                                    errorLogFileDate = fileDate;

                                    // ถ้ามีการกรองวันที่และไฟล์ไม่ตรงกับวันที่ ให้ข้าม
                                    if (_filterDate.HasValue && fileDate.Date != _filterDate.Value.Date)
                                    {
                                        continue;
                                    }
                                }
                            }
                        }

                        string fullLogContent = File.ReadAllText(logFile, Encoding.UTF8);

                        if (string.IsNullOrEmpty(fullLogContent))
                        {
                            _logManager.LogWarning($"File {Path.GetFileName(logFile)} is empty");
                            continue;
                        }

                        // สำหรับไฟล์ hl7_data_raw_*.txt ให้เช็คว่าชื่อไฟล์ตรงกับ Order No หรือไม่
                        if (isRawLog)
                        {
                            // เช็ควันที่ของไฟล์ถ้ามีการกรอง
                            if (_filterDate.HasValue)
                            {
                                DateTime fileDate = File.GetLastWriteTime(logFile).Date;
                                if (fileDate != _filterDate.Value.Date)
                                {
                                    _logManager.LogInfo($"Skipping file {Path.GetFileName(logFile)} - date mismatch (File: {fileDate:yyyy-MM-dd}, Filter: {_filterDate.Value:yyyy-MM-dd})");
                                    continue;
                                }
                            }

                            // ดึง Order No จากชื่อไฟล์: hl7_data_raw_681001000302_NW.txt -> 681001000302
                            // Pattern รองรับทั้ง: 
                            // - hl7_data_raw_681001000302.txt
                            // - hl7_data_raw_681001000302_NW.txt
                            // - hl7_data_raw_681001000302_XX_YY.txt
                            var fileNameMatch = Regex.Match(Path.GetFileName(logFile), @"hl7_data_raw_(.+?)(?:_[A-Z]{2})?\.txt");
                            if (fileNameMatch.Success)
                            {
                                string fileOrderInfo = fileNameMatch.Groups[1].Value; // เช่น "681001000302"

                                // Debug log
                                _logManager.LogInfo($"Comparing: File order '{fileOrderInfo}' (length: {fileOrderInfo.Length}) with search order '{searchOrderNo}' (length: {searchOrderNo.Length})");

                                // ตรวจสอบว่า Order No ตรงกันหรือไม่ (รองรับทั้ง exact match และ contains)
                                bool isMatch = fileOrderInfo.Equals(searchOrderNo, StringComparison.OrdinalIgnoreCase) ||
                                              fileOrderInfo.IndexOf(searchOrderNo, StringComparison.OrdinalIgnoreCase)>=0 ||
                                              searchOrderNo.IndexOf(fileOrderInfo, StringComparison.OrdinalIgnoreCase) >= 0;

                                if (isMatch)
                                {
                                    // ไฟล์นี้ตรงกับ Order No ที่ค้นหา
                                    var logEntry = new LogEntry
                                    {
                                        Timestamp = File.GetLastWriteTime(logFile),
                                        LogLevel = "INFO",
                                        RawLog = fullLogContent.Trim(), // ตัด whitespace ด้านหน้า-หลัง
                                        Message = $"Raw HL7 Data from file: {Path.GetFileName(logFile)}",
                                        ErrorDate = File.GetLastWriteTime(logFile).Date
                                    };
                                    matchingLogs.Add(logEntry);
                                    _logManager.LogInfo($"✓ Found matching raw log file: {Path.GetFileName(logFile)} (Size: {fullLogContent.Length} chars)");
                                }
                                else
                                {
                                    _logManager.LogInfo($"✗ File order '{fileOrderInfo}' does not match search order '{searchOrderNo}'");
                                }
                            }
                            else
                            {
                                _logManager.LogWarning($"Could not parse filename: {Path.GetFileName(logFile)}");
                            }
                            continue; // ข้ามไปไฟล์ถัดไปเพราะประมวลผลเสร็จแล้ว
                        }

                        // สำหรับไฟล์ error log ให้แยก entries ตาม timestamp
                        var logEntries = Regex.Split(
                            fullLogContent,
                            @"(?=\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\])"
                        );

                        _logManager.LogInfo($"Found {logEntries.Length} entries in {Path.GetFileName(logFile)}");

                        foreach (var entry in logEntries)
                        {
                            if (string.IsNullOrWhiteSpace(entry))
                                continue;

                            totalEntriesChecked++;

                            bool hasOrderNo = entry.IndexOf(searchOrderNo, StringComparison.OrdinalIgnoreCase) >= 0;

                            if (!hasOrderNo)
                            {
                                var patterns = new[]
                                {
                            $"Order No: {searchOrderNo}",
                            $"OrderNo: {searchOrderNo}",
                            $"Order: {searchOrderNo}",
                            $"PlacerOrderNumber: {searchOrderNo}",
                            $"orderNo={searchOrderNo}",
                            $"\"{searchOrderNo}\"",
                            $"'{searchOrderNo}'",
                            $"for {searchOrderNo}",
                            $"order: {searchOrderNo}",
                        };

                                foreach (var pattern in patterns)
                                {
                                    if (entry.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        hasOrderNo = true;
                                        break;
                                    }
                                }
                            }

                            if (hasOrderNo)
                            {
                                var logEntry = ParseLogEntry(entry, searchOrderNo);
                                if (logEntry != null)
                                {
                                    // ตรวจสอบวันที่ของ log entry
                                    if (_filterDate.HasValue)
                                    {
                                        // ต้องตรงกับวันที่ที่กรอง
                                        if (logEntry.ErrorDate.Date == _filterDate.Value.Date)
                                        {
                                            if (isErrorLog && errorLogFileDate.HasValue)
                                            {
                                                if (logEntry.ErrorDate.Date == errorLogFileDate.Value.Date)
                                                {
                                                    matchingLogs.Add(logEntry);
                                                }
                                            }
                                            else
                                            {
                                                matchingLogs.Add(logEntry);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // ไม่มีการกรองวันที่
                                        if (isErrorLog && errorLogFileDate.HasValue)
                                        {
                                            if (logEntry.ErrorDate.Date == errorLogFileDate.Value.Date)
                                            {
                                                matchingLogs.Add(logEntry);
                                            }
                                        }
                                        else
                                        {
                                            matchingLogs.Add(logEntry);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logManager.LogError($"Error reading log file {logFile}", ex);
                    }
                }

                string searchLog = _filterDate.HasValue ? $" on {_filterDate.Value:yyyy-MM-dd}" : "";
                _logManager.LogInfo($"Search complete: Checked {totalEntriesChecked} entries in {filesChecked} files, found {matchingLogs.Count} matches for Order No: {orderNo} (Status: {_status}){searchLog}");
            }
            catch (Exception ex)
            {
                _logManager.LogError("Error getting logs for order", ex);
            }

            // เรียงลำดับตาม Timestamp จากใหม่ไปเก่า
            matchingLogs = matchingLogs.OrderByDescending(l => l.Timestamp).ToList();

            return matchingLogs;
        }

        private LogEntry ParseLogEntry(string rawEntry, string orderNo)
        {
            try
            {
                var entry = new LogEntry();

                var timestampMatch = Regex.Match(
                    rawEntry,
                    @"\[(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})\]"
                );

                if (timestampMatch.Success)
                {
                    entry.Timestamp = DateTime.Parse(timestampMatch.Groups[1].Value);
                }
                else
                {
                    entry.Timestamp = DateTime.Now;
                }

                var logLevelMatch = Regex.Match(
                    rawEntry,
                    @"\[(INFO|ERROR|WARNING|DEBUG)\]",
                    RegexOptions.IgnoreCase
                );

                if (logLevelMatch.Success)
                {
                    entry.LogLevel = logLevelMatch.Groups[1].Value.ToUpper();
                }
                else
                {
                    if (rawEntry.Contains("Exception") || rawEntry.Contains("Error"))
                    {
                        entry.LogLevel = "ERROR";
                    }
                    else if (rawEntry.Contains("Warning"))
                    {
                        entry.LogLevel = "WARNING";
                    }
                    else
                    {
                        entry.LogLevel = "INFO";
                    }
                }

                entry.RawLog = rawEntry.TrimEnd();
                entry.Message = "";
                entry.ErrorDate = entry.Timestamp.Date;

                return entry;
            }
            catch
            {
                return null;
            }
        }

        private class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public string LogLevel { get; set; }
            public string Message { get; set; }
            public string RawLog { get; set; }
            public DateTime ErrorDate { get; set; }
        }

        #endregion

        #region DataGridView Setup

        private DataGridView CreateDataGridView()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = System.Drawing.Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                RowHeadersVisible = false
            };
            return grid;
        }

        private DataTable CreateFieldValueTable()
        {
            var table = new DataTable();
            table.Columns.Add("Field", typeof(string));
            table.Columns.Add("Value", typeof(string));
            return table;
        }

        #endregion

        #region Generic Load Methods with Reflection

        // Generic method to load simple objects (non-collection)
        private void LoadObject(DataGridView grid, object obj, string prefix = "")
        {
            var table = CreateFieldValueTable();

            if (obj != null)
            {
                LoadObjectProperties(table, obj, prefix);
            }

            grid.DataSource = table;
        }

        // Recursive method to load properties
        private void LoadObjectProperties(DataTable table, object obj, string prefix)
        {
            if (obj == null) return;

            var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(obj);
                    var fieldName = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";

                    if (value == null)
                    {
                        table.Rows.Add(fieldName, "");
                    }
                    else if (IsSimpleType(prop.PropertyType))
                    {
                        // Simple types (string, int, bool, DateTime, etc.)
                        table.Rows.Add(fieldName, value?.ToString() ?? "");
                    }
                    else if (prop.PropertyType.IsClass && !IsCollection(prop.PropertyType))
                    {
                        // Complex types - recursive (but not collections)
                        LoadObjectProperties(table, value, fieldName);
                    }
                }
                catch (Exception ex)
                {
                    // Log error and skip properties that can't be accessed
                    _logManager.LogError($"Error accessing property '{prop.Name}' in LoadObjectProperties", ex);
                }
            }
        }

        // Generic method to load collections
        private void LoadCollection<T>(DataGridView grid, IEnumerable<T> collection)
        {
            if (collection == null)
            {
                grid.DataSource = null;
                return;
            }

            var table = new DataTable();
            var type = typeof(T);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Create columns dynamically
            foreach (var prop in properties)
            {
                try
                {
                    if (IsSimpleType(prop.PropertyType))
                    {
                        table.Columns.Add(prop.Name, typeof(string));
                    }
                    else if (prop.PropertyType.IsClass && !IsCollection(prop.PropertyType))
                    {
                        // For complex properties, add sub-properties as columns
                        var subProperties = prop.PropertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        foreach (var subProp in subProperties)
                        {
                            if (IsSimpleType(subProp.PropertyType))
                            {
                                table.Columns.Add($"{prop.Name}.{subProp.Name}", typeof(string));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error and skip properties that can't be accessed
                    _logManager.LogError($"Error creating column for property '{prop.Name}' in LoadCollection", ex);
                }
            }

            // Add rows
            foreach (var item in collection)
            {
                var row = table.NewRow();

                foreach (var prop in properties)
                {
                    try
                    {
                        var value = prop.GetValue(item);

                        if (value == null)
                        {
                            if (table.Columns.Contains(prop.Name))
                                row[prop.Name] = "";
                        }
                        else if (IsSimpleType(prop.PropertyType))
                        {
                            if (table.Columns.Contains(prop.Name))
                            {
                                row[prop.Name] = value?.ToString() ?? "";
                            }
                        }
                        else if (prop.PropertyType.IsClass && value != null && !IsCollection(prop.PropertyType))
                        {
                            // Handle complex properties
                            var subProperties = prop.PropertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                            foreach (var subProp in subProperties)
                            {
                                try
                                {
                                    var subValue = subProp.GetValue(value);
                                    var columnName = $"{prop.Name}.{subProp.Name}";

                                    if (table.Columns.Contains(columnName))
                                    {
                                        if (subValue == null)
                                        {
                                            row[columnName] = "";
                                        }
                                        else
                                        {
                                            row[columnName] = subValue?.ToString() ?? "";
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // Log error and skip sub-properties that can't be accessed
                                    _logManager.LogError($"Error accessing sub-property '{subProp.Name}' of property '{prop.Name}' in LoadCollection", ex);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error and skip properties that can't be accessed
                        _logManager.LogError($"Error accessing property '{prop.Name}' in LoadCollection", ex);
                    }
                }

                table.Rows.Add(row);
            }

            grid.DataSource = table;
        }

        // Helper method to check if type is simple
        private bool IsSimpleType(Type type)
        {
            return type.IsPrimitive
                || type.IsEnum
                || type == typeof(string)
                || type == typeof(decimal)
                || type == typeof(DateTime)
                || type == typeof(DateTime?)
                || type == typeof(decimal?)
                || type == typeof(int?)
                || type == typeof(long?)
                || type == typeof(double?)
                || type == typeof(float?)
                || type == typeof(bool?)
                || Nullable.GetUnderlyingType(type) != null;
        }

        // Helper method to check if type is collection
        private bool IsCollection(Type type)
        {
            return typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string);
        }

        #endregion
    }
}