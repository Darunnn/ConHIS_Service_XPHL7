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
        private readonly string _connectionString;        // OPD
        private readonly string _ipdConnectionString;     // IPD
        private readonly LogManager _logger = new LogManager();

        public DatabaseService(string connectionString, string ipdConnectionString = null)
        {
            _connectionString = connectionString;
            _ipdConnectionString = ipdConnectionString ?? connectionString; // fallback ถ้าไม่ได้ส่งมา
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
        //  CHECK TABLE EXISTS — แยก connection IPD / OPD
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// ตรวจสอบว่า table มีอยู่หรือไม่
        /// table ที่ลงท้ายด้วย _ipd จะใช้ _ipdConnectionString
        /// table อื่นๆ จะใช้ _connectionString (OPD)
        /// </summary>
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
                        cmd.CommandTimeout = 5;

                        using (var reader = cmd.ExecuteReader())
                        {
                            int recieveStatusDatetimeOrdinal = reader.GetOrdinal("recieve_status_datetime");
                            int recieveOrderTypeOrdinal = reader.GetOrdinal("recieve_order_type");

                            while (reader.Read())
                            {
                                try
                                {
                                    var orderType = reader.IsDBNull(recieveOrderTypeOrdinal)
                                        ? null : reader.GetString(recieveOrderTypeOrdinal);
                                    var prescId = reader.GetString("presc_id");

                                    result.Add(new DrugDispenseipd
                                    {
                                        DrugDispenseipdId = reader.GetInt32("drug_dispense_ipd_id"),
                                        PrescId = prescId,
                                        DrugRequestMsgType = reader.GetString("drug_request_msg_type"),
                                        Hl7Data = reader["hl7_data"] as byte[],
                                        DrugDispenseDatetime = reader.GetDateTime("drug_dispense_datetime"),
                                        RecieveStatus = reader.GetChar("recieve_status"),
                                        RecieveStatusDatetime = reader.IsDBNull(recieveStatusDatetimeOrdinal)
                                            ? (DateTime?)null : reader.GetDateTime(recieveStatusDatetimeOrdinal),
                                        RecieveOrderType = orderType
                                    });

                                    _logger.LogInfo(
                                        $"GetPendingDispenseData (IPD): Row PrescId={prescId}, " +
                                        $"RecieveOrderType={orderType}");
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
            _logger.LogInfo(
                $"UpdateReceiveStatus (IPD): Start for ID {drugDispenseipdId}, status {status}");

            try
            {
                using (var conn = new MySqlConnection(_ipdConnectionString))
                {
                    conn.Open();
                    _logger.LogInfo("Database connection opened for UpdateReceiveStatus (IPD)");

                    var sql = @"UPDATE drug_dispense_ipd 
                               SET recieve_status = @status, recieve_status_datetime = @datetime
                               WHERE drug_dispense_ipd_id = @id";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@status", status);
                        cmd.Parameters.AddWithValue("@datetime", DateTime.Now);
                        cmd.Parameters.AddWithValue("@id", drugDispenseipdId);
                        cmd.ExecuteNonQuery();
                        _logger.LogInfo(
                            $"UpdateReceiveStatus (IPD): Updated ID {drugDispenseipdId} to status {status}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating receive status (IPD) for ID {drugDispenseipdId}", ex);
            }
        }

        public void UpdateReceiveIPDStatusToPreview(int drugDispenseipdId)
        {
            _logger.LogInfo(
                $"UpdateReceiveIPDStatusToPreview: Setting ID {drugDispenseipdId} to 'P' (Preview)");

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
                        _logger.LogInfo(
                            $"UpdateReceiveIPDStatusToPreview: Updated ID {drugDispenseipdId} to 'P'");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Error updating IPD status to Preview for ID {drugDispenseipdId}", ex);
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
                    _logger.LogWarning(
                        "[GetPendingDispenseData OPD] Table 'drug_dispense_opd' does not exist");
                    return result;
                }

                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    _logger.LogInfo("[GetPendingDispenseData OPD] Database connection opened successfully");

                    var sql = @"SELECT drug_dispense_opd_id, presc_id, drug_request_msg_type, 
                       hl7_data, drug_dispense_datetime, recieve_status, recieve_status_datetime, recieve_order_type
                       FROM drug_dispense_opd 
                       WHERE recieve_status = 'N'
                       ORDER BY drug_dispense_datetime
                       LIMIT 500";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.CommandTimeout = 5;

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
                                    var orderType = reader.IsDBNull(recieveOrderTypeOrdinal)
                                        ? null : reader.GetString(recieveOrderTypeOrdinal);
                                    var prescId = reader.GetString("presc_id");

                                    result.Add(new DrugDispenseopd
                                    {
                                        DrugDispenseopdId = reader.GetInt32("drug_dispense_opd_id"),
                                        PrescId = prescId,
                                        DrugRequestMsgType = reader.GetString("drug_request_msg_type"),
                                        Hl7Data = reader["hl7_data"] as byte[],
                                        DrugDispenseDatetime = reader.GetDateTime("drug_dispense_datetime"),
                                        RecieveStatus = reader.GetChar("recieve_status"),
                                        RecieveStatusDatetime = reader.IsDBNull(recieveStatusDatetimeOrdinal)
                                            ? (DateTime?)null : reader.GetDateTime(recieveStatusDatetimeOrdinal),
                                        RecieveOrderType = orderType
                                    });

                                    if (rowCount % 10 == 0)
                                        _logger.LogInfo(
                                            $"[GetPendingDispenseData OPD] Reading progress: {rowCount} rows...");
                                }
                                catch (Exception rowEx)
                                {
                                    _logger.LogError(
                                        $"[GetPendingDispenseData OPD] Error reading row #{rowCount}: {rowEx.Message}", rowEx);
                                }
                            }

                            _logger.LogInfo(
                                $"[GetPendingDispenseData OPD] Completed - Found {result.Count} rows " +
                                $"(Total read: {rowCount})");
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
            _logger.LogInfo(
                $"UpdateReceiveStatus (OPD): Start for ID {drugDispenseopdId}, status {status}");

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
                        cmd.Parameters.AddWithValue("@status", status);
                        cmd.Parameters.AddWithValue("@datetime", DateTime.Now);
                        cmd.Parameters.AddWithValue("@id", drugDispenseopdId);
                        cmd.ExecuteNonQuery();
                        _logger.LogInfo(
                            $"UpdateReceiveStatus (OPD): Updated ID {drugDispenseopdId} to status {status}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating receive status (OPD) for ID {drugDispenseopdId}", ex);
            }
        }

        #endregion

        // ════════════════════════════════════════════════════════════════════
        //  SHARED METHODS
        // ════════════════════════════════════════════════════════════════════

        #region Shared Methods

        public List<DrugDispenseipd> GetPendingDispenseDataByOrderType(string orderType)
        {
            _logger.LogInfo(
                $"[GetPendingDispenseDataByOrderType] Start - OrderType={orderType}");

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
                    _logger.LogWarning(
                        $"[GetPendingDispenseDataByOrderType] Table '{tableName}' does not exist");
                    return new List<DrugDispenseipd>();
                }

                _logger.LogInfo(
                    $"[GetPendingDispenseDataByOrderType] Table '{tableName}' exists - Proceeding");

                if (orderType == "IPD")
                {
                    try
                    {
                        var result = GetPendingDispenseData();
                        _logger.LogInfo(
                            $"[GetPendingDispenseDataByOrderType] IPD returned {result.Count} records");
                        return result;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("[GetPendingDispenseDataByOrderType] Error in IPD query", ex);
                        return new List<DrugDispenseipd>();
                    }
                }
                else if (orderType == "OPD")
                {
                    try
                    {
                        var opdData = GetPendingDispenseOpdData();
                        _logger.LogInfo(
                            $"[GetPendingDispenseDataByOrderType] OPD returned {opdData.Count} records");

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

                        _logger.LogInfo(
                            $"[GetPendingDispenseDataByOrderType] OPD converted {result.Count} records");
                        return result;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("[GetPendingDispenseDataByOrderType] Error in OPD query", ex);
                        return new List<DrugDispenseipd>();
                    }
                }

                _logger.LogWarning(
                    $"[GetPendingDispenseDataByOrderType] Unknown OrderType: {orderType}");
                return new List<DrugDispenseipd>();
            }
            catch (Exception ex)
            {
                _logger.LogError("[GetPendingDispenseDataByOrderType] Critical error", ex);
                return new List<DrugDispenseipd>();
            }
        }

        // ── GetAllDispenseDataByDate ─────────────────────────────────────
        // IPD → _ipdConnectionString  |  OPD → _connectionString

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
                                    _logger.LogInfo(
                                        $"[IPD] Retrieved {result.Count - before} records " +
                                        $"for {date:yyyy-MM-dd} (IPD DB)");
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
                                    _logger.LogInfo(
                                        $"[OPD] Retrieved {result.Count - before} records " +
                                        $"for {date:yyyy-MM-dd} (OPD DB)");
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
                                    _logger.LogInfo(
                                        $"[IPD Search] Found {result.Count - before} records " +
                                        $"for '{searchText}' on {date:yyyy-MM-dd}");
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
                                    _logger.LogInfo(
                                        $"[OPD Search] Found {result.Count - before} records " +
                                        $"for '{searchText}' on {date:yyyy-MM-dd}");
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

            _logger.LogInfo(
                $"[Total Search] {result.Count} records for '{searchText}' on {date:yyyy-MM-dd}");
            return result;
        }

        // ════════════════════════════════════════════════════════════════════
        //  READ HELPER — ปลอดภัยต่อ null, alias, และ column ต่างกัน
        // ════════════════════════════════════════════════════════════════════

        private DrugDispenseipd ReadDrugDispenseFromReader(MySqlDataReader reader)
        {
            try
            {
                // ── local helpers ─────────────────────────────────────────
                bool HasColumn(string name)
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                        if (reader.GetName(i).Equals(name, StringComparison.OrdinalIgnoreCase))
                            return true;
                    return false;
                }

                int SafeInt(string col, int fallback = 0)
                {
                    if (!HasColumn(col)) return fallback;
                    int ord = reader.GetOrdinal(col);
                    return reader.IsDBNull(ord) ? fallback : reader.GetInt32(ord);
                }

                string SafeString(string col, string fallback = null)
                {
                    if (!HasColumn(col)) return fallback;
                    int ord = reader.GetOrdinal(col);
                    return reader.IsDBNull(ord) ? fallback : reader.GetString(ord);
                }

                DateTime? SafeDateTime(string col)
                {
                    if (!HasColumn(col)) return null;
                    int ord = reader.GetOrdinal(col);
                    return reader.IsDBNull(ord) ? (DateTime?)null : reader.GetDateTime(ord);
                }

                byte[] SafeBytes(string col)
                {
                    if (!HasColumn(col)) return null;
                    int ord = reader.GetOrdinal(col);
                    return reader.IsDBNull(ord) ? null : reader[col] as byte[];
                }

                // ── ID: ลอง alias ก่อน แล้วค่อย column จริง ──────────────
                int id = HasColumn("DrugDispenseipdId")
                    ? SafeInt("DrugDispenseipdId")
                    : HasColumn("drug_dispense_ipd_id")
                        ? SafeInt("drug_dispense_ipd_id")
                        : SafeInt("drug_dispense_opd_id");

                // ── DrugDispenseDatetime ───────────────────────────────────
                DateTime dispDt = DateTime.Now;
                if (HasColumn("DrugDispenseDatetime"))
                    dispDt = reader.GetDateTime(reader.GetOrdinal("DrugDispenseDatetime"));
                else if (HasColumn("drug_dispense_datetime"))
                    dispDt = reader.GetDateTime(reader.GetOrdinal("drug_dispense_datetime"));

                // ── HL7 binary ────────────────────────────────────────────
                byte[] hl7 = HasColumn("Hl7Data")
                    ? SafeBytes("Hl7Data")
                    : SafeBytes("hl7_data");

                // ── RecieveStatus (char) ───────────────────────────────────
                string statusStr = HasColumn("RecieveStatus")
                    ? SafeString("RecieveStatus", "N")
                    : SafeString("recieve_status", "N");
                char status = string.IsNullOrEmpty(statusStr) ? 'N' : statusStr[0];

                // ── RecieveStatusDatetime ─────────────────────────────────
                DateTime? statusDt = HasColumn("RecieveStatusDatetime")
                    ? SafeDateTime("RecieveStatusDatetime")
                    : SafeDateTime("recieve_status_datetime");

                // ── RecieveOrderType ──────────────────────────────────────
                string orderType = HasColumn("RecieveOrderType")
                    ? SafeString("RecieveOrderType")
                    : SafeString("recieve_order_type");

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
                _logger.LogError($"[ReadDrugDispenseFromReader] Error reading row: {ex.Message}", ex);
                return null; // caller ตรวจ null
            }
        }

        #endregion

        // ════════════════════════════════════════════════════════════════════
        //  EXPORT METHODS
        // ════════════════════════════════════════════════════════════════════

        #region Export Methods

        /// <summary>
        /// IPD → ค้นจาก _ipdConnectionString
        /// OPD → ค้นจาก _connectionString
        /// </summary>
        public byte[] GetHL7DataByOrderNoAndType(string orderNo, string orderType)
        {
            _logger.LogInfo(
                $"[GetHL7DataByOrderNoAndType] Start - OrderNo={orderNo}, OrderType={orderType}");

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
                    _logger.LogWarning(
                        $"[GetHL7DataByOrderNoAndType] Table '{tableName}' not found on target connection");
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
                        cmd.CommandTimeout = 5;

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var hl7Data = reader["hl7_data"] as byte[];
                                var id = reader.GetInt32(idColumn);

                                if (hl7Data != null && hl7Data.Length > 0)
                                {
                                    _logger.LogInfo(
                                        $"[GetHL7DataByOrderNoAndType] Found - ID={id}, " +
                                        $"Size={hl7Data.Length} bytes, DB={orderType}");
                                    return hl7Data;
                                }

                                _logger.LogWarning(
                                    $"[GetHL7DataByOrderNoAndType] HL7 data null/empty for ID={id}");
                            }
                            else
                            {
                                _logger.LogWarning(
                                    $"[GetHL7DataByOrderNoAndType] No record found - " +
                                    $"OrderNo={orderNo}, OrderType={orderType}");
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