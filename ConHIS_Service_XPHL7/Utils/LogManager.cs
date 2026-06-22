using ConHIS_Service_XPHL7.Configuration;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;

namespace ConHIS_Service_XPHL7.Utils
{
    public class LogManager
    {
        private string _logFolder;
        public string LogFolder => _logFolder;

        private int _logRetentionDays = 30;
        public int LogRetentionDays
        {
            get => _logRetentionDays;
            set => _logRetentionDays = value > 0 ? value : 30;
        }
        private static bool _hasLoggedInit = false;

        public LogManager(string logFolder = "log", int logRetentionDays = 30)
        {
            var appFolder = AppDomain.CurrentDomain.BaseDirectory ?? Environment.CurrentDirectory;
            var desired = Path.Combine(appFolder, logFolder);
            Directory.CreateDirectory(desired);
            _logFolder = desired;

            _logRetentionDays = LoadLogRetentionDaysFromConfig(logRetentionDays);

            if (!_hasLoggedInit)
            {
                _hasLoggedInit = true;
                LogInfo($"LogManager initialized with retention period: {_logRetentionDays} days (from App.config)");
            }
        }

        private int LoadLogRetentionDaysFromConfig(int defaultValue)
        {
            try
            {
                string configValue = ConfigurationManager.AppSettings["LogRetentionDays"];
                if (!string.IsNullOrEmpty(configValue) && int.TryParse(configValue, out int days) && days > 0)
                    return days;
                return defaultValue;
            }
            catch (Exception ex)
            {
                LogError("Error loading LogRetentionDays from App.config", ex);
                return defaultValue;
            }
        }

        public void ReloadLogRetentionDays()
        {
            try
            {
                ConfigurationManager.RefreshSection("appSettings");
                _logRetentionDays = LoadLogRetentionDaysFromConfig(30);
                LogInfo($"LogRetentionDays reloaded from App.config: {_logRetentionDays} days");
            }
            catch (Exception ex)
            {
                LogError("Error reloading LogRetentionDays", ex);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  GENERAL LOG
        // ════════════════════════════════════════════════════════════════════

        public void LogToFile(string message, string logType = "INFO")
        {
            var logFileName = $"{DateTime.Now:yyyy-MM-dd}.log";
            var logPath = Path.Combine(_logFolder, logFileName);
            var logEntry = $"[{DateTime.Now:HH:mm:ss}] [{logType}] {message}{Environment.NewLine}";

            try { File.AppendAllText(logPath, logEntry); }
            catch (Exception ex) { LogError("Error writing to log file", ex); }
        }

        // ════════════════════════════════════════════════════════════════════
        //  RAW HL7
        // ════════════════════════════════════════════════════════════════════

        public void LogRawHL7Data(string DrugDispenseipdId, string RecieveOrderType, string orderno,
            string hl7Data, string rawLogFolder = "hl7_raw")
        {
            var appFolder = AppDomain.CurrentDomain.BaseDirectory ?? Environment.CurrentDirectory;
            var rawLogBaseDir = Path.Combine(appFolder, rawLogFolder);

            var dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
            var rawLogDir = Path.Combine(rawLogBaseDir, dateFolder);
            Directory.CreateDirectory(rawLogDir);

            CleanOldLogFolders(rawLogBaseDir);

            var safeOrderNo = SanitizeFileName(orderno);
            var safeDispenseId = SanitizeFileName(DrugDispenseipdId);
            var safeOrderType = SanitizeFileName(RecieveOrderType);

            var rawLogPath = Path.Combine(rawLogDir,
                $"hl7_raw_{safeDispenseId}_{safeOrderType}_{safeOrderNo}.log");

            try { File.WriteAllText(rawLogPath, hl7Data); }
            catch (Exception ex) { LogError($"Error logging raw HL7 data for {DrugDispenseipdId}", ex); }
        }

        // ════════════════════════════════════════════════════════════════════
        //  PARSED HL7
        // ════════════════════════════════════════════════════════════════════

        public void LogParsedHL7Data(string DrugDispenseipdId, object parsedData,
            string parsedLogFolder = "hl7_parsed")
        {
            var appFolder = AppDomain.CurrentDomain.BaseDirectory ?? Environment.CurrentDirectory;
            var parsedLogBaseDir = Path.Combine(appFolder, parsedLogFolder);

            var dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
            var parsedLogDir = Path.Combine(parsedLogBaseDir, dateFolder);
            Directory.CreateDirectory(parsedLogDir);

            CleanOldLogFolders(parsedLogBaseDir);

            var safeDispenseId = SanitizeFileName(DrugDispenseipdId);
            var parsedLogPath = Path.Combine(parsedLogDir, $"hl7_parsed_{safeDispenseId}.log");

            try
            {
                var jsonData = JsonConvert.SerializeObject(parsedData, Formatting.Indented);
                File.WriteAllText(parsedLogPath, jsonData);
            }
            catch (Exception ex) { LogError($"Error logging parsed HL7 data for {DrugDispenseipdId}", ex); }
        }

        // ════════════════════════════════════════════════════════════════════
        //  ⭐ API JSON REQUEST — IPD / OPD แยกโฟลเดอร์ + แยกวันที่
        //
        //  โครงสร้างโฟลเดอร์:
        //  api_json_request/
        //  ├── ipd/
        //  │   ├── 2025-10-15/
        //  │   │   ├── api_json_101_ORD001_093045.json
        //  │   │   └── api_json_102_ORD002_093102.json
        //  │   └── 2025-10-16/
        //  └── opd/
        //      ├── 2025-10-15/
        //      └── 2025-10-16/
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// บันทึก JSON body ที่ส่งไป API แยกโฟลเดอร์ IPD / OPD และแยกวันที่
        /// </summary>
        public void LogApiJsonRequest(
            string dispenseId,
            string orderType,          // "IPD" หรือ "OPD"
            string orderNo,
            object jsonData,
            string baseFolder = "api_json_request")
        {
            try
            {
                var appFolder = AppDomain.CurrentDomain.BaseDirectory ?? Environment.CurrentDirectory;
                var baseDir = Path.Combine(appFolder, baseFolder);

                // ── โฟลเดอร์ย่อยตาม orderType (ipd / opd) ─────────────────
                var typeFolder = (orderType ?? "unknown").ToLower();
                var typeDir = Path.Combine(baseDir, typeFolder);

                // ── โฟลเดอร์ย่อยตามวันที่ ──────────────────────────────────
                var dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
                var targetDir = Path.Combine(typeDir, dateFolder);
                Directory.CreateDirectory(targetDir);

                // 🧹 ทำความสะอาดโฟลเดอร์เก่าในแต่ละ typeDir แยกกัน
                CleanOldLogFolders(typeDir);

                // ── ชื่อไฟล์: api_json_{id}_{orderNo}_{HHmmss}.json ────────
                var safeId = SanitizeFileName(dispenseId ?? "unknown");
                var safeOrderNo = SanitizeFileName(orderNo ?? "unknown");
                var timestamp = DateTime.Now.ToString("HHmmss");
                var filePath = Path.Combine(
                    targetDir,
                    $"api_json_{safeId}_{safeOrderNo}_{timestamp}.json");

                // ── เขียนไฟล์ JSON ──────────────────────────────────────────
                var json = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
                File.WriteAllText(filePath, json, Encoding.UTF8);

                LogInfo($"[LogApiJsonRequest] Saved → {Path.GetFileName(filePath)} ({typeFolder}/{dateFolder})");
            }
            catch (Exception ex)
            {
                LogError($"[LogApiJsonRequest] Failed to save JSON for ID={dispenseId}", ex);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  CONNECTION LOG
        // ════════════════════════════════════════════════════════════════════

        public void LogConnectDatabase(bool isConnected, DateTime? lastConnectedTime = null,
            DateTime? lastDisconnectedTime = null, string connectLogFolder = "Connection")
        {
            var appFolder = AppDomain.CurrentDomain.BaseDirectory ?? Environment.CurrentDirectory;
            var connectLogBaseDir = Path.Combine(appFolder, connectLogFolder);

            var dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
            var connectLogDir = Path.Combine(connectLogBaseDir, dateFolder);
            Directory.CreateDirectory(connectLogDir);

            CleanOldLogFolders(connectLogBaseDir);

            var connectLogPath = Path.Combine(connectLogDir, $"connection_{dateFolder}.log");
            var status = isConnected ? "✓ Connected" : "✗ Disconnected";
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logEntry = $"[{timestamp}] Database Status: {status}";

            if (isConnected && lastConnectedTime.HasValue)
                logEntry += $" | Last Connected: {lastConnectedTime.Value:yyyy-MM-dd HH:mm:ss}";
            else if (!isConnected && lastDisconnectedTime.HasValue)
            {
                logEntry += $" | Disconnected at: {lastDisconnectedTime.Value:yyyy-MM-dd HH:mm:ss}";
                if (lastConnectedTime.HasValue)
                    logEntry += $" | Last Connected: {lastConnectedTime.Value:yyyy-MM-dd HH:mm:ss}";
            }

            logEntry += Environment.NewLine;

            try { File.AppendAllText(connectLogPath, logEntry); }
            catch (Exception ex) { LogError("Error logging database connection status", ex); }
        }

        // ════════════════════════════════════════════════════════════════════
        //  CLEANUP
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// ลบโฟลเดอร์ย่อย (yyyy-MM-dd) ที่เก่ากว่าจำนวนวันที่กำหนด
        /// </summary>
        private void CleanOldLogFolders(string baseLogFolder)
        {
            try
            {
                if (!Directory.Exists(baseLogFolder)) return;

                var cutoffDate = DateTime.Now.AddDays(-_logRetentionDays);
                var directories = Directory.GetDirectories(baseLogFolder);

                foreach (var dir in directories)
                {
                    var folderName = Path.GetFileName(dir);

                    if (DateTime.TryParseExact(folderName, "yyyy-MM-dd",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out DateTime folderDate) && folderDate < cutoffDate)
                    {
                        try
                        {
                            Directory.Delete(dir, true);
                            LogInfo($"Deleted old log folder: {dir}");
                        }
                        catch (Exception ex)
                        {
                            LogError($"Error deleting old log folder: {dir}", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error cleaning old log folders in {baseLogFolder}", ex);
            }
        }

        /// <summary>
        /// เรียกทำความสะอาด log ทั้งหมด (เรียกจาก Scheduler หรือปุ่ม)
        /// </summary>
        public void CleanOldLogs()
        {
            var appFolder = AppDomain.CurrentDomain.BaseDirectory ?? Environment.CurrentDirectory;

            LogInfo($"Starting log cleanup. Retention period: {_logRetentionDays} days");

            // โฟลเดอร์เดิม
            CleanOldLogFolders(Path.Combine(appFolder, "hl7_raw"));
            CleanOldLogFolders(Path.Combine(appFolder, "hl7_parsed"));
            CleanOldLogFolders(Path.Combine(appFolder, "logreaderror"));
            CleanOldLogFolders(Path.Combine(appFolder, "Connection"));

            // ⭐ โฟลเดอร์ใหม่ — แยก IPD / OPD
            CleanOldLogFolders(Path.Combine(appFolder, "api_json_request", "ipd"));
            CleanOldLogFolders(Path.Combine(appFolder, "api_json_request", "opd"));

            // ทำความสะอาดโฟลเดอร์ log หลัก
            CleanOldLogFiles(_logFolder);

            LogInfo($"Log cleanup completed. Retention period: {_logRetentionDays} days");
        }

        private void CleanOldLogFiles(string logFolder)
        {
            try
            {
                if (!Directory.Exists(logFolder)) return;

                var cutoffDate = DateTime.Now.AddDays(-_logRetentionDays);
                var files = Directory.GetFiles(logFolder, "*.log");

                foreach (var file in files)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);

                    if (DateTime.TryParseExact(fileName, "yyyy-MM-dd",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out DateTime fileDate) && fileDate < cutoffDate)
                    {
                        try
                        {
                            File.Delete(file);
                            LogInfo($"Deleted old log file: {file}");
                        }
                        catch (Exception ex)
                        {
                            LogError($"Error deleting old log file: {file}", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error cleaning old log files in {logFolder}", ex);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════════════════════════════════

        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return "unknown";

            var invalidChars = Path.GetInvalidFileNameChars();
            var result = new StringBuilder(fileName.Length);

            foreach (var c in fileName)
                result.Append(Array.IndexOf(invalidChars, c) >= 0 ? '_' : c);

            return result.ToString();
        }

        public void LogReadError(string DrugDispenseipdId, string errorMessage,
            string errorLogFolder = "logreaderror")
        {
            var appFolder = AppDomain.CurrentDomain.BaseDirectory ?? Environment.CurrentDirectory;
            var errorLogBaseDir = Path.Combine(appFolder, errorLogFolder);

            var dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
            var errorLogDir = Path.Combine(errorLogBaseDir, dateFolder);
            Directory.CreateDirectory(errorLogDir);

            CleanOldLogFolders(errorLogBaseDir);

            var safeDispenseId = SanitizeFileName(DrugDispenseipdId);
            var errorLogPath = Path.Combine(errorLogDir, $"hl7_error_{safeDispenseId}.log");
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {errorMessage}{Environment.NewLine}";

            try { File.AppendAllText(errorLogPath, logEntry); }
            catch (Exception ex)
            {
                try
                {
                    var fallbackPath = Path.Combine(appFolder, "critical_error.log");
                    var fallbackEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] CRITICAL: Failed to log error - {ex.Message}{Environment.NewLine}";
                    File.AppendAllText(fallbackPath, fallbackEntry);
                }
                catch { }
            }
        }

        public void LogError(string message, Exception ex = null)
        {
            var fullMessage = ex != null
                ? $"{message} - Exception: {ex.Message}{Environment.NewLine}{ex.StackTrace}"
                : message;

            LogReadError(DateTime.Now.ToString("yyyy-MM-dd"), fullMessage);
        }

        public void LogInfo(string message) => LogToFile(message, "INFO");
        public void LogWarning(string message) => LogToFile(message, "WARNING");
    }
}