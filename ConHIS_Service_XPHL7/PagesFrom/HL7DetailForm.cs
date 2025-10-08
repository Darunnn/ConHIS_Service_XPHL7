using System;
using System.Windows.Forms;
using System.Data;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ConHIS_Service_XPHL7.Models;

namespace ConHIS_Service_XPHL7
{
    public partial class HL7DetailForm : Form
    {
        private HL7Message _hl7Message;
        private Utils.LogManager _logManager;
        private string _orderNo;

        public HL7DetailForm(HL7Message hl7Message, string orderNo)
        {
            _hl7Message = hl7Message;
            _orderNo = orderNo;
            _logManager = new Utils.LogManager();
            InitializeComponent();

            this.Text = $"HL7 Message Details - Order: {orderNo}";
            lblOrderNo.Text = $"Order No: {orderNo}";

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

        private void BtnViewLogs_Click(object sender, EventArgs e)
        {
            // Switch to Logs tab
            tabControl.SelectedTab = tabLogs;
            LoadOrderLogs();
        }

        private void BtnRefreshLogs_Click(object sender, EventArgs e)
        {
            LoadOrderLogs();
        }

        private void BtnViewRawLog_Click(object sender, EventArgs e)
        {
            try
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string projectRoot = Directory.GetParent(baseDirectory)?.Parent?.Parent?.FullName ?? baseDirectory;

                List<string> possibleLogPaths = new List<string>
                {
                    Path.Combine(baseDirectory, "logs"),
                    Path.Combine(projectRoot, "logs"),
                    Path.Combine(Directory.GetParent(baseDirectory).FullName, "logs"),
                };

                List<string> possibleErrorLogPaths = new List<string>
                {
                    Path.Combine(baseDirectory, "logreaderror"),
                    Path.Combine(projectRoot, "logreaderror"),
                    Path.Combine(Directory.GetParent(baseDirectory).FullName, "logreaderror"),
                };

                var allLogFiles = new List<string>();

                // Get normal log files
                foreach (var logDir in possibleLogPaths.Where(p => Directory.Exists(p)))
                {
                    allLogFiles.AddRange(Directory.GetFiles(logDir, "*.log"));
                }

                // Get error log files
                foreach (var errorDir in possibleErrorLogPaths.Where(p => Directory.Exists(p)))
                {
                    allLogFiles.AddRange(Directory.GetFiles(errorDir, "hl7_error_*.txt"));
                }

                if (allLogFiles.Count == 0)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("No log files found.");
                    sb.AppendLine();
                    sb.AppendLine("Searched locations:");
                    sb.AppendLine("Normal logs:");
                    foreach (var path in possibleLogPaths)
                        sb.AppendLine($"  - {path} {(Directory.Exists(path) ? "(exists)" : "(not found)")}");
                    sb.AppendLine();
                    sb.AppendLine("Error logs:");
                    foreach (var path in possibleErrorLogPaths)
                        sb.AppendLine($"  - {path} {(Directory.Exists(path) ? "(exists)" : "(not found)")}");

                    MessageBox.Show(sb.ToString(), "Information",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Get latest log file
                string latestLogFile = null;
                DateTime latestTime = DateTime.MinValue;

                foreach (var file in allLogFiles)
                {
                    var fileTime = File.GetLastWriteTime(file);
                    if (fileTime > latestTime)
                    {
                        latestTime = fileTime;
                        latestLogFile = file;
                    }
                }

                if (latestLogFile == null)
                {
                    MessageBox.Show("No log files found.", "Information",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Read and display raw log
                string rawContent = File.ReadAllText(latestLogFile, Encoding.UTF8);

                var result = new StringBuilder();
                result.AppendLine("Raw Log File: " + Path.GetFileName(latestLogFile));
                result.AppendLine("Directory: " + Path.GetDirectoryName(latestLogFile));
                result.AppendLine("Last Modified: " + File.GetLastWriteTime(latestLogFile).ToString("yyyy-MM-dd HH:mm:ss"));
                result.AppendLine("Size: " + new FileInfo(latestLogFile).Length.ToString("N0") + " bytes");
                result.AppendLine(new string('=', 80));
                result.AppendLine();
                result.AppendLine("Searching for Order No patterns...");

                // Count occurrences - simple way
                int exactCount = 0;
                int index = 0;
                while ((index = rawContent.IndexOf(_orderNo, index, StringComparison.OrdinalIgnoreCase)) != -1)
                {
                    exactCount++;
                    index += _orderNo.Length;
                }

                result.AppendLine("Found '" + _orderNo + "' mentioned " + exactCount + " time(s) in this file");
                result.AppendLine(new string('=', 80));
                result.AppendLine();

                // Show first 500 lines
                var lines = rawContent.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                int linesToShow = Math.Min(500, lines.Length);

                result.AppendLine("Showing first " + linesToShow + " of " + lines.Length + " lines:");
                result.AppendLine(new string('-', 80));

                for (int i = 0; i < linesToShow; i++)
                {
                    result.AppendLine(lines[i]);
                }

                if (lines.Length > linesToShow)
                {
                    result.AppendLine();
                    result.AppendLine("... (" + (lines.Length - linesToShow) + " more lines not shown)");
                }

                logTextBox.Text = result.ToString();
                logTextBox.SelectionStart = 0;
                logTextBox.ScrollToCaret();

                _logManager.LogInfo("Displayed raw log file: " + Path.GetFileName(latestLogFile));
            }
            catch (Exception ex)
            {
                _logManager.LogError("Error viewing raw log", ex);
                MessageBox.Show("Error viewing raw log: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                    saveFileDialog.FileName = $"OrderLog_{_orderNo}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                    saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string contentToExport;

                        if (result == DialogResult.Yes)
                        {
                            // Export full raw logs
                            var logs = GetLogsForOrder(_orderNo);
                            var sb = new StringBuilder();
                            sb.AppendLine($"=== Full Raw Logs for Order No: {_orderNo} ===");
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
                        _logManager.LogInfo($"Logs exported for Order No: {_orderNo} to {saveFileDialog.FileName}");
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

                // Debug mode - show what we're looking for
                var debugInfo = new StringBuilder();
                debugInfo.AppendLine($"Search Parameters:");
                debugInfo.AppendLine($"   Order No: '{_orderNo}' (Length: {_orderNo?.Length})");

                // Try multiple possible locations for log directories
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

                // Go up from bin\Debug to project root
                string projectRoot = Directory.GetParent(baseDirectory)?.Parent?.Parent?.FullName ?? baseDirectory;

                List<string> possibleLogPaths = new List<string>
                {
                    Path.Combine(baseDirectory, "logs"),                    // bin\Debug\logs
                    Path.Combine(projectRoot, "logs"),                      // Project\logs
                    Path.Combine(Directory.GetParent(baseDirectory).FullName, "logs"),  // bin\logs
                };

                List<string> possibleErrorLogPaths = new List<string>
                {
                    Path.Combine(baseDirectory, "logreaderror"),                // bin\Debug\logreaderror
                    Path.Combine(projectRoot, "logreaderror"),                  // Project\logreaderror
                    Path.Combine(Directory.GetParent(baseDirectory).FullName, "logreaderror"),  // bin\logreaderror
                };

                string logDirectory = possibleLogPaths.FirstOrDefault(p => Directory.Exists(p));
                string errorLogDirectory = possibleErrorLogPaths.FirstOrDefault(p => Directory.Exists(p));

                debugInfo.AppendLine($"   Base Directory: {baseDirectory}");
                debugInfo.AppendLine($"   Project Root: {projectRoot}");
                debugInfo.AppendLine();
                debugInfo.AppendLine($"   Log Directory: {logDirectory ?? "(not found)"}");
                debugInfo.AppendLine($"   Error Log Directory: {errorLogDirectory ?? "(not found)"}");
                debugInfo.AppendLine();

                // Check directories
                bool logsExists = !string.IsNullOrEmpty(logDirectory) && Directory.Exists(logDirectory);
                bool errorLogsExists = !string.IsNullOrEmpty(errorLogDirectory) && Directory.Exists(errorLogDirectory);

                debugInfo.AppendLine("Directory Status:");
                debugInfo.AppendLine($"   logs: {(logsExists ? "✓ Found" : "✗ Not found")}");
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
                    debugInfo.AppendLine("  Normal logs:");
                    foreach (var path in possibleLogPaths)
                        debugInfo.AppendLine($"    - {path}");
                    debugInfo.AppendLine("  Error logs:");
                    foreach (var path in possibleErrorLogPaths)
                        debugInfo.AppendLine($"    - {path}");

                    logTextBox.Text = debugInfo.ToString();
                    return;
                }

                // Count log files
                int totalLogFiles = 0;
                if (logsExists)
                {
                    totalLogFiles += Directory.GetFiles(logDirectory, "*.log").Length;
                }
                if (errorLogsExists)
                {
                    totalLogFiles += Directory.GetFiles(errorLogDirectory, "hl7_error_*.txt").Length;
                }

                debugInfo.AppendLine($"Found {totalLogFiles} log file(s) to search");
                debugInfo.AppendLine(new string('=', 80));
                debugInfo.AppendLine();

                logTextBox.Text = debugInfo.ToString();
                logTextBox.Refresh();

                var logs = GetLogsForOrder(_orderNo, logDirectory, errorLogDirectory);

                if (logs.Count == 0)
                {
                    var sb = new StringBuilder();
                    sb.Append(debugInfo.ToString());
                    sb.AppendLine($"No logs found for Order No: {_orderNo}");
                    sb.AppendLine($"Search completed at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine();
                    sb.AppendLine("Troubleshooting:");
                    sb.AppendLine("• Check if log files exist in the logs folder");
                    sb.AppendLine("• Verify the Order No is correct (check for spaces)");
                    sb.AppendLine("• Order No might be in a different format in the log");
                    sb.AppendLine();
                    sb.AppendLine("Tip: Click 'View Raw Log' to see the complete log file");

                    logTextBox.Text = sb.ToString();
                }
                else
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"=== Logs for Order No: {_orderNo} ===");
                    sb.AppendLine($"Total Entries: {logs.Count} | Retrieved: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine(new string('=', 80));
                    sb.AppendLine();

                    // แสดง log แบบ raw format เหมือนข้างนอก
                    foreach (var log in logs)
                    {
                        sb.AppendLine(log.RawLog);
                    }

                    // สรุปสถิติ
                    var errorCount = logs.Count(l => l.LogLevel == "ERROR");
                    var warningCount = logs.Count(l => l.LogLevel == "WARNING");
                    var infoCount = logs.Count(l => l.LogLevel == "INFO");

                    sb.AppendLine();
                    sb.AppendLine(new string('=', 80));
                    sb.AppendLine("Statistics:");
                    sb.AppendLine($"Errors: {errorCount} | Warnings: {warningCount} | Info: {infoCount}");
                    sb.AppendLine(new string('=', 80));

                    logTextBox.Text = sb.ToString();
                }

                // Scroll to top
                logTextBox.SelectionStart = 0;
                logTextBox.SelectionLength = 0;
                logTextBox.ScrollToCaret();

                _logManager.LogInfo($"Searched for Order No: {_orderNo}, found {logs.Count} log entries");
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
                // If directories not provided, try to find them
                if (string.IsNullOrEmpty(logDirectory) && string.IsNullOrEmpty(errorLogDirectory))
                {
                    string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    string projectRoot = Directory.GetParent(baseDirectory)?.Parent?.Parent?.FullName ?? baseDirectory;

                    List<string> possibleLogPaths = new List<string>
                    {
                        Path.Combine(baseDirectory, "logs"),
                        Path.Combine(projectRoot, "logs"),
                        Path.Combine(Directory.GetParent(baseDirectory).FullName, "logs"),
                    };

                    List<string> possibleErrorLogPaths = new List<string>
                    {
                        Path.Combine(baseDirectory, "logreaderror"),
                        Path.Combine(projectRoot, "logreaderror"),
                        Path.Combine(Directory.GetParent(baseDirectory).FullName, "logreaderror"),
                    };

                    logDirectory = possibleLogPaths.FirstOrDefault(p => Directory.Exists(p));
                    errorLogDirectory = possibleErrorLogPaths.FirstOrDefault(p => Directory.Exists(p));
                }

                var allLogFiles = new List<string>();

                // Get normal log files
                if (!string.IsNullOrEmpty(logDirectory) && Directory.Exists(logDirectory))
                {
                    var normalLogs = Directory.GetFiles(logDirectory, "*.log")
                        .OrderByDescending(f => File.GetLastWriteTime(f))
                        .Take(30)
                        .ToList();
                    allLogFiles.AddRange(normalLogs);
                    _logManager.LogInfo($"Found {normalLogs.Count} normal log files in: {logDirectory}");
                }

                // Get error log files (hl7_error_*.txt)
                if (!string.IsNullOrEmpty(errorLogDirectory) && Directory.Exists(errorLogDirectory))
                {
                    // Get all error log files first
                    var allErrorLogs = Directory.GetFiles(errorLogDirectory, "hl7_error_*.txt")
                        .OrderByDescending(f => File.GetLastWriteTime(f))
                        .ToList();

                    // Store error logs with their dates for later filtering
                    var errorLogsWithDates = new Dictionary<string, DateTime>();

                    foreach (var errorLog in allErrorLogs)
                    {
                        // Parse date from filename: hl7_error_2568-10-08.txt
                        var dateMatch = Regex.Match(Path.GetFileName(errorLog), @"hl7_error_(\d{4}-\d{2}-\d{2})");
                        if (dateMatch.Success)
                        {
                            if (DateTime.TryParse(dateMatch.Groups[1].Value, out DateTime fileDate))
                            {
                                errorLogsWithDates[errorLog] = fileDate;
                            }
                        }
                    }

                    // Take only recent 30 files
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

                _logManager.LogInfo($"Searching total {allLogFiles.Count} log files for Order No: {orderNo}");

                // ทำให้การค้นหายืดหยุ่นขึ้น - trim whitespace และ case insensitive
                string searchOrderNo = orderNo?.Trim() ?? "";

                int totalEntriesChecked = 0;
                int filesChecked = 0;

                foreach (var logFile in allLogFiles)
                {
                    try
                    {
                        filesChecked++;
                        _logManager.LogInfo($"Checking file {filesChecked}/{allLogFiles.Count}: {Path.GetFileName(logFile)}");

                        // ตรวจสอบว่าเป็นไฟล์ error log หรือไม่
                        bool isErrorLog = Path.GetFileName(logFile).StartsWith("hl7_error_");
                        DateTime? errorLogFileDate = null;

                        if (isErrorLog)
                        {
                            // Parse date from error log filename
                            var dateMatch = Regex.Match(Path.GetFileName(logFile), @"hl7_error_(\d{4}-\d{2}-\d{2})");
                            if (dateMatch.Success)
                            {
                                if (DateTime.TryParse(dateMatch.Groups[1].Value, out DateTime fileDate))
                                {
                                    errorLogFileDate = fileDate;
                                }
                            }
                        }

                        // อ่านไฟล์ทั้งหมดเป็น string เดียว
                        string fullLogContent = File.ReadAllText(logFile, Encoding.UTF8);

                        if (string.IsNullOrEmpty(fullLogContent))
                        {
                            _logManager.LogWarning($"File {Path.GetFileName(logFile)} is empty");
                            continue;
                        }

                        // แยก log entries ตาม pattern timestamp
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

                            // ค้นหาแบบ flexible - trim และ case insensitive
                            bool hasOrderNo = entry.IndexOf(searchOrderNo, StringComparison.OrdinalIgnoreCase) >= 0;

                            // ลองค้นหาด้วยรูปแบบต่างๆ ที่อาจปรากฏใน log
                            if (!hasOrderNo)
                            {
                                // ลองค้นหาด้วย pattern ต่างๆ
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
                                    // ถ้าเป็น error log ให้ตรวจสอบว่าวันที่ตรงกันหรือไม่
                                    if (isErrorLog && errorLogFileDate.HasValue)
                                    {
                                        // เช็คว่าวันที่ของ log entry ตรงกับชื่อไฟล์หรือไม่
                                        if (logEntry.ErrorDate.Date == errorLogFileDate.Value.Date)
                                        {
                                            matchingLogs.Add(logEntry);
                                            _logManager.LogInfo($"Found matching entry #{matchingLogs.Count} in {Path.GetFileName(logFile)} (date matched: {errorLogFileDate.Value:yyyy-MM-dd})");
                                        }
                                        else
                                        {
                                            _logManager.LogInfo($"Skipped entry in {Path.GetFileName(logFile)} - date mismatch (log: {logEntry.ErrorDate:yyyy-MM-dd}, file: {errorLogFileDate.Value:yyyy-MM-dd})");
                                        }
                                    }
                                    else
                                    {
                                        // Normal log file - add without date check
                                        matchingLogs.Add(logEntry);
                                        _logManager.LogInfo($"Found matching entry #{matchingLogs.Count} in {Path.GetFileName(logFile)}");
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

                _logManager.LogInfo($"Search complete: Checked {totalEntriesChecked} entries in {filesChecked} files, found {matchingLogs.Count} matches for Order No: {orderNo}");
            }
            catch (Exception ex)
            {
                _logManager.LogError("Error getting logs for order", ex);
            }

            // Sort by timestamp (newest first)
            matchingLogs = matchingLogs.OrderByDescending(l => l.Timestamp).ToList();

            return matchingLogs;
        }

        private LogEntry ParseLogEntry(string rawEntry, string orderNo)
        {
            try
            {
                var entry = new LogEntry();

                // Parse timestamp
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

                // Parse log level (INFO, ERROR, WARNING, etc.)
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
                    // Try to detect log level from content
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

                // Store raw log
                entry.RawLog = rawEntry.TrimEnd();

                // Extract message (ไม่ใช้แล้ว เพราะแสดง raw)
                entry.Message = "";

                // Store error date for filtering
                entry.ErrorDate = entry.Timestamp.Date;

                return entry;
            }
            catch
            {
                return null;
            }
        }

        // คลาสเก็บข้อมูล Log Entry
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