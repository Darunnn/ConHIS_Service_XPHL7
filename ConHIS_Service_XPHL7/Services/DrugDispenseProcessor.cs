using ConHIS_Service_XPHL7.Models;
using ConHIS_Service_XPHL7.Utils;
using Mysqlx.Session;
using MySqlX.XDevAPI.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Windows.Forms.Design;
using static Org.BouncyCastle.Math.EC.ECCurve;

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
                _databaseService.UpdateReceiveStatus(data.DrugDispenseipdId, 'F');
                _logger.LogReadError(data.PrescId.ToString(), errorMsg, "logreaderror");

                _logger.LogInfo($"Create log success {data.PrescId}");
                _logger.LogError(errorMsg, ex);
                logAction(errorMsg);
                return;
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
                ProcessNewOrder(data, hl7Message);
                _databaseService.UpdateReceiveStatus(data.DrugDispenseipdId, 'Y');
                _logger.LogInfo($"Updated receive status for prescription ID: {data.PrescId}");


            }
            else if (orderControl == "RP")
            {
                ProcessReplaceOrder(data, hl7Message);
                _databaseService.UpdateReceiveStatus(data.DrugDispenseipdId, 'Y');
                _logger.LogInfo($"Updated receive status for prescription ID: {data.PrescId}");
            }
            else
            {
                _logger.LogWarning($"Unknown order control: {orderControl} for prescription ID: {data.PrescId}");


            }
        }

        public class ApiResponse
        {
            public string UniqID { get; set; }
            public bool Status { get; set; }
            public string Message { get; set; }
        }

        private void ProcessNewOrder(DrugDispenseipd data, HL7Message hl7Message)
        {
            _logger.LogInfo($"Processing new order for prescription: {data.PrescId}");

            var apiUrl = ConHIS_Service_XPHL7.Configuration.AppConfig.ApiEndpoint;
            var apiMethod = "POST";
            var bodyObj = CreatePrescriptionBody(hl7Message, data);




            var bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.Indented);


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

            // ส่ง API จริงและรับ Response
            try
            {
                var response = _apiService.SendToMiddlewareWithResponse(bodyObj);
                _logger.LogInfo($"API Response: {response}");

                // Parse response เพื่อตรวจสอบสถานะ
                if (!string.IsNullOrEmpty(response))
                {
                    var responseArray = JsonConvert.DeserializeObject<ApiResponse[]>(response);
                    if (responseArray != null && responseArray.Length > 0)
                    {
                        var firstResponse = responseArray[0];
                        _logger.LogInfo($"UniqID: {firstResponse.UniqID}, Status: {firstResponse.Status}, Message: {firstResponse.Message}");

                        if (firstResponse.Status)
                        {
                            _logger.LogInfo($"Successfully processed order: {data.PrescId}, UniqID: {firstResponse.UniqID}");
                        }
                        else
                        {
                            _logger.LogError($"Order processing failed: {data.PrescId}, Message: {firstResponse.Message}");
                            throw new Exception($"Order processing failed: {firstResponse.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send data to middleware API for new order: {data.PrescId}", ex);
                throw new Exception($"Failed to send data to middleware API: {ex.Message}");
            }
        }

        private void ProcessReplaceOrder(DrugDispenseipd data, HL7Message hl7Message)
        {
            _logger.LogInfo($"Processing replace order for prescription: {data.PrescId}");
            var apiUrl = ConHIS_Service_XPHL7.Configuration.AppConfig.ApiEndpoint;
            var apiMethod = "POST";
            var bodyObj = CreatePrescriptionBody(hl7Message, data);




            var bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.Indented);


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

            // ส่ง API จริงและรับ Response
            try
            {
                var response = _apiService.SendToMiddlewareWithResponse(bodyObj);
                _logger.LogInfo($"API Response: {response}");

                // Parse response เพื่อตรวจสอบสถานะ
                if (!string.IsNullOrEmpty(response))
                {
                    var responseArray = JsonConvert.DeserializeObject<ApiResponse[]>(response);
                    if (responseArray != null && responseArray.Length > 0)
                    {
                        var firstResponse = responseArray[0];
                        _logger.LogInfo($"UniqID: {firstResponse.UniqID}, Status: {firstResponse.Status}, Message: {firstResponse.Message}");

                        if (firstResponse.Status)
                        {
                            _logger.LogInfo($"Successfully processed order: {data.PrescId}, UniqID: {firstResponse.UniqID}");
                        }
                        else
                        {
                            _logger.LogError($"Order processing failed: {data.PrescId}, Message: {firstResponse.Message}");
                            throw new Exception($"Order processing failed: {firstResponse.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send data to middleware API for new order: {data.PrescId}", ex);
                throw new Exception($"Failed to send data to middleware API: {ex.Message}");
            }
        }
        private object CreatePrescriptionBody(HL7Message result, DrugDispenseipd data)
        {

            string FormatDate(DateTime? dt, string fmt, bool forceBuddhistEra = false)
            {
                if (!dt.HasValue || dt.Value == DateTime.MinValue)
                    return null;

                var adjustedDate = dt.Value;
                var year = adjustedDate.Year;

                var currentCulture = System.Globalization.CultureInfo.CurrentCulture;
                bool isBuddhistCalendar = currentCulture.Calendar is System.Globalization.ThaiBuddhistCalendar;

                // แปลงเฉพาะเมื่อไม่ต้องการ BE และปีอยู่ในรูปแบบ BE
                if (year > 2400 && !isBuddhistCalendar && !forceBuddhistEra)
                {
                    adjustedDate = adjustedDate.AddYears(-543);
                    if (adjustedDate.Year <= 0)
                        return null;
                }

                return adjustedDate.ToString(fmt, System.Globalization.CultureInfo.InvariantCulture);
            }

            DateTime? headerDt = result?.MessageHeader != null ? (DateTime?)result.MessageHeader.MessageDateTime : null;


            // คำนวณจำนวนใบยาทั้งหมด
            int totalPrescriptions = result?.PharmacyDispense?.Count() ?? 0;

            // ✅ map ทุก PharmacyDispense พร้อม seq numbering
            var prescriptions = result?.PharmacyDispense?
 .Select((d, index) =>
 {
     // หา RXR ที่ match กับ drug
     var r = result?.RouteInfo?.ElementAtOrDefault(index);
     var n = result?.Notes?.ElementAtOrDefault(index);

     string SafeSubstring(string input, int length)
     {
         if (string.IsNullOrEmpty(input)) return null;
         return input.Substring(0, Math.Min(input.Length, length));
     }

     string SafeJoin(params string[] parts)
     {
         return string.Join(" ", parts.Where(x => !string.IsNullOrWhiteSpace(x)));
     }
     return new
     {
         UniqID = $"{d?.Dispensegivecode?.UniqID ?? ""}-{FormatDate(d?.Prescriptiondate, "yyyyMMdd") ?? ""}",
         f_prescriptionno = result?.CommonOrder?.PlacerOrderNumber,
         f_seq = n?.SetID ?? 0,
         f_seqmax = totalPrescriptions,
         f_prescriptiondate = FormatDate(d?.Prescriptiondate, "yyyyMMdd"),
         f_ordercreatedate = FormatDate(result?.CommonOrder?.TransactionDateTime, "yyyy-MM-dd HH:mm:ss"),
         f_ordertargetdate = FormatDate(headerDt, "yyyy-MM-dd"),
         f_ordertargettime = null as string,
         f_doctorcode = d?.Doctor?.ID ?? null as string,
         f_doctorname = d?.Doctor?.Name ?? null as string,
         f_useracceptby = !string.IsNullOrWhiteSpace(d?.Modifystaff?.StaffName)
                             ? d.Modifystaff.StaffName
                             : !string.IsNullOrWhiteSpace(result?.CommonOrder?.OrderingProvider?.Name)
                                 ? result.CommonOrder.OrderingProvider.Name
                                 : null as string,
         f_orderacceptdate = FormatDate(result?.CommonOrder?.TransactionDateTime, "yyyy-MM-dd HH:mm:ss"),
         f_orderacceptfromip = null as string,
         f_pharmacylocationcode = !string.IsNullOrEmpty(d?.Departmentcode)
                             ? d.Departmentcode.Split(' ')[0]
                             : !string.IsNullOrEmpty(result?.CommonOrder?.EnterersLocation)
                                 ? result.CommonOrder.EnterersLocation.Split(' ')[0]
                                 : null as string,
         f_pharmacylocationdesc = !string.IsNullOrEmpty(d?.Departmentname)
                             ? SafeSubstring(d.Departmentname, 100)
                             : !string.IsNullOrEmpty(result?.CommonOrder?.EnterersLocation)
                                 ? SafeSubstring(result.CommonOrder.EnterersLocation, 100)
                                 : null as string,
         f_prioritycode = !string.IsNullOrEmpty(d?.prioritycode)
     ? d.prioritycode.Substring(0, Math.Min(d.prioritycode.Length, 10)) : d?.RXD31 ?? null as string,
         f_prioritydesc = !string.IsNullOrEmpty(d?.prioritycode)
                             ? SafeSubstring(d.prioritycode, 50)
                             : null as string,
         f_hn = result?.PatientIdentification?.PatientIDExternal ?? null as string,
         f_an = result?.PatientVisit?.VisitNumber ?? null as string,
         f_vn = result?.PatientVisit?.VisitNumber ?? null as string,
         f_title = result?.PatientIdentification?.OfficialName?.Suffix?.Trim() ?? null as string,
         f_patientname = result?.PatientIdentification?.OfficialName != null
                             ? SafeJoin(
                                 result.PatientIdentification.OfficialName.FirstName,
                                 result.PatientIdentification.OfficialName.MiddleName,
                                 result.PatientIdentification.OfficialName.LastName
                               )
                             : result?.CommonOrder?.EnteredBy ?? null as string,
         f_sex = result?.PatientIdentification?.Sex ?? null as string,
         f_patientdob = FormatDate(result?.PatientIdentification?.DateOfBirth, "yyyy-MM-dd"),
         f_wardcode = result?.PatientVisit?.AssignedPatientLocation?.PointOfCare ?? null as string,
         f_warddesc = null as string,
         f_roomcode = null as string,
         f_roomdesc = null as string,
         f_bedcode = null as string,
         f_beddesc = null as string,
         f_right = result?.PatientVisit?.FinancialClass != null
                             ? SafeJoin(result.PatientVisit.FinancialClass.ID, result.PatientVisit.FinancialClass.Name)
                             : null as string,
         f_drugallergy = null as string,
         f_diagnosis = null as string,
         f_orderitemcode = d?.Dispensegivecode?.Identifier ?? null as string,
         f_orderitemname = d?.Dispensegivecode?.DrugName ?? null as string,
         f_orderitemnameTH = d?.Dispensegivecode?.DrugNameThai ?? null as string,
         f_orderitemnamegeneric = null as string,
         f_orderqty = d?.QTY ?? 0,
         f_orderunitcode = d?.Usageunit?.ID ?? null as string,
         f_orderunitdesc = d?.Usageunit?.Name ?? null as string,
         f_dosage = d?.Dose ?? 0,
         f_dosageunit = d?.Usageunit?.Name ?? null as string,
         f_dosagetext = d?.Strengthunit ?? null as string,
         f_drugformcode = d?.Dosageform ?? null as string,
         f_drugformdesc = null as string,
         f_HAD = "0",
         f_narcoticFlg = "0",
         f_psychotropic = "0",
         f_binlocation = null as string,
         f_itemidentify = string.IsNullOrWhiteSpace(d?.Substand?.RXD701) &&
                                          string.IsNullOrWhiteSpace(d?.Substand?.Medicinalproperties) &&
                                          string.IsNullOrWhiteSpace(d?.Substand?.Labelhelp)
                             ? null as string
                             : SafeJoin(d?.Substand?.RXD701, d?.Substand?.Medicinalproperties, d?.Substand?.Labelhelp),
         f_itemlotno = null as string,
         f_itemlotexpire = null as string,
         f_instructioncode = d?.Usagecode?.Instructioncode ?? null as string,
         f_instructiondesc = null as string,
         f_frequencycode = string.IsNullOrWhiteSpace(d?.Usagecode?.Frequencycode)
                              ? null as string
                             : d.Usagecode.Frequencycode,
         f_frequencydesc = string.IsNullOrWhiteSpace(d?.Usagecode?.Frequencydesc)
                             ? null as string
                             : d.Usagecode.Frequencydesc,
         f_timecode = null as string,
         f_timedesc = null as string,
         f_frequencytime = null as string,
         f_dosagedispense = null as string,
         f_dayofweek = null as string,
         f_noteprocessing = !string.IsNullOrWhiteSpace(d?.Substand?.Noteprocessing)
                             ? d.Substand.Noteprocessing
                             : !string.IsNullOrWhiteSpace(d?.RXD33)
                                 ? d.RXD33
                                 : null as string,
         f_prn = "0",
         f_stat = "0",
         f_comment = null as string,
         f_tomachineno = r?.AdministrationDevice ??
                                 (!string.IsNullOrEmpty(d?.Actualdispense) &&
                                  d.Actualdispense.IndexOf("proud", StringComparison.OrdinalIgnoreCase) >= 0
                                      ? 2
                                      : 0),
         f_ipd_order_recordno = null as string,
         f_status = result?.CommonOrder?.OrderControl == "NW" ? "0" :
                    result?.CommonOrder?.OrderControl == "RP" ? "1" : "0",
     };
 })
    .ToArray();
            return new { data = prescriptions ?? Array.Empty<object>() };
        }
    }
}


