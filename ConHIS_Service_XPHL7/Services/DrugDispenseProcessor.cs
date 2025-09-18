using ConHIS_Service_XPHL7.Models;
using ConHIS_Service_XPHL7.Utils;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Text;

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
                _logger.LogRawHL7Data(data.DrugDispenseipdId.ToString(), data.RecieveOrderType.ToString(), hl7String, "hl7_raw");
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

                // Log parsed HL7 data
                _logger.LogParsedHL7Data(data.DrugDispenseipdId.ToString(), hl7Message, "hl7_parsed");



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
                // Log data transformation step: HL7 validation

            }

            // Determine order type based on RecieveOrderType from database
            var orderControl = !string.IsNullOrEmpty(data.RecieveOrderType) ? data.RecieveOrderType : hl7Message.CommonOrder?.OrderControl;
            _logger.LogInfo($"OrderControl: {orderControl} for prescription ID: {data.PrescId}");

            // Log data transformation step: Order type determination


            if (orderControl == "NW")
            {
                ProcessNewOrder(data, hl7Message, logAction);
                _databaseService.UpdateReceiveStatus(data.DrugDispenseipdId, 'Y');
                _logger.LogInfo($"Updated receive status for prescription ID: {data.PrescId}");

                logAction($"Updated receive status for prescription ID: {data.PrescId}");
            }
            else if (orderControl == "RP")
            {
                ProcessReplaceOrder(data, hl7Message, logAction);
                _databaseService.UpdateReceiveStatus(data.DrugDispenseipdId, 'Y');
                _logger.LogInfo($"Updated receive status for prescription ID: {data.PrescId}");

                logAction($"Updated receive status for prescription ID: {data.PrescId}");
            }
            else
            {
                _logger.LogWarning($"Unknown order control: {orderControl} for prescription ID: {data.PrescId}");

                logAction($"Unknown order control: {orderControl}");
                // ไม่อัพเดต status เป็น Y
            }
        }

        private void ProcessNewOrder(DrugDispenseipd data, HL7Message hl7Message, Action<string> logAction)
        {
            _logger.LogInfo($"Processing new order for prescription: {data.PrescId}");
            logAction($"Processing new order for prescription: {data.PrescId}");

            // Prepare prescription API body
            var apiUrl = ConHIS_Service_XPHL7.Configuration.AppConfig.ApiEndpoint; // static property
            var apiMethod = "POST";
            var bodyObj = CreatePrescriptionBody(hl7Message, data);
            var bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.Indented);

            // Log data transformation step: API body creation


            // Log API request data
            var apiRequestData = new
            {
                Url = apiUrl,
                Method = apiMethod,
                Body = bodyObj,
                Timestamp = DateTime.Now
            };
            _logger.LogInfo($"API URL: {apiUrl}");
            _logger.LogInfo($"API Method: {apiMethod}");
            _logger.LogInfo($"API Body: {bodyJson}");
            logAction($"API URL: {apiUrl}");
            logAction($"API Method: {apiMethod}");
            logAction($"API Body: {bodyJson}");

            // ส่ง API จริง (คอมเมนต์ไว้ก่อน)
            // var success = _apiService.SendToMiddleware(apiData);
            // _logger.LogInfo($"SendToMiddleware result: {success} for new order: {data.PrescId}");
            // if (!success)
            // {
            //     _logger.LogError($"Failed to send data to middleware API for new order: {data.PrescId}");
            //     throw new Exception("Failed to send data to middleware API");
            // }

            // Create machine data entry




        }

        private void ProcessReplaceOrder(DrugDispenseipd data, HL7Message hl7Message, Action<string> logAction)
        {
            _logger.LogInfo($"Processing replace order for prescription: {data.PrescId}");
            logAction($"Processing replace order for prescription: {data.PrescId}");

            // Prepare prescription API body
            var apiUrl = ConHIS_Service_XPHL7.Configuration.AppConfig.ApiEndpoint; // static property
            var apiMethod = "POST";
            var bodyObj = CreatePrescriptionBody(hl7Message, data);
            var bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.Indented);



            // Log API request data
            var apiRequestData = new
            {
                Url = apiUrl,
                Method = apiMethod,
                Body = bodyObj,
                OrderType = "Replace",
                Timestamp = DateTime.Now
            };


            _logger.LogInfo($"API URL: {apiUrl}");
            _logger.LogInfo($"API Method: {apiMethod}");
            _logger.LogInfo($"API Body: {bodyJson}");
            logAction($"API URL: {apiUrl}");
            logAction($"API Method: {apiMethod}");
            logAction($"API Body: {bodyJson}");

            // ส่ง API จริง (คอมเมนต์ไว้ก่อน)
            // var success = _apiService.SendToMiddleware(apiData);
            // _logger.LogInfo($"SendToMiddleware result: {success} for replace order: {data.PrescId}");
            // if (!success)
            // {
            //     _logger.LogError($"Failed to send replace data to middleware API for prescription: {data.PrescId}");
            //     throw new Exception("Failed to send replace data to middleware API");
            // }



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

        private object CreatePrescriptionBody(HL7Message hl7, DrugDispenseipd data)
        {
            // Helper for safe DateTime formatting
            string FormatDate(DateTime? dt, string fmt)
            {
                return (dt.HasValue && dt.Value != DateTime.MinValue) ? dt.Value.ToString(fmt) : null;
            }

            DateTime? headerDt = hl7?.MessageHeader != null ? (DateTime?)hl7.MessageHeader.MessageDateTime : null;
            DateTime? patientDob = hl7?.PatientIdentification != null ? (DateTime?)hl7.PatientIdentification.DateOfBirth : null;

            // ✅ map ทุก PharmacyDispense
            var prescriptions = hl7?.PharmacyDispense?.Select(d => new
            {
                UniqID = hl7?.MessageHeader?.MessageControlID ?? data.PrescId.ToString(),
                f_prescriptionno = hl7?.CommonOrder?.PlacerOrderNumber ?? "",
                f_seq = 1,
                f_seqmax = 1,
                f_prescriptiondate = FormatDate(headerDt, "yyyyMMdd"),
                f_ordercreatedate = FormatDate(headerDt, "yyyy-MM-dd HH:mm:ss"),
                f_ordertargetdate = FormatDate(headerDt, "yyyy-MM-dd"),
                f_ordertargettime = (string)null,
                f_doctorcode = hl7?.PatientVisit?.AdmittingDoctor?.ID ?? "",
                f_doctorname = (hl7?.PatientVisit?.AdmittingDoctor != null)
                    ? $"{hl7.PatientVisit.AdmittingDoctor.Prefix} {hl7.PatientVisit.AdmittingDoctor.LastName} {hl7.PatientVisit.AdmittingDoctor.FirstName}".Trim()
                    : "",
                f_useracceptby = "",
                f_orderacceptdate = FormatDate(headerDt, "yyyy-MM-dd"),
                f_orderacceptfromip = (string)null,
                f_pharmacylocationcode = "",
                f_pharmacylocationdesc = "",
                f_prioritycode = "",
                f_prioritydesc = "",
                f_hn = hl7?.PatientIdentification?.PatientIDExternal ?? "",
                f_an = "",
                f_vn = hl7?.CommonOrder?.FillerOrderNumber ?? "",
                f_title = (hl7?.PatientIdentification?.OfficialName != null)
                    ? $"{hl7.PatientIdentification.OfficialName.Suffix}".Trim()
                    : "",
                f_patientname = (hl7?.PatientIdentification?.OfficialName != null)
                    ? $"{hl7.PatientIdentification.OfficialName.FirstName} {hl7.PatientIdentification.OfficialName.LastName}".Trim()
                    : "",
                f_sex = hl7?.PatientIdentification?.Sex ?? "",
                f_patientdob = FormatDate(patientDob, "yyyy-MM-dd"),
                f_wardcode = "",
                f_warddesc = "",
                f_roomcode = "",
                f_roomdesc = hl7?.PatientVisit?.AssignedPatientLocation?.Room ?? "",
                f_bedcode = "",
                f_beddesc = hl7?.PatientVisit?.AssignedPatientLocation?.Bed ?? "",
                f_right = (string)null,
                f_drugallergy = (string)null,
                f_dianosis = (string)null,
                f_orderitemcode = d?.Dispensegivecode?.Identifier ?? "",
                f_orderitemname = d?.Dispensegivecode?.DrugName ?? "",
                f_orderitemnameTH = d?.Dispensegivecode?.DrugNameThai ?? "",
                f_orderitemnamegeneric = d?.Dispensegivecode?.DrugNamePrint ?? "",
                f_orderqty = d?.QTY ?? 0,
                f_orderunitcode = d?.UsageUnit?.Code ?? "",
                f_orderunitdesc = d?.UsageUnit?.UnitName ?? "",
                f_dosage = d?.Dose ?? 0,
                f_dosageunit = d?.UsageUnit?.Code ?? "",
                f_dosagetext = (string)null,
                f_drugformcode = d?.DosageForm ?? "",
                f_drugformdesc = d?.DosageForm ?? "",
                f_HAD = "0",
                f_narcoticFlg = "0",
                f_psychotropic = "0",
                f_binlocation = (string)null,
                f_itemidentify = (string)null,
                f_itemlotno = (string)null,
                f_itemlotexpire = (string)null,
                f_instructioncode = d?.UsageCODE ?? "",
                f_instructiondesc = d?.UsageLine1 ?? "",
                f_frequencycode = d?.Frequency?.FrequencyID ?? "",
                f_frequencydesc = d?.Frequency?.FrequencyName ?? "",
                f_timecode = d?.Time?.TimeID ?? "",
                f_timedesc = d?.Time?.TimeName ?? "",
                f_frequencytime = (string)null,
                f_dosagedispense = (string)null,
                f_dayofweek = (string)null,
                f_noteprocessing = (string)null,
                f_prn = "0",
                f_stat = "0",
                f_comment = (string)null,
                f_tomachineno = "0",
                f_ipd_order_recordno = (string)null,
                f_status = hl7?.CommonOrder?.OrderControl == "NW" ? 1 :
                           hl7?.CommonOrder?.OrderControl == "RP" ? 0 : (int?)null,
            }).ToArray();

            return new { data = prescriptions ?? Array.Empty<object>() };
        }

    }
}