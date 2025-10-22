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

        // ⚙️ จำนวนวันที่จะเก็บ log (ค่าเริ่มต้น 30 วัน)
        private int _logRetentionDays = 30;
        public int LogRetentionDays
        {
            get => _logRetentionDays;
            set => _logRetentionDays = value > 0 ? value : 30;
        }

        public LogManager(string logFolder = "log", int logRetentionDays = 30)
        {
            var appFolder = AppDomain.CurrentDomain.BaseDirectory ?? Environment.CurrentDirectory;
            var desired = Path.Combine(appFolder, logFolder);
            Directory.CreateDirectory(desired);
            _logFolder = desired;

            // 🔧 เปิดโปรแกรมทุกครั้งให้อ่านจาก App.config เสมอ
            _logRetentionDays = LoadLogRetentionDaysFromConfig(logRetentionDays);

            LogInfo($"LogManager initialized with retention period: {_logRetentionDays} days (from App.config)");
        }

        /// <summary>
        /// อ่านค่า LogRetentionDays จาก App.config
        /// </summary>
        private int LoadLogRetentionDaysFromConfig(int defaultValue)
        {
            try
            {
                string configValue = ConfigurationManager.AppSettings["LogRetentionDays"];

                if (!string.IsNullOrEmpty(configValue) && int.TryParse(configValue, out int days))
                {
                    if (days > 0)
                    {
                        Console.WriteLine($"✅ Loaded LogRetentionDays from App.config: {days} days");
                        return days;
                    }
                }

                Console.WriteLine($"⚠️ LogRetentionDays not found in App.config, using default: {defaultValue} days");
                return defaultValue;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error reading LogRetentionDays from App.config: {ex.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// อัปเดตค่า LogRetentionDays ชั่วคราว (ใช้ได้เฉพาะ session นี้)
        /// เมื่อปิดโปรแกรมแล้วเปิดใหม่จะกลับไปอ่าน App.config
        /// </summary>
        public void UpdateLogRetentionDaysTemporary(int days)
        {
            try
            {
                if (days <= 0)
                {
                    throw new ArgumentException("Days must be greater than 0", nameof(days));
                }

                _logRetentionDays = days;

                LogInfo($"LogRetentionDays temporarily updated to: {days} days (for this session only)");
                Console.WriteLine($"💾 LogRetentionDays updated temporarily: {days} days");
                Console.WriteLine($"📌 Note: This setting will reset to App.config value when program restarts");
            }
            catch (Exception ex)
            {
                LogError("Error updating log retention days", ex);
                throw;
            }
        }

        /// <summary>
        /// รีโหลดค่า LogRetentionDays จาก App.config
        /// </summary>
        public void ReloadLogRetentionDays()
        {
            try
            {
                Console.WriteLine("🔄 Reloading LogRetentionDays from App.config...");

                // Refresh config section เพื่อให้ได้ค่าล่าสุด
                ConfigurationManager.RefreshSection("appSettings");

                _logRetentionDays = LoadLogRetentionDaysFromConfig(30);

                LogInfo($"LogRetentionDays reloaded from App.config: {_logRetentionDays} days");
            }
            catch (Exception ex)
            {
                LogError("Error reloading LogRetentionDays", ex);
            }
        }

        public void LogToFile(string message, string logType = "INFO")
        {
            var logFileName = $"{DateTime.Now:yyyy-MM-dd}.txt";
            var logPath = Path.Combine(_logFolder, logFileName);
            var logEntry = $"[{DateTime.Now:HH:mm:ss}] [{logType}] {message}{Environment.NewLine}";

            try
            {
                File.AppendAllText(logPath, logEntry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write log to file: {ex}");
            }
        }

        // 🧾 Raw HL7 - เก็บในโฟลเดอร์ตามวันที่
        public void LogRawHL7Data(string DrugDispenseipdId, string RecieveOrderType, string orderno, string hl7Data, string rawLogFolder = "hl7_raw")
        {
            var appFolder = AppDomain.CurrentDomain.BaseDirectory ?? Environment.CurrentDirectory;
            var rawLogBaseDir = Path.Combine(appFolder, rawLogFolder);

            // 📁 สร้างโฟลเดอร์ตามวันที่ เช่น hl7_raw/2025-10-15
            var dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
            var rawLogDir = Path.Combine(rawLogBaseDir, dateFolder);
            Directory.CreateDirectory(rawLogDir);

            // 🧹 ทำความสะอาดโฟลเดอร์เก่า
            CleanOldLogFolders(rawLogBaseDir);

            // ⭐ ทำความสะอาดชื่อไฟล์
            var safeOrderNo = SanitizeFileName(orderno);
            var safeDispenseId = SanitizeFileName(DrugDispenseipdId);
            var safeOrderType = SanitizeFileName(RecieveOrderType);

            var rawLogPath = Path.Combine(rawLogDir, $"hl7_raw_{safeDispenseId}_{safeOrderType}_{safeOrderNo}.txt");

            try
            {
                File.WriteAllText(rawLogPath, hl7Data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write HL7 raw data file for DrugDispenseipdId {DrugDispenseipdId}: {ex}");
            }
        }

        // 📊 Parsed HL7 - เก็บในโฟลเดอร์ตามวันที่
        public void LogParsedHL7Data(string DrugDispenseipdId, object parsedData, string parsedLogFolder = "hl7_parsed")
        {
            var appFolder = AppDomain.CurrentDomain.BaseDirectory ?? Environment.CurrentDirectory;
            var parsedLogBaseDir = Path.Combine(appFolder, parsedLogFolder);

            // 📁 สร้างโฟลเดอร์ตามวันที่ เช่น hl7_parsed/2025-10-15
            var dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
            var parsedLogDir = Path.Combine(parsedLogBaseDir, dateFolder);
            Directory.CreateDirectory(parsedLogDir);

            // 🧹 ทำความสะอาดโฟลเดอร์เก่า
            CleanOldLogFolders(parsedLogBaseDir);

            // ⭐ ทำความสะอาดชื่อไฟล์
            var safeDispenseId = SanitizeFileName(DrugDispenseipdId);

            var parsedLogPath = Path.Combine(parsedLogDir, $"hl7_parsed_{safeDispenseId}.txt");

            try
            {
                var jsonData = JsonConvert.SerializeObject(parsedData, Formatting.Indented);
                File.WriteAllText(parsedLogPath, jsonData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write HL7 parsed data file for DrugDispenseipdId {DrugDispenseipdId}: {ex}");
            }
        }
        public void LogConnectDatabase(bool isConnected, DateTime? lastConnectedTime = null, DateTime? lastDisconnectedTime = null, string connectLogFolder = "Connection")
        {
            var appFolder = AppDomain.CurrentDomain.BaseDirectory ?? Environment.CurrentDirectory;
            var connectLogBaseDir = Path.Combine(appFolder, connectLogFolder);

            // 📁 สร้างโฟลเดอร์ตามวันที่ เช่น Connection/2025-10-15
            var dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
            var connectLogDir = Path.Combine(connectLogBaseDir, dateFolder);
            Directory.CreateDirectory(connectLogDir);

            // 🧹 ทำความสะอาดโฟลเดอร์เก่า
            CleanOldLogFolders(connectLogBaseDir);

            // สร้างชื่อไฟล์ตามวันที่
            var connectLogPath = Path.Combine(connectLogDir, $"connection_{dateFolder}.txt");

            // สร้าง log message
            var status = isConnected ? "✓ Connected" : "✗ Disconnected";
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            var logEntry = $"[{timestamp}] Database Status: {status}";

            if (isConnected && lastConnectedTime.HasValue)
            {
                logEntry += $" | Last Connected: {lastConnectedTime.Value:yyyy-MM-dd HH:mm:ss}";
            }
            else if (!isConnected && lastDisconnectedTime.HasValue)
            {
                logEntry += $" | Disconnected at: {lastDisconnectedTime.Value:yyyy-MM-dd HH:mm:ss}";
                if (lastConnectedTime.HasValue)
                {
                    logEntry += $" | Last Connected: {lastConnectedTime.Value:yyyy-MM-dd HH:mm:ss}";
                }
            }

            logEntry += Environment.NewLine;

            try
            {
                File.AppendAllText(connectLogPath, logEntry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write connection log: {ex.Message}");
            }
        }
        // 🧹 ลบโฟลเดอร์ที่เก่ากว่าจำนวนวันที่กำหนด
        private void CleanOldLogFolders(string baseLogFolder)
        {
            try
            {
                if (!Directory.Exists(baseLogFolder))
                    return;

                var cutoffDate = DateTime.Now.AddDays(-_logRetentionDays);
                var directories = Directory.GetDirectories(baseLogFolder);

                foreach (var dir in directories)
                {
                    var folderName = Path.GetFileName(dir);

                    // พยายามแปลงชื่อโฟลเดอร์เป็นวันที่ (รูปแบบ yyyy-MM-dd)
                    if (DateTime.TryParseExact(folderName, "yyyy-MM-dd",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out DateTime folderDate))
                    {
                        // ถ้าโฟลเดอร์เก่ากว่าวันที่กำหนด ให้ลบทิ้ง
                        if (folderDate < cutoffDate)
                        {
                            try
                            {
                                Directory.Delete(dir, true); // true = ลบทั้งไฟล์ภายใน
                                Console.WriteLine($"Deleted old log folder: {dir}");
                                LogInfo($"Deleted old log folder: {dir}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to delete folder {dir}: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning old log folders in {baseLogFolder}: {ex.Message}");
            }
        }

        // 🧹 Method สำหรับเรียกทำความสะอาดด้วยตัวเอง (สำหรับ Scheduler หรือ Button)
        public void CleanOldLogs()
        {
            var appFolder = AppDomain.CurrentDomain.BaseDirectory ?? Environment.CurrentDirectory;

            LogInfo($"Starting log cleanup. Retention period: {_logRetentionDays} days");
            Console.WriteLine($"🧹 Starting log cleanup. Retention period: {_logRetentionDays} days");

            // ทำความสะอาดทุกโฟลเดอร์ log
            CleanOldLogFolders(Path.Combine(appFolder, "hl7_raw"));
            CleanOldLogFolders(Path.Combine(appFolder, "hl7_parsed"));
            CleanOldLogFolders(Path.Combine(appFolder, "logreaderror"));
            CleanOldLogFolders(Path.Combine(appFolder, "Connection")); // เพิ่ม

            // ทำความสะอาดโฟลเดอร์ log หลักด้วย
            CleanOldLogFiles(_logFolder);

            Console.WriteLine($"✅ Log cleanup completed. Retention period: {_logRetentionDays} days");
            LogInfo($"Log cleanup completed. Retention period: {_logRetentionDays} days");
        }

        // 🧹 ลบไฟล์ log เก่าในโฟลเดอร์หลัก (สำหรับไฟล์ที่ไม่ได้จัดเก็บในโฟลเดอร์ย่อย)
        private void CleanOldLogFiles(string logFolder)
        {
            try
            {
                if (!Directory.Exists(logFolder))
                    return;

                var cutoffDate = DateTime.Now.AddDays(-_logRetentionDays);
                var files = Directory.GetFiles(logFolder, "*.txt");

                foreach (var file in files)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);

                    // พยายามแปลงชื่อไฟล์เป็นวันที่ (รูปแบบ yyyy-MM-dd)
                    if (DateTime.TryParseExact(fileName, "yyyy-MM-dd",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out DateTime fileDate))
                    {
                        if (fileDate < cutoffDate)
                        {
                            try
                            {
                                File.Delete(file);
                                Console.WriteLine($"Deleted old log file: {file}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to delete file {file}: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning old log files in {logFolder}: {ex.Message}");
            }
        }

        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "unknown";

            var invalidChars = Path.GetInvalidFileNameChars();
            var result = new StringBuilder(fileName.Length);

            foreach (var c in fileName)
            {
                if (Array.IndexOf(invalidChars, c) >= 0)
                    result.Append('_');
                else
                    result.Append(c);
            }

            return result.ToString();
        }

        public void LogReadError(string DrugDispenseipdId, string errorMessage, string errorLogFolder = "logreaderror")
        {
            var appFolder = AppDomain.CurrentDomain.BaseDirectory ?? Environment.CurrentDirectory;
            var errorLogBaseDir = Path.Combine(appFolder, errorLogFolder);

            // 📁 สร้างโฟลเดอร์ตามวันที่
            var dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
            var errorLogDir = Path.Combine(errorLogBaseDir, dateFolder);
            Directory.CreateDirectory(errorLogDir);

            // 🧹 ทำความสะอาดโฟลเดอร์เก่า
            CleanOldLogFolders(errorLogBaseDir);

            var safeDispenseId = SanitizeFileName(DrugDispenseipdId);
            var errorLogPath = Path.Combine(errorLogDir, $"hl7_error_{safeDispenseId}.txt");
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {errorMessage}{Environment.NewLine}";

            try
            {
                File.AppendAllText(errorLogPath, logEntry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write HL7 read error log for DrugDispenseipdId {DrugDispenseipdId}: {ex}");
            }
        }

        // ⚙️ LogError ทุกอันจะเขียนเข้า logreaderror โดยใช้ชื่อไฟล์เป็นวันที่
        public void LogError(string message, Exception ex = null)
        {
            var fullMessage = ex != null
                ? $"{message} - Exception: {ex.Message}{Environment.NewLine}{ex.StackTrace}"
                : message;

            var dateFileName = DateTime.Now.ToString("yyyy-MM-dd");
            LogReadError(dateFileName, fullMessage);
        }

        public void LogInfo(string message)
        {
            LogToFile(message, "INFO");
        }

        public void LogWarning(string message)
        {
            LogToFile(message, "WARNING");
        }

        // 🔌 Log การเชื่อมต่อ Database - เก็บในโฟลเดอร์ตามวันที่
       
    }
}