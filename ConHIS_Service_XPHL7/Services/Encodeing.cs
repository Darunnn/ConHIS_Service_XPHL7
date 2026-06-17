using System;
using System.IO;
using System.Text;

namespace ConHIS_Service_XPHL7.Services
{
    public class EncodingService
    {
        private readonly string _selectedEncoding;
        private readonly string _ipdEncoding;  // ⭐ เพิ่ม encoding สำหรับ IPD
        private readonly Action<string> _logger;

        public EncodingService(string selectedEncoding, Action<string> logger = null, string ipdEncoding = null)
        {
            _selectedEncoding = selectedEncoding?.ToUpper() ?? "UTF-8";
            _ipdEncoding = ipdEncoding?.ToUpper() ?? _selectedEncoding; // ⭐ fallback ใช้ OPD ถ้าไม่มี
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════════
        //  สร้างจาก config file — อ่านทั้ง OPD และ IPD
        // ════════════════════════════════════════════════════════════════════

        public static EncodingService FromConnectionConfig(Action<string> logger = null)
        {
            const string ConnFolder = "Connection";
            const string ConnFile = "connectdatabase.ini";
            const string ConnFileIPD = "connectdatabase_ipd.ini"; // ⭐

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // ── OPD ──────────────────────────────────────────────────────
            string opdEncoding = ReadCharsetFromFile(
                Path.Combine(baseDir, ConnFolder, ConnFile), logger);

            // ── IPD ──────────────────────────────────────────────────────
            string ipdEncoding = ReadCharsetFromFile(
                Path.Combine(baseDir, ConnFolder, ConnFileIPD), logger);

            // ถ้าไม่มีไฟล์ IPD ให้ fallback ใช้ค่า OPD
            if (string.IsNullOrEmpty(ipdEncoding))
            {
                ipdEncoding = opdEncoding;
                logger?.Invoke($"IPD config not found, using OPD encoding: {opdEncoding}");
            }

            logger?.Invoke($"Encoding initialized — OPD: {opdEncoding}, IPD: {ipdEncoding}");
            return new EncodingService(opdEncoding, logger, ipdEncoding);
        }

        // ── helper อ่าน Charset= จากไฟล์ ────────────────────────────────
        private static string ReadCharsetFromFile(string filePath, Action<string> logger)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    logger?.Invoke($"Config file not found: {filePath}");
                    return "UTF-8";
                }

                foreach (var line in File.ReadAllLines(filePath))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (line.StartsWith("Charset=", StringComparison.OrdinalIgnoreCase))
                    {
                        var charset = line.Replace("Charset=", "").Trim().TrimEnd(';').ToUpper();
                        logger?.Invoke($"Loaded charset '{charset}' from: {Path.GetFileName(filePath)}");
                        return NormalizeCharset(charset);
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.Invoke($"Error reading config '{filePath}': {ex.Message}");
            }

            return "UTF-8";
        }

        // ── แปลง charset name ให้เป็น standard ──────────────────────────
        private static string NormalizeCharset(string charset)
        {
            if (charset == "TIS620" || charset == "TIS-620")
                return "TIS-620";
            return "UTF-8";
        }

        // ════════════════════════════════════════════════════════════════════
        //  Decode — เลือก encoding ตาม orderType
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Decode HL7 data โดยเลือก encoding ตาม orderType (IPD/OPD)
        /// </summary>
        public string DecodeHl7Data(byte[] hl7Data, string orderType = null)
        {
            if (hl7Data == null || hl7Data.Length == 0)
            {
                _logger?.Invoke("HL7 data is null or empty");
                return string.Empty;
            }

            // ⭐ เลือก encoding ตาม orderType
            string encoding = (orderType?.ToUpper() == "IPD") ? _ipdEncoding : _selectedEncoding;

            return DecodeWithEncoding(hl7Data, encoding);
        }

        // ── decode ด้วย encoding ที่กำหนด ───────────────────────────────
        private string DecodeWithEncoding(byte[] hl7Data, string encoding)
        {
            try
            {
                if (encoding == "TIS-620" || encoding == "TIS620")
                {
                    try
                    {
                        Encoding tis620 = Encoding.GetEncoding("TIS-620");
                        return tis620.GetString(hl7Data);
                    }
                    catch (Exception ex)
                    {
                        _logger?.Invoke($"TIS-620 decode failed: {ex.Message}, falling back to UTF-8");
                        return Encoding.UTF8.GetString(hl7Data);
                    }
                }
                else
                {
                    return Encoding.UTF8.GetString(hl7Data);
                }
            }
            catch (Exception ex)
            {
                _logger?.Invoke($"Critical error decoding HL7: {ex.Message}");
                return Encoding.Default.GetString(hl7Data);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  Properties
        // ════════════════════════════════════════════════════════════════════

        public string CurrentEncoding => _selectedEncoding;
        public string CurrentIPDEncoding => _ipdEncoding;  // ⭐

        // ── static helper (backward compatible) ─────────────────────────
        public static string GetEncodingFromConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return "UTF-8";

            try
            {
                foreach (var part in connectionString.Split(';'))
                {
                    var trimmed = part.Trim();
                    if (trimmed.StartsWith("Charset=", StringComparison.OrdinalIgnoreCase))
                    {
                        var charset = trimmed.Split('=')[1].Trim().ToUpper();
                        return NormalizeCharset(charset);
                    }
                }
            }
            catch { }

            return "UTF-8";
        }
    }
}