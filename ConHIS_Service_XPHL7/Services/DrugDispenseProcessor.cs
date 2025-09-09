using ConHIS_Service_XPHL7.Models;
using ConHIS_Service_XPHL7.Utils;
using System;
using System.Text;
using System.Windows.Forms.Design;
using System.IO;

namespace ConHIS_Service_XPHL7.Services
{
    public class DrugDispenseProcessor
    {
        private readonly DatabaseService _databaseService;
        private readonly HL7Service _hl7Service;
        private readonly ApiService _apiService;
        private readonly LogManager _logger = new LogManager();

        public DrugDispenseProcessor(DatabaseService databaseService, HL7Service hl7Service, ApiService apiService)
        {
            _databaseService = databaseService;
            _hl7Service = hl7Service;
            _apiService = apiService;
        }

        public void ProcessPendingOrders(Action<string> logAction)
        {
            _logger.LogInfo("Start ProcessPendingOrders");
            try
            {
                var pendingData = _databaseService.GetPendingDispenseData();
                _logger.LogInfo($"Found {pendingData.Count} pending orders");
                logAction($"Found {pendingData.Count} pending orders");

                foreach (var data in pendingData)
                {
                    try
                    {
                        _logger.LogInfo($"Processing order {data.PrescId}");
                        ProcessSingleOrder(data, logAction);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error processing order {data.PrescId}", ex);
                        logAction($"Error processing order {data.PrescId}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in ProcessPendingOrders", ex);
                logAction($"Error in ProcessPendingOrders: {ex.Message}");
            }
        }

        private void ProcessSingleOrder(DrugDispenseipd data, Action<string> logAction)
        {
            // Convert byte array to string
            string hl7String;
            try
            {
                Encoding tisEncoding = null;
                try { tisEncoding = Encoding.GetEncoding("TIS-620"); } catch { }
                if (tisEncoding == null) { try { tisEncoding = Encoding.GetEncoding(874); } catch { } }
                if (tisEncoding != null) { hl7String = tisEncoding.GetString(data.Hl7Data); }
                else { hl7String = Encoding.UTF8.GetString(data.Hl7Data); }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to decode HL7 data with TIS-620: {ex.Message}. Falling back to UTF8.");
                hl7String = Encoding.UTF8.GetString(data.Hl7Data);
            }

            // สร้าง log แยกไฟล์ hl7_data_raw_xxx.txt ในโฟลเดอร์ hl7_raw
            try
            {
                _logger.LogRawHL7Data(data.PrescId.ToString(), hl7String, "hl7_raw");
                _logger.LogInfo($"HL7 raw data saved to hl7_raw/hl7_data_raw_{data.PrescId}.txt");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to write HL7 raw data file for PrescId {data.PrescId}: {ex.Message}");
            }

            logAction($"Processing prescription ID: {data.PrescId}");

            // Parse HL7 message
            HL7Message hl7Message = null;
            try
            {
                hl7Message = _hl7Service.ParseHL7Message(hl7String);
                _logger.LogInfo($"Parsed HL7 message for prescription ID: {data.PrescId}");
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error parsing HL7 for prescription ID: {data.PrescId} - {ex.Message}";
                _databaseService.UpdateReceiveStatus(data.DrugDispenseipdId, 'F'); // เปลี่ยนสถานะในฐานข้อมูล
                _logger.LogReadError(data.PrescId.ToString(), errorMsg, "logreaderror");
                _logger.LogInfo($"Create log success {data.PrescId}");
                _logger.LogError(errorMsg, ex);
                logAction(errorMsg);
                return; // ข้ามการ process order นี้
            }

            // Check message format
            if (hl7Message == null || hl7Message.MessageHeader == null)
            {
                var errorFields = new System.Collections.Generic.List<string>();
                if (hl7Message == null) errorFields.Add("HL7Message");
                if (hl7Message != null && hl7Message.MessageHeader == null) errorFields.Add("MSH segment");
                var errorMsg = $"Invalid HL7 format for prescription ID: {data.PrescId}. Error fields: {string.Join(", ", errorFields)}";
                _databaseService.UpdateReceiveStatus(data.DrugDispenseipdId, 'F'); // เปลี่ยนสถานะในฐานข้อมูล
                _logger.LogReadError(data.PrescId.ToString(), errorMsg, "logreaderror");
                _logger.LogInfo($"Create log success {data.PrescId}");
                _logger.LogError(errorMsg);
                logAction(errorMsg);
                return; // ข้ามการ process order นี้
            }
            else
            {
                _logger.LogInfo($"Check success {data.PrescId}");
            }

            // Determine order type based on ORC.OrderControl
            var orderControl = hl7Message.CommonOrder?.OrderControl;
            _logger.LogInfo($"OrderControl: {orderControl} for prescription ID: {data.PrescId}");

            if (orderControl == "NW")
            {
                ProcessNewOrder(data, hl7Message, logAction);
            }
            else if (orderControl == "RP")
            {
                ProcessReplaceOrder(data, hl7Message, logAction);
            }
            else
            {
                _logger.LogWarning($"Unknown order control: {orderControl} for prescription ID: {data.PrescId}");
                logAction($"Unknown order control: {orderControl}");
            }

            // Update receive status
            _databaseService.UpdateReceiveStatus(data.DrugDispenseipdId, 'Y');
            _logger.LogInfo($"Updated receive status for prescription ID: {data.PrescId}");
            logAction($"Updated receive status for prescription ID: {data.PrescId}");
        }

        private void ProcessNewOrder(DrugDispenseipd data, HL7Message hl7Message, Action<string> logAction)
        {
            _logger.LogInfo($"Processing new order for prescription: {data.PrescId}");
            logAction($"Processing new order for prescription: {data.PrescId}");

            // Prepare data for middle API
            var apiData = PrepareApiData(hl7Message);
            _logger.LogInfo($"Prepared API data for new order: {data.PrescId}");

            // Send to API
            var success = _apiService.SendToMiddleware(apiData);
            _logger.LogInfo($"SendToMiddleware result: {success} for new order: {data.PrescId}");

            if (success)
            {
                // Create machine data entry
                var machineData = new DrugMachineipd
                {
                    PrescId = data.PrescId,
                    DrugRequestMsgType = data.DrugRequestMsgType,
                    Hl7Data = data.Hl7Data,
                    DrugMachineDatetime = DateTime.Now,
                    RecieveStatus = 'Y'
                };

                _databaseService.InsertDrugMachineData(machineData);
                _logger.LogInfo($"Inserted DrugMachineipd for new order: {data.PrescId}");
                logAction($"Successfully processed new order: {data.PrescId}");
            }
            else
            {
                _logger.LogError($"Failed to send data to middleware API for new order: {data.PrescId}");
                throw new Exception("Failed to send data to middleware API");
            }
        }

        private void ProcessReplaceOrder(DrugDispenseipd data, HL7Message hl7Message, Action<string> logAction)
        {
            _logger.LogInfo($"Processing replace order for prescription: {data.PrescId}");
            logAction($"Processing replace order for prescription: {data.PrescId}");

            // Similar processing for replace order
            var apiData = PrepareApiData(hl7Message);
            _logger.LogInfo($"Prepared API data for replace order: {data.PrescId}");

            var success = _apiService.SendToMiddleware(apiData);
            _logger.LogInfo($"SendToMiddleware result: {success} for replace order: {data.PrescId}");

            if (success)
            {
                var machineData = new DrugMachineipd
                {
                    PrescId = data.PrescId,
                    DrugRequestMsgType = data.DrugRequestMsgType,
                    Hl7Data = data.Hl7Data,
                    DrugMachineDatetime = DateTime.Now,
                    RecieveStatus = 'Y'
                };

                _databaseService.InsertDrugMachineData(machineData);
                _logger.LogInfo($"Inserted DrugMachineipd for replace order: {data.PrescId}");
                logAction($"Successfully processed replace order: {data.PrescId}");
            }
            else
            {
                _logger.LogError($"Failed to send replace data to middleware API for prescription: {data.PrescId}");
                throw new Exception("Failed to send replace data to middleware API");
            }
        }

        private object PrepareApiData(HL7Message hl7Message)
        {
            return new
            {
                MessageHeader = hl7Message.MessageHeader,
                Patient = hl7Message.PatientIdentification,
                Visit = hl7Message.PatientVisit,
                Order = hl7Message.CommonOrder,
                Allergies = hl7Message.Allergies,
                Medications = hl7Message.PharmacyDispense,
                Routes = hl7Message.RouteInfo,
                Notes = hl7Message.Notes,
                ProcessedDateTime = DateTime.Now
            };
        }
    }
}