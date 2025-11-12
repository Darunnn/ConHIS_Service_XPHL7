using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using ConHIS_Service_XPHL7.Models;
using ConHIS_Service_XPHL7.Utils;

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

        public List<DrugDispenseipd> GetDispenseDataByDate(DateTime startDate, DateTime endDate)
        {
            var result = new List<DrugDispenseipd>();
            _logger.LogInfo($"GetDispenseDataByDate (IPD): Start - From {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    _logger.LogInfo("Database connection opened for GetDispenseDataByDate (IPD)");

                    var sql = @"SELECT drug_dispense_ipd_id, presc_id, drug_request_msg_type, 
                               hl7_data, drug_dispense_datetime, recieve_status, recieve_status_datetime, recieve_order_type
                               FROM drug_dispense_ipd 
                               WHERE recieve_status IN ('Y', 'F')
                               AND DATE(recieve_status_datetime) BETWEEN @startDate AND @endDate
                               ORDER BY recieve_status_datetime DESC
                               LIMIT 1000";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.CommandTimeout = 30;
                        cmd.Parameters.AddWithValue("@startDate", startDate.Date);
                        cmd.Parameters.AddWithValue("@endDate", endDate.Date);

                        using (var reader = cmd.ExecuteReader())
                        {
                            int recieveStatusDatetimeOrdinal = reader.GetOrdinal("recieve_status_datetime");
                            int recieveOrderTypeOrdinal = reader.GetOrdinal("recieve_order_type");

                            while (reader.Read())
                            {
                                try
                                {
                                    var orderType = reader.IsDBNull(recieveOrderTypeOrdinal) ? null : reader.GetString(recieveOrderTypeOrdinal);
                                    var prescId = reader.GetInt32("presc_id");
                                    var hl7Data = reader["hl7_data"] as byte[];

                                    result.Add(new DrugDispenseipd
                                    {
                                        DrugDispenseipdId = reader.GetInt32("drug_dispense_ipd_id"),
                                        PrescId = prescId,
                                        DrugRequestMsgType = reader.GetString("drug_request_msg_type"),
                                        Hl7Data = hl7Data,
                                        DrugDispenseDatetime = reader.GetDateTime("drug_dispense_datetime"),
                                        RecieveStatus = reader.GetChar("recieve_status"),
                                        RecieveStatusDatetime = reader.IsDBNull(recieveStatusDatetimeOrdinal) ?
                                            (DateTime?)null : reader.GetDateTime(recieveStatusDatetimeOrdinal),
                                        RecieveOrderType = orderType
                                    });
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning($"Error reading row from GetDispenseDataByDate (IPD): {ex.Message}");
                                }
                            }
                        }
                    }

                    _logger.LogInfo($"GetDispenseDataByDate (IPD): Found {result.Count} rows");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting dispense data by date (IPD)", ex);
            }

            return result;
        }

        public List<DrugDispenseipd> GetDispenseDataByDateAndSearch(DateTime date, string orderNoOrHN)
        {
            var result = new List<DrugDispenseipd>();
            _logger.LogInfo($"GetDispenseDataByDateAndSearch (IPD): Date={date:yyyy-MM-dd}, Search={orderNoOrHN}");

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    _logger.LogInfo("Database connection opened for GetDispenseDataByDateAndSearch (IPD)");

                    var sql = @"SELECT DISTINCT ddi.drug_dispense_ipd_id, ddi.presc_id, ddi.drug_request_msg_type, 
                               ddi.hl7_data, ddi.drug_dispense_datetime, ddi.recieve_status, ddi.recieve_status_datetime, ddi.recieve_order_type
                               FROM drug_dispense_ipd ddi
                               WHERE ddi.recieve_status IN ('Y', 'F')
                               AND DATE(ddi.recieve_status_datetime) = @date
                               AND CAST(ddi.hl7_data AS CHAR) LIKE @searchPattern
                               ORDER BY ddi.recieve_status_datetime DESC
                               LIMIT 1000";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.CommandTimeout = 30;
                        cmd.Parameters.AddWithValue("@date", date.Date);
                        cmd.Parameters.AddWithValue("@searchPattern", $"%{orderNoOrHN}%");

                        using (var reader = cmd.ExecuteReader())
                        {
                            int recieveStatusDatetimeOrdinal = reader.GetOrdinal("recieve_status_datetime");
                            int recieveOrderTypeOrdinal = reader.GetOrdinal("recieve_order_type");

                            while (reader.Read())
                            {
                                try
                                {
                                    var orderType = reader.IsDBNull(recieveOrderTypeOrdinal) ? null : reader.GetString(recieveOrderTypeOrdinal);
                                    var prescId = reader.GetInt32("presc_id");

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
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning($"Error reading row from GetDispenseDataByDateAndSearch (IPD): {ex.Message}");
                                }
                            }
                        }
                    }

                    _logger.LogInfo($"GetDispenseDataByDateAndSearch (IPD): Found {result.Count} rows");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting dispense data by date and search (IPD)", ex);
                _logger.LogWarning("Fallback: Using in-memory search instead");
                return GetDispenseDataByDate(date, date);
            }

            return result;
        }

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
                                    var prescId = reader.GetInt32("presc_id");

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

        public List<DrugDispenseopd> GetDispenseOpdDataByDate(DateTime startDate, DateTime endDate)
        {
            var result = new List<DrugDispenseopd>();
            _logger.LogInfo($"GetDispenseDataByDate (OPD): Start - From {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    _logger.LogInfo("Database connection opened for GetDispenseDataByDate (OPD)");

                    var sql = @"SELECT drug_dispense_opd_id, presc_id, drug_request_msg_type, 
                               hl7_data, drug_dispense_datetime, recieve_status, recieve_status_datetime, recieve_order_type
                               FROM drug_dispense_opd 
                               WHERE recieve_status IN ('Y', 'F')
                               AND DATE(recieve_status_datetime) BETWEEN @startDate AND @endDate
                               ORDER BY recieve_status_datetime DESC
                               LIMIT 1000";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.CommandTimeout = 30;
                        cmd.Parameters.AddWithValue("@startDate", startDate.Date);
                        cmd.Parameters.AddWithValue("@endDate", endDate.Date);

                        using (var reader = cmd.ExecuteReader())
                        {
                            int recieveStatusDatetimeOrdinal = reader.GetOrdinal("recieve_status_datetime");
                            int recieveOrderTypeOrdinal = reader.GetOrdinal("recieve_order_type");

                            while (reader.Read())
                            {
                                try
                                {
                                    var orderType = reader.IsDBNull(recieveOrderTypeOrdinal) ? null : reader.GetString(recieveOrderTypeOrdinal);
                                    var prescId = reader.GetInt32("presc_id");
                                    var hl7Data = reader["hl7_data"] as byte[];

                                    result.Add(new DrugDispenseopd
                                    {
                                        DrugDispenseopdId = reader.GetInt32("drug_dispense_opd_id"),
                                        PrescId = prescId,
                                        DrugRequestMsgType = reader.GetString("drug_request_msg_type"),
                                        Hl7Data = hl7Data,
                                        DrugDispenseDatetime = reader.GetDateTime("drug_dispense_datetime"),
                                        RecieveStatus = reader.GetChar("recieve_status"),
                                        RecieveStatusDatetime = reader.IsDBNull(recieveStatusDatetimeOrdinal) ?
                                            (DateTime?)null : reader.GetDateTime(recieveStatusDatetimeOrdinal),
                                        RecieveOrderType = orderType
                                    });
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning($"Error reading row from GetDispenseDataByDate (OPD): {ex.Message}");
                                }
                            }
                        }
                    }

                    _logger.LogInfo($"GetDispenseDataByDate (OPD): Found {result.Count} rows");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting dispense data by date (OPD)", ex);
            }

            return result;
        }

        public List<DrugDispenseopd> GetDispenseOpdDataByDateAndSearch(DateTime date, string orderNoOrHN)
        {
            var result = new List<DrugDispenseopd>();
            _logger.LogInfo($"GetDispenseDataByDateAndSearch (OPD): Date={date:yyyy-MM-dd}, Search={orderNoOrHN}");

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    _logger.LogInfo("Database connection opened for GetDispenseDataByDateAndSearch (OPD)");

                    var sql = @"SELECT DISTINCT ddo.drug_dispense_opd_id, ddo.presc_id, ddo.drug_request_msg_type, 
                               ddo.hl7_data, ddo.drug_dispense_datetime, ddo.recieve_status, ddo.recieve_status_datetime, ddo.recieve_order_type
                               FROM drug_dispense_opd ddo
                               WHERE ddo.recieve_status IN ('Y', 'F')
                               AND DATE(ddo.recieve_status_datetime) = @date
                               AND CAST(ddo.hl7_data AS CHAR) LIKE @searchPattern
                               ORDER BY ddo.recieve_status_datetime DESC
                               LIMIT 1000";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.CommandTimeout = 30;
                        cmd.Parameters.AddWithValue("@date", date.Date);
                        cmd.Parameters.AddWithValue("@searchPattern", $"%{orderNoOrHN}%");

                        using (var reader = cmd.ExecuteReader())
                        {
                            int recieveStatusDatetimeOrdinal = reader.GetOrdinal("recieve_status_datetime");
                            int recieveOrderTypeOrdinal = reader.GetOrdinal("recieve_order_type");

                            while (reader.Read())
                            {
                                try
                                {
                                    var orderType = reader.IsDBNull(recieveOrderTypeOrdinal) ? null : reader.GetString(recieveOrderTypeOrdinal);
                                    var prescId = reader.GetInt32("presc_id");

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
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning($"Error reading row from GetDispenseDataByDateAndSearch (OPD): {ex.Message}");
                                }
                            }
                        }
                    }

                    _logger.LogInfo($"GetDispenseDataByDateAndSearch (OPD): Found {result.Count} rows");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting dispense data by date and search (OPD)", ex);
                _logger.LogWarning("Fallback: Using in-memory search instead");
                return GetDispenseOpdDataByDate(date, date);
            }

            return result;
        }

        public List<DrugDispenseopd> GetPendingDispenseOpdData()
        {
            var result = new List<DrugDispenseopd>();
            _logger.LogInfo("GetPendingDispenseData (OPD): Start");
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    _logger.LogInfo("Database connection opened for GetPendingDispenseData (OPD)");

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

                            while (reader.Read())
                            {
                                try
                                {
                                    var orderType = reader.IsDBNull(recieveOrderTypeOrdinal) ? null : reader.GetString(recieveOrderTypeOrdinal);
                                    var prescId = reader.GetInt32("presc_id");

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

                                    _logger.LogInfo($"GetPendingDispenseData (OPD): Row PrescId={prescId}, RecieveOrderType={orderType}");
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning($"Error reading pending row (OPD): {ex.Message}");
                                }
                            }

                            _logger.LogInfo($"GetPendingDispenseData (OPD): Found {result.Count} rows");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting pending dispense data (OPD)", ex);
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
    }
}