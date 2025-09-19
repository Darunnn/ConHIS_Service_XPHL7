using System;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json;
using ConHIS_Service_XPHL7.Configuration;

namespace ConHIS_Service_XPHL7.Utils
{
    public class LogManager
    {
        private string _logFolder;
        

        public string LogFolder => _logFolder;

        public LogManager(string logFolder = "log")
        {
            var appFolder = AppDomain.CurrentDomain.BaseDirectory ?? Environment.CurrentDirectory;
            var desired = Path.Combine(appFolder, logFolder);
            Directory.CreateDirectory(desired);
            _logFolder = desired;
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

        // Method to log raw HL7 data to a separate folder (hl7_raw)
        public void LogRawHL7Data(string DrugDispenseipdId, string RecieveOrderType, string hl7Data, string rawLogFolder = "hl7_raw")
        {
            var appFolder = AppDomain.CurrentDomain.BaseDirectory ?? Environment.CurrentDirectory;
            var rawLogDir = Path.Combine(appFolder, rawLogFolder);
            Directory.CreateDirectory(rawLogDir);
            var rawLogPath = Path.Combine(rawLogDir, $"hl7_data_raw_{DrugDispenseipdId}_{RecieveOrderType}.txt");
            try
            {
                File.WriteAllText(rawLogPath, hl7Data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write HL7 raw data file for DrugDispenseipdId {DrugDispenseipdId}: {ex}");
            }
        }

        // Method to log parsed HL7 data to a separate folder (hl7_parsed)
        public void LogParsedHL7Data(string DrugDispenseipdId,  object parsedData, string parsedLogFolder = "hl7_parsed")
        {
            var appFolder = AppDomain.CurrentDomain.BaseDirectory ?? Environment.CurrentDirectory;
            var parsedLogDir = Path.Combine(appFolder, parsedLogFolder);
            Directory.CreateDirectory(parsedLogDir);
            var parsedLogPath = Path.Combine(parsedLogDir, $"hl7_data_parsed_{DrugDispenseipdId}.txt");
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

       

        // Method to log HL7 read/parse errors to a separate folder (logreaderror)
        public void LogReadError(string DrugDispenseipdId, string errorMessage, string errorLogFolder = "logreaderror")
        {
            var appFolder = AppDomain.CurrentDomain.BaseDirectory ?? Environment.CurrentDirectory;
            var errorLogDir = Path.Combine(appFolder, errorLogFolder);
            Directory.CreateDirectory(errorLogDir);
            var errorLogPath = Path.Combine(errorLogDir, $"hl7_read_error_{DrugDispenseipdId}.txt");
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

        public void LogError(string message, Exception ex = null)
        {
            var fullMessage = ex != null ? $"{message} - Exception: {ex}" : message;
            LogToFile(fullMessage, "ERROR");
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