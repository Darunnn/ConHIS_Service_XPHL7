using ConHIS_Service_XPHL7.Models;
using ConHIS_Service_XPHL7.Utils;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ConHIS_Service_XPHL7.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly string _ipdConnectionString;
        private readonly LogManager _logger = new LogManager();

        public DatabaseService(string connectionString, string ipdConnectionString = null)
        {
            _connectionString = connectionString;
            _ipdConnectionString = ipdConnectionString ?? connectionString;
        }

        // ════════════════════════════════════════════════════════════════════
        //  HELPERS — อ่านค่าจาก reader อย่างปลอดภัย ไม่ว่า DB จะ return type อะไร
        // ════════════════════════════════════════════════════════════════════

        private char ReadStatusChar(MySqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ordinal)) return 'N';

                object raw = reader.GetValue(ordinal);
                if (raw is string s && s.Length > 0) return s[0];
                if (raw is char c) return c;
                if (raw is int i) return (char)i;
                if (raw is long l) return (char)l;
                if (raw is byte b) return (char)b;

                string str = Convert.ToString(raw);
                return str?.Length > 0 ? str[0] : 'N';
            }
            catch { return 'N'; }
        }

        private string SafeGetString(MySqlDataReader reader, string columnName, string fallback = "")
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ordinal)) return fallback;
                return Convert.ToString(reader.GetValue(ordinal)) ?? fallback;
            }
            catch { return fallback; }
        }

        private int SafeGetInt(MySqlDataReader reader, string columnName, int fallback = 0)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ordinal)) return fallback;
                return Convert.ToInt32(reader.GetValue(ordinal));
            }
            catch { return fallback; }
        }

        private DateTime SafeGetDateTime(MySqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ordinal)) return DateTime.Now;
                return reader.GetDateTime(ordinal);
            }
            catch { return DateTime.Now; }
        }

        private DateTime? SafeGetDateTimeNullable(MySqlDataReader reader, int ordinal)
        {
            try
            {
                return reader.IsDBNull(ordinal) ? (DateTime?)null : reader.GetDateTime(ordinal);
            }
            catch { return null; }
        }

        // ════════════════════════════════════════════════════════════════════
        //  CONNECTION TEST
        // ════════════════════════════════════════════════════════════════════

        public bool TestConnection()
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Connection test failed (OPD): {ex.Message}");
                return false;
            }
        }

        public bool TestIPDConnection()
        {
            try
            {
                using (var conn = new MySqlConnection(_ipdConnectionString))
                {
                    conn.Open();
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Connection test failed (IPD): {ex.Message}");
                return false;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  CHECK TABLE EXISTS
        // ════════════════════════════════════════════════════════════════════

        public bool CheckTableExists(string tableName)
        {
            bool isIpd = tableName.IndexOf("_ipd", StringComparison.OrdinalIgnoreCase) >= 0;
            return CheckTableExistsOnConnection(tableName, isIpd ? _ipdConnectionString : _connectionString);
        }

        private bool CheckTableExistsOnConnection(string tableName, string connStr)
        {
            try
            {
                using (var connection = new MySqlConnection(connStr))
                {
                    connection.Open();

                    string query = @"
                        SELECT COUNT(*) 
                        FROM information_schema.TABLES 
                        WHERE TABLE_SCHEMA = DATABASE() 
                        AND TABLE_NAME = @TableName";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TableName", tableName);
                        var count = Convert.ToInt32(command.ExecuteScalar());
                        _logger.LogInfo($"CheckTableExists '{tableName}': count={count}");
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error checking table '{tableName}': {ex.Message}");
                return false;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  IPD METHODS
        // ════════════════════════════════════════════════════════════════════

        #region IPD Methods

        public List<DrugDispenseipd> GetPendingDispenseData()
        {
            var result = new List<DrugDispenseipd>();
            _logger.LogInfo("GetPendingDispenseData (IPD): Start");

            try
            {
                using (var conn = new MySqlConnection(_ipdConnectionString))
                {
                    conn.Open();
                    _logger.LogInfo("Database connection opened for GetPendingDispenseData (IPD)");

                    var sql = @"SELECT drug_dispense_ipd_id, presc_id, drug_request_msg_type, 
                               hl7_data, drug_dispense_datetime, recieve_status, recieve_status_datetime, recieve_order_type
                               FROM drug_dispense_ipd 
                               WHERE recieve_status = 'N'
                               ORDER BY drug_dispense_datetime
                               LIMIT 500";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.CommandTimeout = 30;

                        using (var reader = cmd.ExecuteReader())
                        {
                            int recieveStatusDatetimeOrdinal = reader.GetOrdinal("recieve_status_datetime");
                            int recieveOrderTypeOrdinal = reader.GetOrdinal("recieve_order_type");

                            while (reader.Read())
                            {
                                try
                                {
                                    // ⭐ ใช้ SafeGetString แทน GetString ทุกตัว
                                    var prescId = SafeGetString(reader, "presc_id");
                                    var msgType = SafeGetString(reader, "drug_request_msg_type");
                                    var orderType = reader.IsDBNull(recieveOrderTypeOrdinal)
                                                    ? null
                                                    : SafeGetString(reader, "recieve_order_type");
                                    char status = ReadStatusChar(reader, "recieve_status");

                                    result.Add(new DrugDispenseipd
                                    {
                                        DrugDispenseipdId = SafeGetInt(reader, "drug_dispense_ipd_id"),
                                        PrescId = prescId,
                                        DrugRequestMsgType = msgType,
                                        Hl7Data = reader["hl7_data"] as byte[],
                                        DrugDispenseDatetime = SafeGetDateTime(reader, "drug_dispense_datetime"),
                                        RecieveStatus = status,
                                        RecieveStatusDatetime = SafeGetDateTimeNullable(reader, recieveStatusDatetimeOrdinal),
                                        RecieveOrderType = orderType
                                    });

                                    _logger.LogInfo(
                                        $"GetPendingDispenseData (IPD): PrescId={prescId}, " +
                                        $"Status={status}, OrderType={orderType}");
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning($"Error reading pending row (IPD): {ex.Message}");
                                }
                            }

                            _logger.LogInfo($"GetPendingDispenseData (IPD): Found {result.Count} rows");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting pending dispense data (IPD)", ex);
            }

            return result;
        }

        public void UpdateReceiveStatus(int drugDispenseipdId, char status)
        {
            _logger.LogInfo($"UpdateReceiveStatus (IPD): ID={drugDispenseipdId}, status={status}");

            try
            {
                using (var conn = new MySqlConnection(_ipdConnectionString))
                {
                    conn.Open();

                    var sql = @"UPDATE drug_dispense_ipd 
                               SET recieve_status = @status, recieve_status_datetime = @datetime
                               WHERE drug_dispense_ipd_id = @id";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@status", status.ToString());
                        cmd.Parameters.AddWithValue("@datetime", DateTime.Now);
                        cmd.Parameters.AddWithValue("@id", drugDispenseipdId);
                        cmd.ExecuteNonQuery();
                        _logger.LogInfo($"UpdateReceiveStatus (IPD): Updated ID={drugDispenseipdId} to status={status}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating receive status (IPD) for ID={drugDispenseipdId}", ex);
            }
        }

        public void UpdateReceiveIPDStatusToPreview(int drugDispenseipdId)
        {
            _logger.LogInfo($"UpdateReceiveIPDStatusToPreview: ID={drugDispenseipdId} -> 'P'");

            try
            {
                using (var conn = new MySqlConnection(_ipdConnectionString))
                {
                    conn.Open();

                    var sql = @"UPDATE drug_dispense_ipd 
                               SET recieve_status = 'P', recieve_status_datetime = @datetime
                               WHERE drug_dispense_ipd_id = @id";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@datetime", DateTime.Now);
                        cmd.Parameters.AddWithValue("@id", drugDispenseipdId);
                        cmd.ExecuteNonQuery();
                        _logger.LogInfo($"UpdateReceiveIPDStatusToPreview: Updated ID={drugDispenseipdId} to 'P'");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating IPD status to Preview for ID={drugDispenseipdId}", ex);
            }
        }

        #endregion

        // ════════════════════════════════════════════════════════════════════
        //  OPD METHODS
        // ════════════════════════════════════════════════════════════════════

        #region OPD Methods

        public List<DrugDispenseopd> GetPendingDispenseOpdData()
        {
            var result = new List<DrugDispenseopd>();
            _logger.LogInfo("[GetPendingDispenseData OPD] Start");

            try
            {
                if (!CheckTableExistsOnConnection("drug_dispense_opd", _connectionString))
                {
                    _logger.LogWarning("[GetPendingDispenseData OPD] Table 'drug_dispense_opd' does not exist");
                    return result;
                }

                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    _logger.LogInfo("[GetPendingDispenseData OPD] Database connection opened");

                    var sql = @"SELECT drug_dispense_opd_id, presc_id, drug_request_msg_type, 
                       hl7_data, drug_dispense_datetime, recieve_status, recieve_status_datetime, recieve_order_type
                       FROM drug_dispense_opd 
                       WHERE recieve_status = 'N'
                       ORDER BY drug_dispense_datetime
                       LIMIT 500";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.CommandTimeout = 30;

                        using (var reader = cmd.ExecuteReader())
                        {
                            int recieveStatusDatetimeOrdinal = reader.GetOrdinal("recieve_status_datetime");
                            int recieveOrderTypeOrdinal = reader.GetOrdinal("recieve_order_type");

                            int rowCount = 0;
                            while (reader.Read())
                            {
                                try
                                {
                                    rowCount++;

                                    // ⭐ ใช้ SafeGetString แทน GetString ทุกตัว
                                    var prescId = SafeGetString(reader, "presc_id");
                                    var msgType = SafeGetString(reader, "drug_request_msg_type");
                                    var orderType = reader.IsDBNull(recieveOrderTypeOrdinal)
                                                    ? null
                                                    : SafeGetString(reader, "recieve_order_type");
                                    char status = ReadStatusChar(reader, "recieve_status");

                                    result.Add(new DrugDispenseopd
                                    {
                                        DrugDispenseopdId = SafeGetInt(reader, "drug_dispense_opd_id"),
                                        PrescId = prescId,
                                        DrugRequestMsgType = msgType,
                                        Hl7Data = reader["hl7_data"] as byte[],
                                        DrugDispenseDatetime = SafeGetDateTime(reader, "drug_dispense_datetime"),
                                        RecieveStatus = status,
                                        RecieveStatusDatetime = SafeGetDateTimeNullable(reader, recieveStatusDatetimeOrdinal),
                                        RecieveOrderType = orderType
                                    });

                                    if (rowCount % 10 == 0)
                                        _logger.LogInfo($"[GetPendingDispenseData OPD] Progress: {rowCount} rows...");
                                }
                                catch (Exception rowEx)
                                {
                                    _logger.LogError(
                                        $"[GetPendingDispenseData OPD] Error reading row #{rowCount}: {rowEx.Message}", rowEx);
                                }
                            }

                            _logger.LogInfo(
                                $"[GetPendingDispenseData OPD] Completed - Found {result.Count} rows (read: {rowCount})");
                        }
                    }
                }
            }
            catch (MySqlException mysqlEx)
            {
                _logger.LogError(
                    $"[GetPendingDispenseData OPD] MySQL Error (Code: {mysqlEx.Number}): {mysqlEx.Message}", mysqlEx);
            }
            catch (Exception ex)
            {
                _logger.LogError("[GetPendingDispenseData OPD] Critical error", ex);
            }

            return result;
        }

        public void UpdateReceiveOpdStatus(int drugDispenseopdId, char status)
        {
            _logger.LogInfo($"UpdateReceiveStatus (OPD): ID={drugDispenseopdId}, status={status}");

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    var sql = @"UPDATE drug_dispense_opd 
                               SET recieve_status = @status, recieve_status_datetime = @datetime
                               WHERE drug_dispense_opd_id = @id";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@status", status.ToString());
                        cmd.Parameters.AddWithValue("@datetime", DateTime.Now);
                        cmd.Parameters.AddWithValue("@id", drugDispenseopdId);
                        cmd.ExecuteNonQuery();
                        _logger.LogInfo($"UpdateReceiveStatus (OPD): Updated ID={drugDispenseopdId} to status={status}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating receive status (OPD) for ID={drugDispenseopdId}", ex);
            }
        }

        #endregion

        // ════════════════════════════════════════════════════════════════════
        //  SHARED METHODS
        // ════════════════════════════════════════════════════════════════════

        #region Shared Methods

        public List<DrugDispenseipd> GetPendingDispenseDataByOrderType(string orderType)
        {
            _logger.LogInfo($"[GetPendingDispenseDataByOrderType] Start - OrderType={orderType}");

            try
            {
                if (string.IsNullOrWhiteSpace(orderType))
                {
                    _logger.LogError("[GetPendingDispenseDataByOrderType] OrderType is null or empty");
                    return new List<DrugDispenseipd>();
                }

                string tableName = orderType == "IPD" ? "drug_dispense_ipd" : "drug_dispense_opd";
                string connStr = orderType == "IPD" ? _ipdConnectionString : _connectionString;

                if (!CheckTableExistsOnConnection(tableName, connStr))
                {
                    _logger.LogWarning($"[GetPendingDispenseDataByOrderType] Table '{tableName}' does not exist");
                    return new List<DrugDispenseipd>();
                }

                if (orderType == "IPD")
                {
                    var result = GetPendingDispenseData();
                    _logger.LogInfo($"[GetPendingDispenseDataByOrderType] IPD returned {result.Count} records");
                    return result;
                }
                else if (orderType == "OPD")
                {
                    var opdData = GetPendingDispenseOpdData();
                    _logger.LogInfo($"[GetPendingDispenseDataByOrderType] OPD returned {opdData.Count} records");

                    var result = opdData.Select(opd => new DrugDispenseipd
                    {
                        DrugDispenseipdId = opd.DrugDispenseopdId,
                        PrescId = opd.PrescId,
                        DrugRequestMsgType = opd.DrugRequestMsgType,
                        Hl7Data = opd.Hl7Data,
                        DrugDispenseDatetime = opd.DrugDispenseDatetime,
                        RecieveStatus = opd.RecieveStatus,
                        RecieveStatusDatetime = opd.RecieveStatusDatetime,
                        RecieveOrderType = opd.RecieveOrderType
                    }).ToList();

                    return result;
                }

                _logger.LogWarning($"[GetPendingDispenseDataByOrderType] Unknown OrderType: {orderType}");
                return new List<DrugDispenseipd>();
            }
            catch (Exception ex)
            {
                _logger.LogError("[GetPendingDispenseDataByOrderType] Critical error", ex);
                return new List<DrugDispenseipd>();
            }
        }

        // ── GetAllDispenseDataByDate ─────────────────────────────────────

        public List<DrugDispenseipd> GetAllDispenseDataByDate(
            DateTime date, bool includeIPD = true, bool includeOPD = true)
        {
            var result = new List<DrugDispenseipd>();

            try
            {
                // ── IPD ──────────────────────────────────────────────────
                if (includeIPD && CheckTableExistsOnConnection("drug_dispense_ipd", _ipdConnectionString))
                {
                    try
                    {
                        using (var conn = new MySqlConnection(_ipdConnectionString))
                        {
                            conn.Open();

                            string sql = @"
                                SELECT 
                                    drug_dispense_ipd_id    AS DrugDispenseipdId,
                                    drug_dispense_datetime  AS DrugDispenseDatetime,
                                    hl7_data                AS Hl7Data,
                                    recieve_status          AS RecieveStatus,
                                    recieve_status_datetime AS RecieveStatusDatetime,
                                    'IPD'                   AS RecieveOrderType
                                FROM drug_dispense_ipd
                                WHERE (DATE(recieve_status_datetime) = @Date
                                       OR DATE(drug_dispense_datetime) = @Date)
                                  AND recieve_status IN ('Y', 'F', 'P')
                                ORDER BY drug_dispense_datetime DESC";

                            using (var cmd = new MySqlCommand(sql, conn))
                            {
                                cmd.Parameters.AddWithValue("@Date", date.Date);
                                using (var reader = cmd.ExecuteReader())
                                {
                                    int before = result.Count;
                                    while (reader.Read())
                                    {
                                        var row = ReadDrugDispenseFromReader(reader);
                                        if (row != null) result.Add(row);
                                    }
                                    _logger.LogInfo($"[IPD] Retrieved {result.Count - before} records for {date:yyyy-MM-dd}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error querying IPD table: {ex.Message}", ex);
                    }
                }

                // ── OPD ──────────────────────────────────────────────────
                if (includeOPD && CheckTableExistsOnConnection("drug_dispense_opd", _connectionString))
                {
                    try
                    {
                        using (var conn = new MySqlConnection(_connectionString))
                        {
                            conn.Open();

                            string sql = @"
                                SELECT 
                                    drug_dispense_opd_id    AS DrugDispenseipdId,
                                    drug_dispense_datetime  AS DrugDispenseDatetime,
                                    hl7_data                AS Hl7Data,
                                    recieve_status          AS RecieveStatus,
                                    recieve_status_datetime AS RecieveStatusDatetime,
                                    'OPD'                   AS RecieveOrderType
                                FROM drug_dispense_opd
                                WHERE (DATE(recieve_status_datetime) = @Date
                                       OR DATE(drug_dispense_datetime) = @Date)
                                  AND recieve_status IN ('Y', 'F')
                                ORDER BY drug_dispense_datetime DESC";

                            using (var cmd = new MySqlCommand(sql, conn))
                            {
                                cmd.Parameters.AddWithValue("@Date", date.Date);
                                using (var reader = cmd.ExecuteReader())
                                {
                                    int before = result.Count;
                                    while (reader.Read())
                                    {
                                        var row = ReadDrugDispenseFromReader(reader);
                                        if (row != null) result.Add(row);
                                    }
                                    _logger.LogInfo($"[OPD] Retrieved {result.Count - before} records for {date:yyyy-MM-dd}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error querying OPD table: {ex.Message}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetAllDispenseDataByDate: {ex.Message}", ex);
            }

            _logger.LogInfo($"[Total] {result.Count} records for {date:yyyy-MM-dd}");
            return result;
        }

        // ── GetAllDispenseDataByDateAndSearch ────────────────────────────

        public List<DrugDispenseipd> GetAllDispenseDataByDateAndSearch(
            DateTime date, string searchText, bool includeIPD = true, bool includeOPD = true)
        {
            var result = new List<DrugDispenseipd>();

            try
            {
                // ── IPD ──────────────────────────────────────────────────
                if (includeIPD && CheckTableExistsOnConnection("drug_dispense_ipd", _ipdConnectionString))
                {
                    try
                    {
                        using (var conn = new MySqlConnection(_ipdConnectionString))
                        {
                            conn.Open();

                            string sql = @"
                                SELECT 
                                    drug_dispense_ipd_id    AS DrugDispenseipdId,
                                    drug_dispense_datetime  AS DrugDispenseDatetime,
                                    hl7_data                AS Hl7Data,
                                    recieve_status          AS RecieveStatus,
                                    recieve_status_datetime AS RecieveStatusDatetime,
                                    'IPD'                   AS RecieveOrderType
                                FROM drug_dispense_ipd
                                WHERE (DATE(recieve_status_datetime) = @Date
                                       OR DATE(drug_dispense_datetime) = @Date)
                                  AND recieve_status IN ('Y', 'F', 'P')
                                  AND CAST(hl7_data AS CHAR) LIKE @SearchText
                                ORDER BY drug_dispense_datetime DESC";

                            using (var cmd = new MySqlCommand(sql, conn))
                            {
                                cmd.Parameters.AddWithValue("@Date", date.Date);
                                cmd.Parameters.AddWithValue("@SearchText", $"%{searchText}%");
                                using (var reader = cmd.ExecuteReader())
                                {
                                    int before = result.Count;
                                    while (reader.Read())
                                    {
                                        var row = ReadDrugDispenseFromReader(reader);
                                        if (row != null) result.Add(row);
                                    }
                                    _logger.LogInfo($"[IPD Search] Found {result.Count - before} records");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error searching IPD table: {ex.Message}", ex);
                    }
                }

                // ── OPD ──────────────────────────────────────────────────
                if (includeOPD && CheckTableExistsOnConnection("drug_dispense_opd", _connectionString))
                {
                    try
                    {
                        using (var conn = new MySqlConnection(_connectionString))
                        {
                            conn.Open();

                            string sql = @"
                                SELECT 
                                    drug_dispense_opd_id    AS DrugDispenseipdId,
                                    drug_dispense_datetime  AS DrugDispenseDatetime,
                                    hl7_data                AS Hl7Data,
                                    recieve_status          AS RecieveStatus,
                                    recieve_status_datetime AS RecieveStatusDatetime,
                                    'OPD'                   AS RecieveOrderType
                                FROM drug_dispense_opd
                                WHERE (DATE(recieve_status_datetime) = @Date
                                       OR DATE(drug_dispense_datetime) = @Date)
                                  AND recieve_status IN ('Y', 'F')
                                  AND CAST(hl7_data AS CHAR) LIKE @SearchText
                                ORDER BY drug_dispense_datetime DESC";

                            using (var cmd = new MySqlCommand(sql, conn))
                            {
                                cmd.Parameters.AddWithValue("@Date", date.Date);
                                cmd.Parameters.AddWithValue("@SearchText", $"%{searchText}%");
                                using (var reader = cmd.ExecuteReader())
                                {
                                    int before = result.Count;
                                    while (reader.Read())
                                    {
                                        var row = ReadDrugDispenseFromReader(reader);
                                        if (row != null) result.Add(row);
                                    }
                                    _logger.LogInfo($"[OPD Search] Found {result.Count - before} records");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error searching OPD table: {ex.Message}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetAllDispenseDataByDateAndSearch: {ex.Message}", ex);
            }

            _logger.LogInfo($"[Total Search] {result.Count} records for '{searchText}' on {date:yyyy-MM-dd}");
            return result;
        }

        // ════════════════════════════════════════════════════════════════════
        //  READ HELPER
        // ════════════════════════════════════════════════════════════════════

        private DrugDispenseipd ReadDrugDispenseFromReader(MySqlDataReader reader)
        {
            try
            {
                bool HasColumn(string name)
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                        if (reader.GetName(i).Equals(name, StringComparison.OrdinalIgnoreCase))
                            return true;
                    return false;
                }

                // ── ID ──────────────────────────────────────────────────────
                int id = HasColumn("DrugDispenseipdId") ? SafeGetInt(reader, "DrugDispenseipdId")
                       : HasColumn("drug_dispense_ipd_id") ? SafeGetInt(reader, "drug_dispense_ipd_id")
                       : SafeGetInt(reader, "drug_dispense_opd_id");

                // ── DrugDispenseDatetime ────────────────────────────────────
                DateTime dispDt = HasColumn("DrugDispenseDatetime")
                    ? SafeGetDateTime(reader, "DrugDispenseDatetime")
                    : SafeGetDateTime(reader, "drug_dispense_datetime");

                // ── HL7 binary ──────────────────────────────────────────────
                byte[] hl7 = null;
                try
                {
                    string hl7Col = HasColumn("Hl7Data") ? "Hl7Data" : "hl7_data";
                    int hl7Ord = reader.GetOrdinal(hl7Col);
                    if (!reader.IsDBNull(hl7Ord))
                        hl7 = reader[hl7Col] as byte[];
                }
                catch { }

                // ── RecieveStatus ⭐ ──────────────────────────────────────
                char status = HasColumn("RecieveStatus")
                    ? ReadStatusChar(reader, "RecieveStatus")
                    : ReadStatusChar(reader, "recieve_status");

                // ── RecieveStatusDatetime ───────────────────────────────────
                DateTime? statusDt = null;
                try
                {
                    string dtCol = HasColumn("RecieveStatusDatetime") ? "RecieveStatusDatetime" : "recieve_status_datetime";
                    int dtOrd = reader.GetOrdinal(dtCol);
                    statusDt = SafeGetDateTimeNullable(reader, dtOrd);
                }
                catch { }

                // ── RecieveOrderType ────────────────────────────────────────
                string orderType = HasColumn("RecieveOrderType")
                    ? SafeGetString(reader, "RecieveOrderType")
                    : SafeGetString(reader, "recieve_order_type");

                return new DrugDispenseipd
                {
                    DrugDispenseipdId = id,
                    DrugDispenseDatetime = dispDt,
                    Hl7Data = hl7,
                    RecieveStatus = status,
                    RecieveStatusDatetime = statusDt,
                    RecieveOrderType = orderType
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ReadDrugDispenseFromReader] Error: {ex.Message}", ex);
                return null;
            }
        }

        #endregion

        // ════════════════════════════════════════════════════════════════════
        //  EXPORT METHODS
        // ════════════════════════════════════════════════════════════════════

        #region Export Methods

        public byte[] GetHL7DataByOrderNoAndType(string orderNo, string orderType)
        {
            _logger.LogInfo($"[GetHL7DataByOrderNoAndType] OrderNo={orderNo}, OrderType={orderType}");

            try
            {
                if (string.IsNullOrWhiteSpace(orderNo) || string.IsNullOrWhiteSpace(orderType))
                {
                    _logger.LogWarning("[GetHL7DataByOrderNoAndType] OrderNo or OrderType is empty");
                    return null;
                }

                bool isIpd = orderType == "IPD";
                string connStr = isIpd ? _ipdConnectionString : _connectionString;
                string tableName = isIpd ? "drug_dispense_ipd" : "drug_dispense_opd";
                string idColumn = isIpd ? "drug_dispense_ipd_id" : "drug_dispense_opd_id";

                if (!CheckTableExistsOnConnection(tableName, connStr))
                {
                    _logger.LogWarning($"[GetHL7DataByOrderNoAndType] Table '{tableName}' not found");
                    return null;
                }

                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = $@"
                        SELECT hl7_data, {idColumn}
                        FROM {tableName}
                        WHERE CAST(hl7_data AS CHAR) LIKE @OrderNoPattern
                        ORDER BY drug_dispense_datetime DESC
                        LIMIT 1";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@OrderNoPattern", $"%{orderNo}%");
                        cmd.CommandTimeout = 30;

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var hl7Data = reader["hl7_data"] as byte[];
                                var id = SafeGetInt(reader, idColumn);

                                if (hl7Data != null && hl7Data.Length > 0)
                                {
                                    _logger.LogInfo($"[GetHL7DataByOrderNoAndType] Found ID={id}, Size={hl7Data.Length} bytes");
                                    return hl7Data;
                                }

                                _logger.LogWarning($"[GetHL7DataByOrderNoAndType] HL7 data null/empty for ID={id}");
                            }
                            else
                            {
                                _logger.LogWarning($"[GetHL7DataByOrderNoAndType] No record found for OrderNo={orderNo}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[GetHL7DataByOrderNoAndType] Error: {ex.Message}", ex);
            }

            return null;
        }

        #endregion
    }
}