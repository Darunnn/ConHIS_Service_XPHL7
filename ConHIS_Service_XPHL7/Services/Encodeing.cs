using System;
using System.IO;
using System.Text;

namespace ConHIS_Service_XPHL7.Services
{
    public class EncodingService
    {
        private readonly string _selectedEncoding;
        private readonly Action<string> _logger;

        /// <summary>
        /// Initialize EncodingService with selected encoding type
        /// </summary>
        /// <param name="selectedEncoding">Encoding type: "UTF-8" or "TIS-620"</param>
        /// <param name="logger">Optional logger action for warnings/errors</param>
        public EncodingService(string selectedEncoding, Action<string> logger = null)
        {
            _selectedEncoding = selectedEncoding?.ToUpper() ?? "UTF-8";
            _logger = logger;
        }

        /// <summary>
        /// Create EncodingService from connection configuration file
        /// </summary>
        /// <param name="logger">Optional logger action</param>
        /// <returns>EncodingService instance</returns>
        public static EncodingService FromConnectionConfig(Action<string> logger = null)
        {
            const string ConnFolder = "Connection";
            const string ConnFile = "connectdatabase.ini";

            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConnFolder, ConnFile);

            if (File.Exists(path))
            {
                var lines = File.ReadAllLines(path);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (line.StartsWith("Charset=", StringComparison.OrdinalIgnoreCase))
                    {
                        var charset = line.Replace("Charset=", "").Trim().TrimEnd(';').ToUpper();
                        logger?.Invoke($"Loaded encoding from config: {charset}");
                        return new EncodingService(charset, logger);
                    }
                }
            }

            logger?.Invoke("No charset found in config, using default UTF-8");
            return new EncodingService("UTF-8", logger);
        }

        /// <summary>
        /// Decode HL7 byte data based on selected encoding
        /// </summary>
        /// <param name="hl7Data">Raw byte array from database</param>
        /// <returns>Decoded HL7 string</returns>
        public string DecodeHl7Data(byte[] hl7Data)
        {
            if (hl7Data == null || hl7Data.Length == 0)
            {
                _logger?.Invoke("HL7 data is null or empty");
                return string.Empty;
            }

            string hl7String = string.Empty;

            try
            {
                // Check selected encoding and decode accordingly
                if (_selectedEncoding == "TIS-620" || _selectedEncoding == "TIS620")
                {
                    // Try TIS-620 encoding first
                    try
                    {
                        Encoding tis620 = Encoding.GetEncoding("TIS-620");
                        hl7String = tis620.GetString(hl7Data);
                        _logger?.Invoke($"Successfully decoded HL7 data with TIS-620 encoding ({hl7Data.Length} bytes)");
                    }
                    catch (Exception ex)
                    {
                        _logger?.Invoke($"Failed to decode with TIS-620: {ex.Message}. Falling back to UTF-8.");
                        hl7String = Encoding.UTF8.GetString(hl7Data);
                    }
                }
                else
                {
                    // Use UTF-8 encoding
                    hl7String = Encoding.UTF8.GetString(hl7Data);
                    _logger?.Invoke($"Successfully decoded HL7 data with UTF-8 encoding ({hl7Data.Length} bytes)");
                }
            }
            catch (Exception ex)
            {
                _logger?.Invoke($"Critical error decoding HL7 data: {ex.Message}");
                // Last resort: try default encoding
                hl7String = Encoding.Default.GetString(hl7Data);
            }

            return hl7String;
        }

        /// <summary>
        /// Load encoding setting from connection string
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        /// <returns>Encoding type (UTF-8 or TIS-620)</returns>
        public static string GetEncodingFromConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return "UTF-8";

            try
            {
                var parts = connectionString.Split(';');
                foreach (var part in parts)
                {
                    var trimmedPart = part.Trim();
                    if (trimmedPart.StartsWith("Charset=", StringComparison.OrdinalIgnoreCase))
                    {
                        var charset = trimmedPart.Split('=')[1].Trim().ToUpper();

                        if (charset == "TIS620" || charset == "TIS-620")
                            return "TIS-620";
                        else
                            return "UTF-8";
                    }
                }
            }
            catch
            {
                // If parsing fails, return default
            }

            return "UTF-8"; // Default
        }

        /// <summary>
        /// Get current encoding being used
        /// </summary>
        public string CurrentEncoding => _selectedEncoding;
    }
}