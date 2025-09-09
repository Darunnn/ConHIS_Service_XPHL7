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

        // Fix for CS8370 and CS1503:
        // - Replace the raw string literal """recieve_status_datetime""" with a standard string "recieve_status_datetime".
        // - Use reader.GetOrdinal("recieve_status_datetime") to get the column index for IsDBNull(int).

        public List<DrugDispenseipd> GetPendingDispenseData()
        {
            var result = new List<DrugDispenseipd>();
            _logger.LogInfo("GetPendingDispenseData: Start");
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    _logger.LogInfo("Database connection opened for GetPendingDispenseData");
                    var sql = @"SELECT drug_dispense_ipd_id, presc_id, drug_request_msg_type, 
                               hl7_data, drug_dispense_datetime, recieve_status, recieve_status_datetime
                               FROM drug_dispense_ipd 
                               WHERE recieve_status = 'N'
                               ORDER BY drug_dispense_datetime";

                    using (var cmd = new MySqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        int recieveStatusDatetimeOrdinal = reader.GetOrdinal("recieve_status_datetime");
                        while (reader.Read())
                        {
                            result.Add(new DrugDispenseipd
                            {
                                DrugDispenseipdId = reader.GetInt32("drug_dispense_ipd_id"),
                                PrescId = reader.GetInt32("presc_id"),
                                DrugRequestMsgType = reader.GetString("drug_request_msg_type"),
                                Hl7Data = reader["hl7_data"] as byte[],
                                DrugDispenseDatetime = reader.GetDateTime("drug_dispense_datetime"),
                                RecieveStatus = reader.GetChar("recieve_status"),
                                RecieveStatusDatetime = reader.IsDBNull(recieveStatusDatetimeOrdinal) ?
                                    (DateTime?)null : reader.GetDateTime(recieveStatusDatetimeOrdinal)
                            });
                        }
                        // log เฉพาะจำนวน row สุดท้าย
                        _logger.LogInfo($"GetPendingDispenseData: Found {result.Count} rows");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting pending dispense data", ex);
                throw new Exception($"Error getting pending dispense data: {ex.Message}", ex);
            }

            return result;
        }

        public void UpdateReceiveStatus(int drugDispenseipdId, char status)
        {
            _logger.LogInfo($"UpdateReceiveStatus: Start for ID {drugDispenseipdId}, status {status}");
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    _logger.LogInfo("Database connection opened for UpdateReceiveStatus");
                    var sql = @"UPDATE drug_dispense_ipd 
                               SET recieve_status = @status, recieve_status_datetime = @datetime
                               WHERE drug_dispense_ipd_id = @id";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@status", status);
                        cmd.Parameters.AddWithValue("@datetime", DateTime.Now);
                        cmd.Parameters.AddWithValue("@id", drugDispenseipdId);
                        cmd.ExecuteNonQuery();
                        _logger.LogInfo($"UpdateReceiveStatus: Updated ID {drugDispenseipdId} to status {status}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating receive status for ID {drugDispenseipdId}", ex);
                throw new Exception($"Error updating receive status: {ex.Message}", ex);
            }
        }

        public void InsertDrugMachineData(DrugMachineipd data)
        {
            _logger.LogInfo($"InsertDrugMachineData: Start for PrescId {data.PrescId}");
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    _logger.LogInfo("Database connection opened for InsertDrugMachineData");
                    var sql = @"INSERT INTO drug_dispense_ipd 
                               (presc_id, drug_request_msg_type, hl7_data, drug_machine_datetime, recieve_status)
                               VALUES (@presc_id, @msg_type, @hl7_data, @datetime, @status)";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@presc_id", data.PrescId);
                        cmd.Parameters.AddWithValue("@msg_type", data.DrugRequestMsgType);
                        cmd.Parameters.AddWithValue("@hl7_data", data.Hl7Data);
                        cmd.Parameters.AddWithValue("@datetime", data.DrugMachineDatetime);
                        cmd.Parameters.AddWithValue("@status", data.RecieveStatus);
                        cmd.ExecuteNonQuery();
                        _logger.LogInfo($"InsertDrugMachineData: Inserted for PrescId {data.PrescId}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error inserting drug machine data for PrescId {data.PrescId}", ex);
                throw new Exception($"Error inserting drug machine data: {ex.Message}", ex);
            }
        }
    }
}