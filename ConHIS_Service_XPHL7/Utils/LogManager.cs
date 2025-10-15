using ConHIS_Service_XPHL7.Configuration;
using Newtonsoft.Json;
using System;
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
            _logRetentionDays = logRetentionDays > 0 ? logRetentionDays : 30;
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

            var timestamp = DateTime.Now.ToString("HHmmss");
            var rawLogPath = Path.Combine(rawLogDir, $"hl7_raw_{safeDispenseId}_{safeOrderType}_{safeOrderNo}_{timestamp}.txt");

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

            var timestamp = DateTime.Now.ToString("HHmmss");
            var parsedLogPath = Path.Combine(parsedLogDir, $"hl7_parsed_{safeDispenseId}_{timestamp}.txt");

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

            // ทำความสะอาดทุกโฟลเดอร์ log
            CleanOldLogFolders(Path.Combine(appFolder, "hl7_raw"));
            CleanOldLogFolders(Path.Combine(appFolder, "hl7_parsed"));
            CleanOldLogFolders(Path.Combine(appFolder, "logreaderror"));

            Console.WriteLine($"Log cleanup completed. Retention period: {_logRetentionDays} days");
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
    }
}