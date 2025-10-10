using ConHIS_Service_XPHL7.Configuration;
using ConHIS_Service_XPHL7.Models;
using ConHIS_Service_XPHL7.Services;
using ConHIS_Service_XPHL7.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static System.Net.WebRequestMethods;
using File = System.IO.File;
namespace ConHIS_Service_XPHL7.Services
{
    /// <summary>
    /// ประมวลผลไฟล์ HL7 โดยตรงโดยไม่ต้องผ่านฐานข้อมูล
    /// </summary>
    public class SimpleHL7FileProcessor
    {
        private readonly HL7Service _hl7Service;
        private readonly LogManager _logger;
        private ApiService _apiService;

        public SimpleHL7FileProcessor(string apiEndpoint = null)
        {
            _hl7Service = new HL7Service();
            _logger = new LogManager();

            if (!string.IsNullOrEmpty(apiEndpoint))
            {
                _apiService = new ApiService(apiEndpoint);
            }
        }

        /// <summary>
        /// ประมวลผลไฟล์ HL7 เดี่ยว
        /// </summary>
        /// <param name="filePath">เส้นทางไฟล์ HL7</param>
        /// <returns>HL7Message object หรือ null หากเกิดข้อผิดพลาด</returns>
        public HL7Message ProcessHL7File(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            _logger.LogInfo($"Starting to process HL7 file: {filePath}");
          
            try
            {
                if (!File.Exists(filePath))
                {
                    var errorMsg = $"HL7 file not found: {filePath}";
                    _logger.LogError(errorMsg);
                    return null;
                }

                // ⭐ อ่านไฟล์ก่อน
                var hl7RawData = File.ReadAllText(filePath, Encoding.UTF8);
                _logger.LogInfo($"Successfully read HL7 file: {filePath}, Length: {hl7RawData.Length} characters");
                var parsedMessage = _hl7Service.ParseHL7Message(hl7RawData);
                var orderNo = parsedMessage?.CommonOrder?.PlacerOrderNumber;
                var RecieveOrderType = parsedMessage?.CommonOrder?.OrderControl;
                // ⭐ ตอนนี้มี orderNo แล้ว ค่อยเขียน log
                _logger.LogRawHL7Data(fileName, RecieveOrderType, orderNo, hl7RawData);
                _logger.LogInfo($"Successfully parsed HL7 message for prescription: {orderNo}");
                _logger.LogParsedHL7Data(fileName, parsedMessage);

                return parsedMessage;
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error processing HL7 file {filePath}: {ex.Message}";
                _logger.LogError(errorMsg, ex);
                return null;
            }
        }

        /// <summary>
        /// ประมวลผลและส่งไฟล์ HL7 ไปยัง API
        /// </summary>
        public HL7TestResult ProcessAndSendHL7File(string filePath, bool sendToApi)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var result = new HL7TestResult
            {
                FileName = fileName,
                FilePath = filePath,
                SendToApi = sendToApi
            };

            try
            {
                _logger.LogInfo($"=== Starting HL7 File Processing ===");
                _logger.LogInfo($"File: {filePath}");
                _logger.LogInfo($"Send to API: {sendToApi}");

                // Process HL7 file
                var parsedMessage = ProcessHL7File(filePath);
                if (parsedMessage == null)
                {
                    var err = "Failed to parse HL7 file";
                    _logger.LogError(err);
                    

                    result.Success = false;
                    result.ErrorMessage = err;
                    return result;
                }

                result.ParsedMessage = parsedMessage;
                result.Success = true;

                // Create JSON payload
                var jsonPayload = CreateApiPayload(parsedMessage);
                result.JsonPayload = jsonPayload;

                // Send to API if requested
                if (sendToApi)
                {
                    var apiUrl = $"{AppConfig.ApiEndpoint}";
                    var apiMethod = "POST";
                    var bodyObj = JsonConvert.DeserializeObject(jsonPayload);

                    _logger.LogInfo($"API URL: {apiUrl}");
                    _logger.LogInfo($"API Method: {apiMethod}");
                    _logger.LogInfo($"API Body: {jsonPayload}");

                    var apiService = new ApiService(apiUrl);

                    try
                    {
                        var response = apiService.SendToMiddlewareWithResponse(bodyObj);
                        _logger.LogInfo($"API Response: {response}");

                        result.ApiResponse = response;
                        result.ApiSent = true;

                        if (!string.IsNullOrEmpty(response))
                        {
                            var responseArray = JsonConvert.DeserializeObject<ApiResponseItem[]>(response);
                            if (responseArray != null && responseArray.Length > 0)
                            {
                                foreach (var item in responseArray)
                                {
                                    _logger.LogInfo($"UniqID: {item.UniqID}, Status: {item.Status}, Message: {item.Message}");

                                    if (item.Status)
                                    {
                                        _logger.LogInfo($"Successfully processed order: UniqID: {item.UniqID}");
                                    }
                                    else
                                    {
                                        var err = $"Order processing failed: UniqID: {item.UniqID}, Message: {item.Message}";
                                        _logger.LogError(err);
                                       
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        var err = $"Failed to send data to middleware API: {ex.Message}";
                        var orderNo = parsedMessage?.CommonOrder?.PlacerOrderNumber ;
                        _logger.LogError($"Failed to send data to middleware API for file: {orderNo}",ex);
                     

                        result.Success = false;
                        result.ErrorMessage = err;
                        result.ApiSent = false;
                    }
                }
                else
                {
                    _logger.LogInfo("=== API Send Skipped (Test Mode) ===");
                    result.ApiSent = false;
                }

                return result;
            }
            catch (Exception ex)
            {
                var err = $"Error processing and sending HL7 file: {ex.Message}";
                _logger.LogError(err, ex);
                

                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }



        /// <summary>
        /// สร้าง JSON Payload สำหรับส่งไป API
        /// </summary>
        private string CreateApiPayload(HL7Message result)
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
            int totalPrescriptions = result?.PharmacyDispense?.Count() ?? 0;

            var prescriptions = result?.PharmacyDispense?
                .Select((d, index) =>
                {
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

            var payload = new { data = prescriptions ?? new object[0] };
            return JsonConvert.SerializeObject(payload, Formatting.Indented);
        }

        
        
        /// <summary>
        /// คลาสสำหรับเก็บผลลัพธ์การทดสอบไฟล์ HL7 พร้อมข้อมูล API
        /// </summary>
        public class HL7TestResult
        {
            public string FileName { get; set; }
            public string FilePath { get; set; }
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
            public HL7Message ParsedMessage { get; set; }
            public bool SendToApi { get; set; }
            public bool ApiSent { get; set; }
            public string JsonPayload { get; set; }
            public string ApiResponse { get; set; }
        }
        public class ApiResponseItem
        {
            public string UniqID { get; set; }
            public bool Status { get; set; }
            public string Message { get; set; }
        }
    }
}