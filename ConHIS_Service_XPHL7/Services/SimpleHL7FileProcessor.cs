using ConHIS_Service_XPHL7.Models;
using ConHIS_Service_XPHL7.Services;
using ConHIS_Service_XPHL7.Utils;
using ConHIS_Service_XPHL7.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ConHIS_Service_XPHL7.Services
{
    /// <summary>
    /// ประมวลผลไฟล์ HL7 โดยตรงโดยไม่ต้องผ่านฐานข้อมูล
    /// </summary>
    public class SimpleHL7FileProcessor
    {
        private readonly HL7Service _hl7Service;
        private readonly LogManager _logger;
        private readonly ApiService _apiService;

        public SimpleHL7FileProcessor(string apiEndpoint = null)
        {
            _hl7Service = new HL7Service();
            _logger = new LogManager();

            if (!string.IsNullOrEmpty(apiEndpoint))
            {
                _apiService = new ApiService(apiEndpoint);
            }
        }

        /// <summary>
        /// ประมวลผลไฟล์ HL7 เดี่ยว
        /// </summary>
        /// <param name="filePath">เส้นทางไฟล์ HL7</param>
        /// <returns>HL7Message object หรือ null หากเกิดข้อผิดพลาด</returns>
        public HL7Message ProcessHL7File(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            _logger.LogInfo($"Starting to process HL7 file: {filePath}");

            try
            {
                // ตรวจสอบว่าไฟล์มีอยู่หรือไม่
                if (!File.Exists(filePath))
                {
                    var errorMsg = $"HL7 file not found: {filePath}";
                    _logger.LogError(errorMsg);
                    _logger.LogReadError(fileName, errorMsg);
                    return null;
                }

                // อ่านข้อมูลจากไฟล์
                var hl7RawData = File.ReadAllText(filePath, Encoding.UTF8);
                _logger.LogInfo($"Successfully read HL7 file: {filePath}, Length: {hl7RawData.Length} characters");

                // บันทึก raw data
                _logger.LogRawHL7Data(fileName, "FILE_PROCESS", hl7RawData);

                // แปลงข้อมูล HL7
                var parsedMessage = _hl7Service.ParseHL7Message(hl7RawData);
                _logger.LogInfo($"Successfully parsed HL7 message for file: {fileName}");

                // บันทึก parsed data
                _logger.LogParsedHL7Data(fileName, parsedMessage);

                return parsedMessage;
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error processing HL7 file {filePath}: {ex.Message}";
                _logger.LogError(errorMsg, ex);
                _logger.LogReadError(fileName, errorMsg);
                return null;
            }
        }

        /// <summary>
        /// ประมวลผลไฟล์ HL7 หลายไฟล์ในโฟลเดอร์
        /// </summary>
        /// <param name="folderPath">เส้นทางโฟลเดอร์</param>
        /// <param name="filePattern">รูปแบบไฟล์ (เช่น "*.txt" หรือ "HL7-*.txt")</param>
        /// <returns>รายการผลลัพธ์การประมวลผล</returns>
        public List<HL7ProcessResult> ProcessHL7FilesInFolder(string folderPath, string filePattern = "*.txt")
        {
            _logger.LogInfo($"Starting to process HL7 files in folder: {folderPath} with pattern: {filePattern}");

            var results = new List<HL7ProcessResult>();

            try
            {
                if (!Directory.Exists(folderPath))
                {
                    _logger.LogError($"Folder not found: {folderPath}");
                    return results;
                }

                var files = Directory.GetFiles(folderPath, filePattern);
                _logger.LogInfo($"Found {files.Length} files matching pattern {filePattern}");

                foreach (var file in files)
                {
                    var result = new HL7ProcessResult
                    {
                        FilePath = file,
                        FileName = Path.GetFileName(file),
                        ProcessedAt = DateTime.Now
                    };

                    try
                    {
                        result.ParsedMessage = ProcessHL7File(file);
                        result.IsSuccess = result.ParsedMessage != null;

                        if (result.IsSuccess)
                        {
                            _logger.LogInfo($"Successfully processed file: {result.FileName}");
                        }
                        else
                        {
                            result.ErrorMessage = "Failed to parse HL7 message";
                            _logger.LogWarning($"Failed to process file: {result.FileName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.IsSuccess = false;
                        result.ErrorMessage = ex.Message;
                        _logger.LogError($"Error processing file {result.FileName}: {ex.Message}", ex);
                    }

                    results.Add(result);
                }

                var successCount = results.Count(r => r.IsSuccess);
                _logger.LogInfo($"Processed {results.Count} files. Success: {successCount}, Failed: {results.Count - successCount}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing HL7 files in folder {folderPath}: {ex.Message}", ex);
            }

            return results;
        }

        /// <summary>
        /// ประมวลผลไฟล์ HL7 และส่งไปยัง API Middleware
        /// </summary>
        /// <param name="filePath">เส้นทางไฟล์ HL7</param>
        /// <returns>true หากประมวลผลและส่งสำเร็จ</returns>
        public bool ProcessAndSendHL7File(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            _logger.LogInfo($"Starting to process and send HL7 file: {filePath}");

            try
            {
                if (_apiService == null)
                {
                    _logger.LogError("API Service is not configured. Cannot send to middleware.");
                    return false;
                }

                var parsedMessage = ProcessHL7File(filePath);
                if (parsedMessage == null)
                {
                    _logger.LogError($"Failed to parse HL7 file: {filePath}");
                    return false;
                }

                // ส่งไปยัง Middleware
                var success = _apiService.SendToMiddleware(parsedMessage);
                if (success)
                {
                    _logger.LogInfo($"Successfully sent HL7 data to middleware for file: {fileName}");
                    return true;
                }
                else
                {
                    _logger.LogError($"Failed to send HL7 data to middleware for file: {fileName}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error processing and sending HL7 file {filePath}: {ex.Message}";
                _logger.LogError(errorMsg, ex);
                _logger.LogReadError(fileName, errorMsg);
                return false;
            }
        }

        /// <summary>
        /// ประมวลผลไฟล์ HL7 หลายไฟล์และส่งไปยัง API Middleware
        /// </summary>
        /// <param name="folderPath">เส้นทางโฟลเดอร์</param>
        /// <param name="filePattern">รูปแบบไฟล์</param>
        /// <returns>รายการผลลัพธ์การประมวลผลและการส่ง</returns>
        public List<HL7ProcessResult> ProcessAndSendHL7FilesInFolder(string folderPath, string filePattern = "*.txt")
        {
            _logger.LogInfo($"Starting to process and send HL7 files in folder: {folderPath}");

            var results = ProcessHL7FilesInFolder(folderPath, filePattern);

            if (_apiService == null)
            {
                _logger.LogError("API Service is not configured. Cannot send to middleware.");
                foreach (var result in results)
                {
                    if (result.IsSuccess)
                    {
                        result.IsSuccess = false;
                        result.ErrorMessage = "API Service not configured";
                    }
                }
                return results;
            }

            foreach (var result in results.Where(r => r.IsSuccess))
            {
                try
                {
                    var sendSuccess = _apiService.SendToMiddleware(result.ParsedMessage);
                    result.IsSentToMiddleware = sendSuccess;

                    if (sendSuccess)
                    {
                        _logger.LogInfo($"Successfully sent {result.FileName} to middleware");
                    }
                    else
                    {
                        _logger.LogError($"Failed to send {result.FileName} to middleware");
                    }
                }
                catch (Exception ex)
                {
                    result.IsSentToMiddleware = false;
                    result.ErrorMessage += $"; Send Error: {ex.Message}";
                    _logger.LogError($"Error sending {result.FileName} to middleware: {ex.Message}", ex);
                }
            }

            var sentCount = results.Count(r => r.IsSentToMiddleware);
            _logger.LogInfo($"Sent {sentCount} files to middleware successfully");

            return results;
        }



        /// <summary>
        /// อ่านและแสดงข้อมูล HL7 แบบ Raw
        /// </summary>
        /// <param name="filePath">เส้นทางไฟล์</param>
        /// <returns>ข้อมูล HL7 แบบ Raw</returns>
        public string ReadHL7RawData(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            _logger.LogInfo($"Reading raw HL7 data from: {filePath}");

            try
            {
                if (!File.Exists(filePath))
                {
                    var errorMsg = $"File not found: {filePath}";
                    _logger.LogError(errorMsg);
                    return null;
                }

                var rawData = File.ReadAllText(filePath, Encoding.UTF8);
                _logger.LogInfo($"Successfully read {rawData.Length} characters from {fileName}");

                return rawData;
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error reading file {filePath}: {ex.Message}";
                _logger.LogError(errorMsg, ex);
                _logger.LogReadError(fileName, errorMsg);
                return null;
            }
        }

        public void Dispose()
        {
            _apiService?.Dispose();
        }
    }

    /// <summary>
    /// คลาสสำหรับเก็บผลลัพธ์การประมวลผลไฟล์ HL7
    /// </summary>
    public class HL7ProcessResult
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public bool IsSuccess { get; set; }
        public bool IsSentToMiddleware { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime ProcessedAt { get; set; }
        public HL7Message ParsedMessage { get; set; }
    }
}