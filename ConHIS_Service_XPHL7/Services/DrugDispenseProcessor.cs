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
      

        
        private object CreatePrescriptionBody(HL7Message hl7, DrugDispenseipd data)
        {
          
            string FormatDate(DateTime? dt, string fmt)
            {
                return (dt.HasValue && dt.Value != DateTime.MinValue) ? dt.Value.ToString(fmt) : null;
            }

            DateTime? headerDt = hl7?.MessageHeader != null ? (DateTime?)hl7.MessageHeader.MessageDateTime : null;


            // คำนวณจำนวนใบยาทั้งหมด
            int totalPrescriptions = hl7?.PharmacyDispense?.Count() ?? 0;

            // ✅ map ทุก PharmacyDispense พร้อม seq numbering
            var prescriptions = hl7?.PharmacyDispense?
 .Select((d, index) =>
 {
     // หา RXR ที่ match กับ drug
     var r = hl7?.RouteInfo?.ElementAtOrDefault(index);
     var n = hl7?.Notes?.ElementAtOrDefault(index);

     return new
     {
         UniqID = $"{d?.Dispensegivecode?.UniqID ?? ""}-{FormatDate(d?.Prescriptiondate, "yyyyMMdd") ?? null}",
         f_prescriptionno = hl7?.CommonOrder?.PlacerOrderNumber ?? "",
         f_seq = n?.SetID ?? (index + 1),
         f_seqmax = totalPrescriptions,  // ยังไม่เจอ field ใดใน HL7
         f_prescriptiondate = FormatDate(d?.Prescriptiondate, "yyyyMMdd" ?? null),
         f_ordercreatedate = FormatDate(hl7?.CommonOrder.TransactionDateTime, "yyyy-MM-dd HH:mm:ss" ?? null),
         f_ordertargetdate = FormatDate(headerDt, "yyyy-MM-dd") ?? null,// ยังไม่เจอ field ใดใน HL7 
         f_ordertargettime = (string)null,// ยังไม่เจอ field ใดใน HL7
         f_doctorcode = d?.Doctor?.ID ?? null,
         f_doctorname = d?.Doctor?.Name ?? null,
         f_useracceptby = !string.IsNullOrWhiteSpace(d.Modifystaff.StaffName)
                            ? d.Modifystaff.StaffName
                            : !string.IsNullOrWhiteSpace(hl7?.CommonOrder?.OrderingProvider?.Name)
                                ? hl7?.CommonOrder?.OrderingProvider?.Name
                                : null,
         f_orderacceptdate = FormatDate(hl7?.CommonOrder.TransactionDateTime, "yyyy-MM-dd HH:mm:ss") ?? null,
         f_orderacceptfromip = (string)null,// ยังไม่เจอ field ใดใน HL7
         f_pharmacylocationcode = !string.IsNullOrEmpty(d?.Departmentcode)
                                                    ? d.Departmentcode.Split(' ')[0]
                                                         : (!string.IsNullOrEmpty(hl7?.CommonOrder?.EnterersLocation)
                                                            ? hl7.CommonOrder.EnterersLocation.Split(' ')[0]
                                                         : null),
         f_pharmacylocationdesc = !string.IsNullOrEmpty(d?.Departmentname)
    ? d.Departmentname.Substring(0, Math.Min(d.Departmentname.Length, 100))
    : (!string.IsNullOrEmpty(hl7?.CommonOrder?.EnterersLocation)
        ? hl7.CommonOrder.EnterersLocation.Substring(0, Math.Min(hl7.CommonOrder.EnterersLocation.Length, 100))
        : null),
         f_prioritycode = !string.IsNullOrEmpty(d?.prioritycode)
    ? d.prioritycode.Substring(0, Math.Min(d.prioritycode.Length, 10)) : d?.RXD31 ?? null,
         f_prioritydesc = !string.IsNullOrEmpty(d?.prioritycode)
    ? d.prioritycode.Substring(0, Math.Min(d.prioritycode.Length, 50)) : null,
         f_hn = hl7?.PatientIdentification?.PatientIDExternal ?? null,
         f_an = hl7?.PatientVisit?.VisitNumber ?? null,
         f_vn = hl7?.PatientVisit?.VisitNumber ?? null,
         f_title = (hl7?.PatientIdentification?.OfficialName != null)
             ? $"{hl7.PatientIdentification.OfficialName.Suffix}".Trim()
             : null,
         f_patientname = (hl7?.PatientIdentification?.OfficialName != null)
                         ? string.Join(" ", new[]
                         {
                            hl7.PatientIdentification.OfficialName.FirstName,
                            hl7.PatientIdentification.OfficialName.MiddleName,
                            hl7.PatientIdentification.OfficialName.LastName
                         }.Where(x => !string.IsNullOrWhiteSpace(x)))
                         : hl7?.CommonOrder?.EnteredBy ?? null,
         f_sex = hl7?.PatientIdentification?.Sex ?? "",
         f_patientdob = FormatDate(hl7?.PatientIdentification?.DateOfBirth, "yyyy-MM-dd") ?? null,
         f_wardcode = hl7?.PatientVisit?.AssignedPatientLocation?.PointOfCare ?? null,
         f_warddesc = "",// ยังไม่เจอ field ใดใน HL7
         f_roomcode = "",// ยังไม่เจอ field ใดใน HL7
         f_roomdesc = "",// ยังไม่เจอ field ใดใน HL7
         f_bedcode = (string)null,// ยังไม่เจอ field ใดใน HL7
         f_beddesc = (string)null,// ยังไม่เจอ field ใดใน HL7
         f_right = $"{hl7.PatientVisit?.FinancialClass.ID}  {hl7.PatientVisit?.FinancialClass.Name}" ?? null,
         f_drugallergy = (string)null,// ยังไม่เจอ field ใดใน HL7
         f_diagnosis = (string)null,// ยังไม่เจอ field ใดใน HL7
         f_orderitemcode = d?.Dispensegivecode?.Identifier ?? "",
         f_orderitemname = d?.Dispensegivecode?.DrugName ?? "",
         f_orderitemnameTH = d?.Dispensegivecode?.DrugNameThai ?? "",
         f_orderitemnamegeneric = "",// ยังไม่เจอ field ใดใน HL7
         f_orderqty = d?.QTY ?? 0,
         f_orderunitcode = d?.Usageunit?.ID ?? "",
         f_orderunitdesc = d?.Usageunit?.Name ?? "",
         f_dosage = d?.Dose ?? 0,
         f_dosageunit = d?.Usageunit?.Name ?? "",
         f_dosagetext = d?.Strengthunit ?? null,
         f_drugformcode = d?.Dosageform ?? "",
         f_drugformdesc = "",// ยังไม่เจอ field ใดใน HL7
         f_HAD = "0",// ยังไม่เจอ field ใดใน HL7
         f_narcoticFlg = "0",// ยังไม่เจอ field ใดใน HL7
         f_psychotropic = "0",// ยังไม่เจอ field ใดใน HL7
         f_binlocation = (string)null,// ยังไม่เจอ field ใดใน HL7
         f_itemidentify =
    string.IsNullOrWhiteSpace(d?.Substand?.RXD701)
 && string.IsNullOrWhiteSpace(d?.Substand?.Medicinalproperties)
 && string.IsNullOrWhiteSpace(d?.Substand?.Labelhelp)
    ? null
    : $"{d?.Substand?.RXD701 ?? ""} {d?.Substand?.Medicinalproperties ?? ""} {d?.Substand?.Labelhelp ?? ""}".Trim(),
         f_itemlotno = (string)null,// ยังไม่เจอ field ใดใน HL7
         f_itemlotexpire = (string)null,// ยังไม่เจอ field ใดใน HL7
         f_instructioncode = d?.Usagecode?.Instructioncode ?? "",
         f_instructiondesc = "",// ยังไม่เจอ field ใดใน HL7
         f_frequencycode = string.IsNullOrWhiteSpace(d?.Usagecode?.Frequencycode)
                                          ? null
                                          : d.Usagecode.Frequencycode,
         f_frequencydesc = string.IsNullOrWhiteSpace(d?.Usagecode?.Frequencydesc)
                                          ? null
                                          : d.Usagecode.Frequencydesc,
          
         f_timecode = "",// ยังไม่เจอ field ใดใน HL7
         f_timedesc = "",// ยังไม่เจอ field ใดใน HL7
         f_frequencytime = "",// ยังไม่เจอ field ใดใน HL7
         f_dosagedispense = (string)null,// ยังไม่เจอ field ใดใน HL7
         f_dayofweek = (string)null,// ยังไม่เจอ field ใดใน HL7
         f_noteprocessing = !string.IsNullOrWhiteSpace(d?.Substand?.Noteprocessing)
    ? d.Substand.Noteprocessing
    : !string.IsNullOrWhiteSpace(d?.RXD33)
        ? d.RXD33
        : null,
         f_prn = "0",// ยังไม่เจอ field ใดใน HL7
         f_stat = "0",// ยังไม่เจอ field ใดใน HL7
         f_comment = (string)null,// ยังไม่เจอ field ใดใน HL7
         f_tomachineno = r?.AdministrationDevice ??
                (!string.IsNullOrEmpty(d?.Actualdispense) &&
                 d.Actualdispense.IndexOf("proud", StringComparison.OrdinalIgnoreCase) >= 0
                     ? 2
                     : 0),
         f_ipd_order_recordno = (string)null,// ยังไม่เจอ field ใดใน HL7
         f_status = hl7?.CommonOrder?.OrderControl == "NW" ? "0" :
                    hl7?.CommonOrder?.OrderControl == "RP" ? "1" : "0",
     };
 })
 .ToArray();
            return new { data = prescriptions ?? Array.Empty<object>() };
        }

    }
}