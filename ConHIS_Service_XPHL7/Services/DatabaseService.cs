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
        private readonly LogManager _logger = new LogManager();

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// ทดสอบการเชื่อมต่อกับฐานข้อมูล
        /// </summary>
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
                _logger.LogWarning($"Connection test failed: {ex.Message}");
                return false;
            }
        }

        #region IPD Methods

        //public List<DrugDispenseipd> GetDispenseDataByDate(DateTime startDate, DateTime endDate)
        //{
        //    var result = new List<DrugDispenseipd>();
        //    _logger.LogInfo($"GetDispenseDataByDate (IPD): Start - From {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

        //    try
        //    {
        //        using (var conn = new MySqlConnection(_connectionString))
        //        {
        //            conn.Open();
        //            _logger.LogInfo("Database connection opened for GetDispenseDataByDate (IPD)");

        //            var sql = @"SELECT drug_dispense_ipd_id, presc_id, drug_request_msg_type, 
        //                       hl7_data, drug_dispense_datetime, recieve_status, recieve_status_datetime, recieve_order_type
        //                       FROM drug_dispense_ipd 
        //                       WHERE recieve_status IN ('Y', 'F')
        //                       AND DATE(recieve_status_datetime) BETWEEN @startDate AND @endDate
        //                       ORDER BY recieve_status_datetime DESC
        //                       LIMIT 1000";

        //            using (var cmd = new MySqlCommand(sql, conn))
        //            {
        //                cmd.CommandTimeout = 30;
        //                cmd.Parameters.AddWithValue("@startDate", startDate.Date);
        //                cmd.Parameters.AddWithValue("@endDate", endDate.Date);

        //                using (var reader = cmd.ExecuteReader())
        //                {
        //                    int recieveStatusDatetimeOrdinal = reader.GetOrdinal("recieve_status_datetime");
        //                    int recieveOrderTypeOrdinal = reader.GetOrdinal("recieve_order_type");

        //                    while (reader.Read())
        //                    {
        //                        try
        //                        {
        //                            var orderType = reader.IsDBNull(recieveOrderTypeOrdinal) ? null : reader.GetString(recieveOrderTypeOrdinal);
        //                            var prescId = reader.GetString("presc_id");
        //                            var hl7Data = reader["hl7_data"] as byte[];

        //                            result.Add(new DrugDispenseipd
        //                            {
        //                                DrugDispenseipdId = reader.GetInt32("drug_dispense_ipd_id"),
        //                                PrescId = prescId,
        //                                DrugRequestMsgType = reader.GetString("drug_request_msg_type"),
        //                                Hl7Data = hl7Data,
        //                                DrugDispenseDatetime = reader.GetDateTime("drug_dispense_datetime"),
        //                                RecieveStatus = reader.GetChar("recieve_status"),
        //                                RecieveStatusDatetime = reader.IsDBNull(recieveStatusDatetimeOrdinal) ?
        //                                    (DateTime?)null : reader.GetDateTime(recieveStatusDatetimeOrdinal),
        //                                RecieveOrderType = orderType
        //                            });
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            _logger.LogWarning($"Error reading row from GetDispenseDataByDate (IPD): {ex.Message}");
        //                        }
        //                    }
        //                }
        //            }

        //            _logger.LogInfo($"GetDispenseDataByDate (IPD): Found {result.Count} rows");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError("Error getting dispense data by date (IPD)", ex);
        //    }

        //    return result;
        //}

        //public List<DrugDispenseipd> GetDispenseDataByDateAndSearch(DateTime date, string orderNoOrHN)
        //{
        //    var result = new List<DrugDispenseipd>();
        //    _logger.LogInfo($"GetDispenseDataByDateAndSearch (IPD): Date={date:yyyy-MM-dd}, Search={orderNoOrHN}");

        //    try
        //    {
        //        using (var conn = new MySqlConnection(_connectionString))
        //        {
        //            conn.Open();
        //            _logger.LogInfo("Database connection opened for GetDispenseDataByDateAndSearch (IPD)");

        //            var sql = @"SELECT DISTINCT ddi.drug_dispense_ipd_id, ddi.presc_id, ddi.drug_request_msg_type, 
        //                       ddi.hl7_data, ddi.drug_dispense_datetime, ddi.recieve_status, ddi.recieve_status_datetime, ddi.recieve_order_type
        //                       FROM drug_dispense_ipd ddi
        //                       WHERE ddi.recieve_status IN ('Y', 'F')
        //                       AND DATE(ddi.recieve_status_datetime) = @date
        //                       AND CAST(ddi.hl7_data AS CHAR) LIKE @searchPattern
        //                       ORDER BY ddi.recieve_status_datetime DESC
        //                       LIMIT 1000";

        //            using (var cmd = new MySqlCommand(sql, conn))
        //            {
        //                cmd.CommandTimeout = 30;
        //                cmd.Parameters.AddWithValue("@date", date.Date);
        //                cmd.Parameters.AddWithValue("@searchPattern", $"%{orderNoOrHN}%");

        //                using (var reader = cmd.ExecuteReader())
        //                {
        //                    int recieveStatusDatetimeOrdinal = reader.GetOrdinal("recieve_status_datetime");
        //                    int recieveOrderTypeOrdinal = reader.GetOrdinal("recieve_order_type");

        //                    while (reader.Read())
        //                    {
        //                        try
        //                        {
        //                            var orderType = reader.IsDBNull(recieveOrderTypeOrdinal) ? null : reader.GetString(recieveOrderTypeOrdinal);
        //                            var prescId = reader.GetString("presc_id");

        //                            result.Add(new DrugDispenseipd
        //                            {
        //                                DrugDispenseipdId = reader.GetInt32("drug_dispense_ipd_id"),
        //                                PrescId = prescId,
        //                                DrugRequestMsgType = reader.GetString("drug_request_msg_type"),
        //                                Hl7Data = reader["hl7_data"] as byte[],
        //                                DrugDispenseDatetime = reader.GetDateTime("drug_dispense_datetime"),
        //                                RecieveStatus = reader.GetChar("recieve_status"),
        //                                RecieveStatusDatetime = reader.IsDBNull(recieveStatusDatetimeOrdinal) ?
        //                                    (DateTime?)null : reader.GetDateTime(recieveStatusDatetimeOrdinal),
        //                                RecieveOrderType = orderType
        //                            });
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            _logger.LogWarning($"Error reading row from GetDispenseDataByDateAndSearch (IPD): {ex.Message}");
        //                        }
        //                    }
        //                }
        //            }

        //            _logger.LogInfo($"GetDispenseDataByDateAndSearch (IPD): Found {result.Count} rows");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError("Error getting dispense data by date and search (IPD)", ex);
        //        _logger.LogWarning("Fallback: Using in-memory search instead");
        //        return GetDispenseDataByDate(date, date);
        //    }

        //    return result;
        //}

        public List<DrugDispenseipd> GetPendingDispenseData()
        {
            var result = new List<DrugDispenseipd>();
            _logger.LogInfo("GetPendingDispenseData (IPD): Start");
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
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
                                    var orderType = reader.IsDBNull(recieveOrderTypeOrdinal) ? null : reader.GetString(recieveOrderTypeOrdinal);
                                    var prescId = reader.GetString("presc_id");

                                    result.Add(new DrugDispenseipd
                                    {
                                        DrugDispenseipdId = reader.GetInt32("drug_dispense_ipd_id"),
                                        PrescId = prescId,
                                        DrugRequestMsgType = reader.GetString("drug_request_msg_type"),
                                        Hl7Data = reader["hl7_data"] as byte[],
                                        DrugDispenseDatetime = reader.GetDateTime("drug_dispense_datetime"),
                                        RecieveStatus = reader.GetChar("recieve_status"),
                                        RecieveStatusDatetime = reader.IsDBNull(recieveStatusDatetimeOrdinal) ?
                                            (DateTime?)null : reader.GetDateTime(recieveStatusDatetimeOrdinal),
                                        RecieveOrderType = orderType
                                    });

                                    _logger.LogInfo($"GetPendingDispenseData (IPD): Row PrescId={prescId}, RecieveOrderType={orderType}");
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
            _logger.LogInfo($"UpdateReceiveStatus (IPD): Start for ID {drugDispenseipdId}, status {status}");
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
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
                        _logger.LogInfo($"UpdateReceiveStatus (IPD): Updated ID {drugDispenseipdId} to status {status}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating receive status (IPD) for ID {drugDispenseipdId}", ex);
            }
        }

        #endregion

        #region OPD Methods

        //public List<DrugDispenseopd> GetDispenseOpdDataByDate(DateTime startDate, DateTime endDate)
        //{
        //    var result = new List<DrugDispenseopd>();
        //    _logger.LogInfo($"GetDispenseDataByDate (OPD): Start - From {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

        //    try
        //    {
        //        using (var conn = new MySqlConnection(_connectionString))
        //        {
        //            conn.Open();
        //            _logger.LogInfo("Database connection opened for GetDispenseDataByDate (OPD)");

        //            var sql = @"SELECT drug_dispense_opd_id, presc_id, drug_request_msg_type, 
        //                       hl7_data, drug_dispense_datetime, recieve_status, recieve_status_datetime, recieve_order_type
        //                       FROM drug_dispense_opd 
        //                       WHERE recieve_status IN ('Y', 'F')
        //                       AND DATE(recieve_status_datetime) BETWEEN @startDate AND @endDate
        //                       ORDER BY recieve_status_datetime DESC
        //                       LIMIT 1000";

        //            using (var cmd = new MySqlCommand(sql, conn))
        //            {
        //                cmd.CommandTimeout = 30;
        //                cmd.Parameters.AddWithValue("@startDate", startDate.Date);
        //                cmd.Parameters.AddWithValue("@endDate", endDate.Date);

        //                using (var reader = cmd.ExecuteReader())
        //                {
        //                    int recieveStatusDatetimeOrdinal = reader.GetOrdinal("recieve_status_datetime");
        //                    int recieveOrderTypeOrdinal = reader.GetOrdinal("recieve_order_type");

        //                    while (reader.Read())
        //                    {
        //                        try
        //                        {
        //                            var orderType = reader.IsDBNull(recieveOrderTypeOrdinal) ? null : reader.GetString(recieveOrderTypeOrdinal);
        //                            var prescId = reader.GetString("presc_id");
        //                            var hl7Data = reader["hl7_data"] as byte[];

        //                            result.Add(new DrugDispenseopd
        //                            {
        //                                DrugDispenseopdId = reader.GetInt32("drug_dispense_opd_id"),
        //                                PrescId = prescId,
        //                                DrugRequestMsgType = reader.GetString("drug_request_msg_type"),
        //                                Hl7Data = hl7Data,
        //                                DrugDispenseDatetime = reader.GetDateTime("drug_dispense_datetime"),
        //                                RecieveStatus = reader.GetChar("recieve_status"),
        //                                RecieveStatusDatetime = reader.IsDBNull(recieveStatusDatetimeOrdinal) ?
        //                                    (DateTime?)null : reader.GetDateTime(recieveStatusDatetimeOrdinal),
        //                                RecieveOrderType = orderType
        //                            });
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            _logger.LogWarning($"Error reading row from GetDispenseDataByDate (OPD): {ex.Message}");
        //                        }
        //                    }
        //                }
        //            }

        //            _logger.LogInfo($"GetDispenseDataByDate (OPD): Found {result.Count} rows");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError("Error getting dispense data by date (OPD)", ex);
        //    }

        //    return result;
        //}

        //public List<DrugDispenseopd> GetDispenseOpdDataByDateAndSearch(DateTime date, string orderNoOrHN)
        //{
        //    var result = new List<DrugDispenseopd>();
        //    _logger.LogInfo($"GetDispenseDataByDateAndSearch (OPD): Date={date:yyyy-MM-dd}, Search={orderNoOrHN}");

        //    try
        //    {
        //        using (var conn = new MySqlConnection(_connectionString))
        //        {
        //            conn.Open();
        //            _logger.LogInfo("Database connection opened for GetDispenseDataByDateAndSearch (OPD)");

        //            var sql = @"SELECT DISTINCT ddo.drug_dispense_opd_id, ddo.presc_id, ddo.drug_request_msg_type, 
        //                       ddo.hl7_data, ddo.drug_dispense_datetime, ddo.recieve_status, ddo.recieve_status_datetime, ddo.recieve_order_type
        //                       FROM drug_dispense_opd ddo
        //                       WHERE ddo.recieve_status IN ('Y', 'F')
        //                       AND DATE(ddo.recieve_status_datetime) = @date
        //                       AND CAST(ddo.hl7_data AS CHAR) LIKE @searchPattern
        //                       ORDER BY ddo.recieve_status_datetime DESC
        //                       LIMIT 1000";

        //            using (var cmd = new MySqlCommand(sql, conn))
        //            {
        //                cmd.CommandTimeout = 30;
        //                cmd.Parameters.AddWithValue("@date", date.Date);
        //                cmd.Parameters.AddWithValue("@searchPattern", $"%{orderNoOrHN}%");

        //                using (var reader = cmd.ExecuteReader())
        //                {
        //                    int recieveStatusDatetimeOrdinal = reader.GetOrdinal("recieve_status_datetime");
        //                    int recieveOrderTypeOrdinal = reader.GetOrdinal("recieve_order_type");

        //                    while (reader.Read())
        //                    {
        //                        try
        //                        {
        //                            var orderType = reader.IsDBNull(recieveOrderTypeOrdinal) ? null : reader.GetString(recieveOrderTypeOrdinal);
        //                            var prescId = reader.GetString("presc_id");

        //                            result.Add(new DrugDispenseopd
        //                            {
        //                                DrugDispenseopdId = reader.GetInt32("drug_dispense_opd_id"),
        //                                PrescId = prescId,
        //                                DrugRequestMsgType = reader.GetString("drug_request_msg_type"),
        //                                Hl7Data = reader["hl7_data"] as byte[],
        //                                DrugDispenseDatetime = reader.GetDateTime("drug_dispense_datetime"),
        //                                RecieveStatus = reader.GetChar("recieve_status"),
        //                                RecieveStatusDatetime = reader.IsDBNull(recieveStatusDatetimeOrdinal) ?
        //                                    (DateTime?)null : reader.GetDateTime(recieveStatusDatetimeOrdinal),
        //                                RecieveOrderType = orderType
        //                            });
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            _logger.LogWarning($"Error reading row from GetDispenseDataByDateAndSearch (OPD): {ex.Message}");
        //                        }
        //                    }
        //                }
        //            }

        //            _logger.LogInfo($"GetDispenseDataByDateAndSearch (OPD): Found {result.Count} rows");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError("Error getting dispense data by date and search (OPD)", ex);
        //        _logger.LogWarning("Fallback: Using in-memory search instead");
        //        return GetDispenseOpdDataByDate(date, date);
        //    }

        //    return result;
        //}

        public List<DrugDispenseopd> GetPendingDispenseOpdData()
        {
            var result = new List<DrugDispenseopd>();
            _logger.LogInfo("[GetPendingDispenseData OPD] Start");

            try
            {
                // ⭐ เพิ่ม: ตรวจสอบ table ก่อน
                if (!CheckTableExists("drug_dispense_opd"))
                {
                    _logger.LogWarning("[GetPendingDispenseData OPD] Table 'drug_dispense_opd' does not exist");
                    return result;
                }

                using (var conn = new MySqlConnection(_connectionString))
                {
                    // ⭐ เพิ่ม: Log connection string (ซ่อน password)
                    var safeConnectionString = _connectionString.Replace(
                        System.Text.RegularExpressions.Regex.Match(_connectionString, @"password=([^;]+)",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase).Groups[1].Value,
                        "****"
                    );
                    _logger.LogInfo($"[GetPendingDispenseData OPD] Connecting to database: {safeConnectionString}");

                    conn.Open();
                    _logger.LogInfo("[GetPendingDispenseData OPD] Database connection opened successfully");

                    var sql = @"SELECT drug_dispense_opd_id, presc_id, drug_request_msg_type, 
                       hl7_data, drug_dispense_datetime, recieve_status, recieve_status_datetime, recieve_order_type
                       FROM drug_dispense_opd 
                       WHERE recieve_status = 'N'
                       ORDER BY drug_dispense_datetime
                       LIMIT 500";

                    _logger.LogInfo($"[GetPendingDispenseData OPD] Executing query: {sql}");

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.CommandTimeout = 30;

                        using (var reader = cmd.ExecuteReader())
                        {
                            _logger.LogInfo("[GetPendingDispenseData OPD] Query executed, reading results...");

                            int recieveStatusDatetimeOrdinal = reader.GetOrdinal("recieve_status_datetime");
                            int recieveOrderTypeOrdinal = reader.GetOrdinal("recieve_order_type");

                            int rowCount = 0; // ⭐ เพิ่ม counter
                            while (reader.Read())
                            {
                                try
                                {
                                    rowCount++;
                                    var orderType = reader.IsDBNull(recieveOrderTypeOrdinal) ? null : reader.GetString(recieveOrderTypeOrdinal);
                                    var prescId = reader.GetString("presc_id");

                                    result.Add(new DrugDispenseopd
                                    {
                                        DrugDispenseopdId = reader.GetInt32("drug_dispense_opd_id"),
                                        PrescId = prescId,
                                        DrugRequestMsgType = reader.GetString("drug_request_msg_type"),
                                        Hl7Data = reader["hl7_data"] as byte[],
                                        DrugDispenseDatetime = reader.GetDateTime("drug_dispense_datetime"),
                                        RecieveStatus = reader.GetChar("recieve_status"),
                                        RecieveStatusDatetime = reader.IsDBNull(recieveStatusDatetimeOrdinal) ?
                                            (DateTime?)null : reader.GetDateTime(recieveStatusDatetimeOrdinal),
                                        RecieveOrderType = orderType
                                    });

                                    // ⭐ Log ทุก 10 rows
                                    if (rowCount % 10 == 0)
                                    {
                                        _logger.LogInfo($"[GetPendingDispenseData OPD] Reading progress: {rowCount} rows...");
                                    }
                                }
                                catch (Exception rowEx)
                                {
                                    _logger.LogError($"[GetPendingDispenseData OPD] Error reading row #{rowCount}: {rowEx.Message}", rowEx);
                                }
                            }

                            _logger.LogInfo($"[GetPendingDispenseData OPD] Query completed - Found {result.Count} rows (Total read: {rowCount})");
                        }
                    }
                }
            }
            catch (MySqlException mysqlEx)
            {
                // ⭐ เพิ่ม: จัดการ MySQL Error แยก
                _logger.LogError($"[GetPendingDispenseData OPD] MySQL Error (Code: {mysqlEx.Number}): {mysqlEx.Message}", mysqlEx);
                _logger.LogError($"[GetPendingDispenseData OPD] StackTrace: {mysqlEx.StackTrace}", mysqlEx);
            }
            catch (Exception ex)
            {
                _logger.LogError("[GetPendingDispenseData OPD] Critical error", ex);
                _logger.LogError($"[GetPendingDispenseData OPD] StackTrace: {ex.StackTrace}", ex);
            }

            return result;
        }

        public void UpdateReceiveOpdStatus(int drugDispenseopdId, char status)
        {
            _logger.LogInfo($"UpdateReceiveStatus (OPD): Start for ID {drugDispenseopdId}, status {status}");
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    _logger.LogInfo("Database connection opened for UpdateReceiveStatus (OPD)");

                    var sql = @"UPDATE drug_dispense_opd 
                               SET recieve_status = @status, recieve_status_datetime = @datetime
                               WHERE drug_dispense_opd_id = @id";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@status", status);
                        cmd.Parameters.AddWithValue("@datetime", DateTime.Now);
                        cmd.Parameters.AddWithValue("@id", drugDispenseopdId);
                        cmd.ExecuteNonQuery();
                        _logger.LogInfo($"UpdateReceiveStatus (OPD): Updated ID {drugDispenseopdId} to status {status}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating receive status (OPD) for ID {drugDispenseopdId}", ex);
            }
        }

        #endregion
        #region Shared Methods

        /// <summary>
        /// ดึงข้อมูล Pending ตาม OrderType (IPD หรือ OPD)
        /// </summary>
        public List<DrugDispenseipd> GetPendingDispenseDataByOrderType(string orderType)
        {
            _logger.LogInfo($"[GetPendingDispenseDataByOrderType] Start - OrderType={orderType}");

            try
            {
                // ⭐ เพิ่ม: ตรวจสอบ parameter
                if (string.IsNullOrWhiteSpace(orderType))
                {
                    _logger.LogError("[GetPendingDispenseDataByOrderType] OrderType is null or empty");
                    return new List<DrugDispenseipd>();
                }

                // ⭐ เพิ่ม: ตรวจสอบ table ก่อน query
                string tableName = orderType == "IPD" ? "drug_dispense_ipd" : "drug_dispense_opd";

                if (!CheckTableExists(tableName))
                {
                    _logger.LogWarning($"[GetPendingDispenseDataByOrderType] Table '{tableName}' does not exist");
                    return new List<DrugDispenseipd>();
                }

                _logger.LogInfo($"[GetPendingDispenseDataByOrderType] Table '{tableName}' exists - Proceeding with query");

                // ⭐ เพิ่ม: Try-Catch แยกสำหรับแต่ละ OrderType
                if (orderType == "IPD")
                {
                    try
                    {
                        var result = GetPendingDispenseData();
                        _logger.LogInfo($"[GetPendingDispenseDataByOrderType] IPD returned {result.Count} records");
                        return result;
                    }
                    catch (Exception ipdEx)
                    {
                        _logger.LogError($"[GetPendingDispenseDataByOrderType] Error in IPD query", ipdEx);
                        return new List<DrugDispenseipd>();
                    }
                }
                else if (orderType == "OPD")
                {
                    try
                    {
                        var opdData = GetPendingDispenseOpdData();
                        _logger.LogInfo($"[GetPendingDispenseDataByOrderType] OPD query returned {opdData.Count} records");

                        // แปลง DrugDispenseopd เป็น DrugDispenseipd
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

                        _logger.LogInfo($"[GetPendingDispenseDataByOrderType] OPD converted {result.Count} records");
                        return result;
                    }
                    catch (Exception opdEx)
                    {
                        _logger.LogError($"[GetPendingDispenseDataByOrderType] Error in OPD query", opdEx);
                        return new List<DrugDispenseipd>();
                    }
                }
                else
                {
                    _logger.LogWarning($"[GetPendingDispenseDataByOrderType] Unknown OrderType: {orderType}");
                    return new List<DrugDispenseipd>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[GetPendingDispenseDataByOrderType] Critical error", ex);
                return new List<DrugDispenseipd>();
            }
        }

        /// <summary>
        /// ดึงข้อมูลตามวันที่ รวมทั้ง IPD และ OPD
        /// </summary>
        public List<DrugDispenseipd> GetAllDispenseDataByDate(DateTime date, bool includeIPD = true, bool includeOPD = true)
        {
            var result = new List<DrugDispenseipd>();

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    // ⭐ Query จาก IPD table
                    if (includeIPD && CheckTableExists("drug_dispense_ipd"))
                    {
                        try
                        {
                            string ipdQuery = @"
                        SELECT 
                            drug_dispense_ipd_id as DrugDispenseipdId,
                            drug_dispense_datetime as DrugDispenseDatetime,
                            hl7_data as Hl7Data,
                            recieve_status as RecieveStatus,
                            recieve_status_datetime as RecieveStatusDatetime,
                            'IPD' as RecieveOrderType
                        FROM drug_dispense_ipd
                     WHERE(  DATE(recieve_status_datetime) = @Date
                             OR
                             DATE(drug_dispense_datetime) = @Date
                          )

                        AND recieve_status IN ('Y', 'F')
                        ORDER BY drug_dispense_datetime DESC";

                            using (var command = new MySqlCommand(ipdQuery, connection))
                            {
                                command.Parameters.AddWithValue("@Date", date.Date);

                                using (var reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        result.Add(ReadDrugDispenseFromReader(reader));
                                    }
                                }
                            }
                            _logger.LogInfo($"[IPD] Retrieved {result.Count} records for date {date:yyyy-MM-dd}");
                        }
                        catch (Exception ipdEx)
                        {
                            _logger.LogError($"Error querying IPD table: {ipdEx.Message}", ipdEx);
                        }
                    }

                    // ⭐ Query จาก OPD table
                    if (includeOPD && CheckTableExists("drug_dispense_opd"))
                    {
                        try
                        {
                            string opdQuery = @"
                        SELECT 
                            drug_dispense_opd_id as DrugDispenseipdId,
                            drug_dispense_datetime as DrugDispenseDatetime,
                            hl7_data as Hl7Data,
                            recieve_status as RecieveStatus,
                            recieve_status_datetime as RecieveStatusDatetime,
                            'OPD' as RecieveOrderType
                        FROM drug_dispense_opd
                       WHERE(DATE(recieve_status_datetime) = @Date
                             OR
                             DATE(drug_dispense_datetime) = @Date
                            )
                        AND recieve_status IN ('Y', 'F')
                        ORDER BY drug_dispense_datetime DESC";

                            using (var command = new MySqlCommand(opdQuery, connection))
                            {
                                command.Parameters.AddWithValue("@Date", date.Date);

                                using (var reader = command.ExecuteReader())
                                {
                                    int opdCount = 0;
                                    while (reader.Read())
                                    {
                                        result.Add(ReadDrugDispenseFromReader(reader));
                                        opdCount++;
                                    }
                                    _logger.LogInfo($"[OPD] Retrieved {opdCount} records for date {date:yyyy-MM-dd}");
                                }
                            }
                        }
                        catch (Exception opdEx)
                        {
                            _logger.LogError($"Error querying OPD table: {opdEx.Message}", opdEx);
                        }
                    }
                }

                _logger.LogInfo($"[Total] Retrieved {result.Count} records for date {date:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetAllDispenseDataByDate: {ex.Message}", ex);
            }

            return result;
        }
        /// <summary>
        /// ค้นหาข้อมูลตามวันที่และคำค้นหา รวมทั้ง IPD และ OPD
        /// </summary>
        public List<DrugDispenseipd> GetAllDispenseDataByDateAndSearch(DateTime date, string searchText, bool includeIPD = true, bool includeOPD = true)
        {
            var result = new List<DrugDispenseipd>();

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    // ⭐ Search ใน IPD table
                    if (includeIPD && CheckTableExists("drug_dispense_ipd"))
                    {
                        try
                        {
                            string ipdQuery = @"
                        SELECT 
                            drug_dispense_ipd_id as DrugDispenseipdId,
                            drug_dispense_datetime as DrugDispenseDatetime,
                            hl7_data as Hl7Data,
                            recieve_status as RecieveStatus,
                            recieve_status_datetime as RecieveStatusDatetime,
                            'IPD' as RecieveOrderType
                        FROM drug_dispense_ipd
                        WHERE(  DATE(recieve_status_datetime) = @Date
                             OR
                             DATE(drug_dispense_datetime) = @Date
                            )
                     
                        AND recieve_status IN ('Y', 'F')
                        AND CAST(hl7_data AS CHAR) LIKE @SearchText
                        ORDER BY drug_dispense_datetime DESC";

                            using (var command = new MySqlCommand(ipdQuery, connection))
                            {
                                command.Parameters.AddWithValue("@Date", date.Date);
                                command.Parameters.AddWithValue("@SearchText", $"%{searchText}%");

                                using (var reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        result.Add(ReadDrugDispenseFromReader(reader));
                                    }
                                }
                            }
                            _logger.LogInfo($"[IPD Search] Found {result.Count} records for '{searchText}' on {date:yyyy-MM-dd}");
                        }
                        catch (Exception ipdEx)
                        {
                            _logger.LogError($"Error searching IPD table: {ipdEx.Message}", ipdEx);
                        }
                    }

                    // ⭐ Search ใน OPD table
                    if (includeOPD && CheckTableExists("drug_dispense_opd"))
                    {
                        try
                        {
                            string opdQuery = @"
                        SELECT 
                            drug_dispense_opd_id as DrugDispenseipdId,
                            drug_dispense_datetime as DrugDispenseDatetime,
                            hl7_data as Hl7Data,
                            recieve_status as RecieveStatus,
                            recieve_status_datetime as RecieveStatusDatetime,
                            'OPD' as RecieveOrderType
                        FROM drug_dispense_opd
                       WHERE(  DATE(recieve_status_datetime) = @Date
                             OR
                             DATE(drug_dispense_datetime) = @Date
                            )
                        AND recieve_status IN ('Y', 'F')
                        AND CAST(hl7_data AS CHAR) LIKE @SearchText
                        ORDER BY drug_dispense_datetime DESC";

                            using (var command = new MySqlCommand(opdQuery, connection))
                            {
                                command.Parameters.AddWithValue("@Date", date.Date);
                                command.Parameters.AddWithValue("@SearchText", $"%{searchText}%");

                                using (var reader = command.ExecuteReader())
                                {
                                    int opdCount = 0;
                                    while (reader.Read())
                                    {
                                        result.Add(ReadDrugDispenseFromReader(reader));
                                        opdCount++;
                                    }
                                    _logger.LogInfo($"[OPD Search] Found {opdCount} records for '{searchText}' on {date:yyyy-MM-dd}");
                                }
                            }
                        }
                        catch (Exception opdEx)
                        {
                            _logger.LogError($"Error searching OPD table: {opdEx.Message}", opdEx);
                        }
                    }
                }

                _logger.LogInfo($"[Total Search] Found {result.Count} records for '{searchText}' on {date:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetAllDispenseDataByDateAndSearch: {ex.Message}", ex);
            }

            return result;
        }
        public bool CheckTableExists(string tableName)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
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
                Console.WriteLine($"Error checking table '{tableName}': {ex.Message}");
                return false;
            }
        }

        private DrugDispenseipd ReadDrugDispenseFromReader(MySqlDataReader reader)
        {
            return new DrugDispenseipd
            {
                DrugDispenseipdId = reader.GetInt32("DrugDispenseipdId"),
                DrugDispenseDatetime = reader.GetDateTime("DrugDispenseDatetime"),
                Hl7Data = reader["Hl7Data"] as byte[],
                RecieveStatus = reader.GetString("RecieveStatus")[0],
                RecieveStatusDatetime = reader.IsDBNull(reader.GetOrdinal("RecieveStatusDatetime"))
                    ? (DateTime?)null
                    : reader.GetDateTime("RecieveStatusDatetime"),
                RecieveOrderType = reader.GetString("RecieveOrderType")
            };
        }
        #endregion
    }
}