using ConHIS_Service_XPHL7.Models;
using ConHIS_Service_XPHL7.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConHIS_Service_XPHL7.Services
{
    // เพิ่ม class สำหรับส่งข้อมูลกลับไปแสดงบน Grid
    public class ProcessResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ApiResponse { get; set; }
        public HL7Message ParsedMessage { get; set; }
        public DrugDispenseipd DispenseData { get; set; }
    }

    public class DrugDispenseProcessor
    {
        private readonly DatabaseService _databaseService;
        private readonly HL7Service _hl7Service;
        private readonly ApiService _apiService;
        private readonly LogManager _logger = new LogManager();

        private List<int> _pendingUpdatedIds = new List<int>();
        private object _pendingUpdateLock = new object();
        public DrugDispenseProcessor(DatabaseService databaseService, HL7Service hl7Service, ApiService apiService)
        {
            _databaseService = databaseService;
            _hl7Service = hl7Service;
            _apiService = apiService;
        }

        public class ApiResponse
        {
            public string UniqID { get; set; }
            public bool Status { get; set; }
            public string Message { get; set; }
        }

        public void RollbackAllPendingUpdates()
        {
            lock (_pendingUpdateLock)
            {
                _logger.LogInfo($"RollbackAllPendingUpdates: Rolling back {_pendingUpdatedIds.Count} records");
                foreach (var id in _pendingUpdatedIds)
                {
                    _databaseService.RollbackReceiveStatus(id);
                    _logger.LogInfo($"RollbackAllPendingUpdates: Rolled back ID {id}");
                }
                _pendingUpdatedIds.Clear();
            }
        }

        public void ClearPendingUpdates()
        {
            lock (_pendingUpdateLock)
            {
                _pendingUpdatedIds.Clear();
            }
        }

        public void ProcessPendingOrders(Action<string> logAction, Action<ProcessResult> onProcessed = null)
        {
            _logger.LogInfo("Start ProcessPendingOrders");

            // ⭐ Reset pending updates list
            lock (_pendingUpdateLock)
            {
                _pendingUpdatedIds.Clear();
            }

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
                        ProcessSingleOrder(data, logAction, onProcessed);
                    }
                    catch (OperationCanceledException)
                    {
                        // ⭐ ถ้า Cancel ให้ throw ออกไป ไม่ update เป็น F
                        _logger.LogInfo($"Processing cancelled for order {data.PrescId}");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        // ⭐ ถ้า Error ปกติให้ update เป็น F แล้วต่อเรื่อย
                        _logger.LogError($"Error processing order {data.PrescId}", ex);
                        logAction($"Error processing order {data.PrescId}: {ex.Message}");

                        onProcessed?.Invoke(new ProcessResult
                        {
                            Success = false,
                            Message = ex.Message,
                            DispenseData = data,
                            ParsedMessage = null
                        });
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInfo("ProcessPendingOrders: Cancelled by user or connection lost");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in ProcessPendingOrders", ex);
                logAction($"Error in ProcessPendingOrders: {ex.Message}");
            }
        }

        private void ProcessSingleOrder(DrugDispenseipd data, Action<string> logAction, Action<ProcessResult> onProcessed)
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

            // Parse HL7 and raw HL7
            HL7Message hl7Message = null;
            try
            {
                hl7Message = _hl7Service.ParseHL7Message(hl7String);
                var orderNo = hl7Message?.CommonOrder?.PlacerOrderNumber;
                _logger.LogInfo($"HL7 raw data saved to hl7_raw/hl7_data_raw_{data.PrescId}.txt");
                _logger.LogRawHL7Data(data.DrugDispenseipdId.ToString(), data.RecieveOrderType.ToString(), hl7String, orderNo, "hl7_raw");
                _logger.LogInfo($"Parsed HL7 message for prescription ID: {data.PrescId}");
                _logger.LogParsedHL7Data(data.DrugDispenseipdId.ToString(), hl7Message, "hl7_parsed");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to write HL7 raw data file for PrescId {data.PrescId}: {ex.Message}");
                var errorMsg = $"Error parsing HL7 for prescription ID: {data.PrescId} - {ex.Message}";

                // ⭐ ถ้าเป็น Error ปกติให้ update เป็น F
                _databaseService.UpdateReceiveStatus(data.DrugDispenseipdId, 'F');
                _logger.LogError(errorMsg, ex);
                logAction(errorMsg);

                onProcessed?.Invoke(new ProcessResult
                {
                    Success = false,
                    Message = errorMsg,
                    DispenseData = data,
                    ParsedMessage = null
                });
                return;
            }

            // Check message format
            if (hl7Message == null || hl7Message.MessageHeader == null)
            {
                var errorFields = new List<string>();
                if (hl7Message == null) errorFields.Add("HL7Message");
                if (hl7Message != null && hl7Message.MessageHeader == null) errorFields.Add("MSH segment");
                var errorMsg = $"Invalid HL7 format for prescription ID: {data.PrescId}. Error fields: {string.Join(", ", errorFields)}";

                // ⭐ ถ้าเป็น Error ปกติให้ update เป็น F
                _databaseService.UpdateReceiveStatus(data.DrugDispenseipdId, 'F');
                _logger.LogError(errorMsg);
                logAction(errorMsg);

                onProcessed?.Invoke(new ProcessResult
                {
                    Success = false,
                    Message = errorMsg,
                    DispenseData = data,
                    ParsedMessage = hl7Message
                });
                return;
            }

            // Determine order type
            var orderControl = !string.IsNullOrEmpty(data.RecieveOrderType) ? data.RecieveOrderType : hl7Message.CommonOrder?.OrderControl;
            _logger.LogInfo($"OrderControl: {orderControl} for prescription ID: {data.PrescId}");

            string apiResponse = null;
            bool success = false;

            try
            {
                if (orderControl == "NW")
                {
                    apiResponse = ProcessNewOrder(data, hl7Message);
                    success = true;

                    // ⭐ ถ้าสำเร็จให้ update เป็น Y แล้ว track ID
                    _databaseService.UpdateReceiveStatus(data.DrugDispenseipdId, 'Y');
                    lock (_pendingUpdateLock)
                    {
                        _pendingUpdatedIds.Add(data.DrugDispenseipdId);
                    }
                    _logger.LogInfo($"Updated receive status for prescription ID: {data.PrescId}");
                }
                else if (orderControl == "RP")
                {
                    apiResponse = ProcessReplaceOrder(data, hl7Message);
                    success = true;

                    // ⭐ ถ้าสำเร็จให้ update เป็น Y แล้ว track ID
                    _databaseService.UpdateReceiveStatus(data.DrugDispenseipdId, 'Y');
                    lock (_pendingUpdateLock)
                    {
                        _pendingUpdatedIds.Add(data.DrugDispenseipdId);
                    }
                    _logger.LogInfo($"Updated receive status for prescription ID: {data.PrescId}");
                }
                else
                {
                    apiResponse = $"Unknown order control: {orderControl}";
                    _logger.LogWarning(apiResponse);
                }

                // ส่งผลลัพธ์กลับไปแสดงบน Grid
                onProcessed?.Invoke(new ProcessResult
                {
                    Success = success,
                    Message = success ? "Processed successfully" : apiResponse,
                    ApiResponse = apiResponse,
                    DispenseData = data,
                    ParsedMessage = hl7Message
                });
            }
            catch (OperationCanceledException)
            {
                // ⭐ ถ้า Cancel ให้ throw ออกไป ไม่ update สถานะ
                _logger.LogInfo($"Processing cancelled for prescription ID: {data.PrescId}");
                throw;
            }
            catch (Exception ex)
            {
                // ⭐ ถ้า Error ปกติให้ update เป็น F
                _logger.LogError($"Error processing order: {data.PrescId}", ex);
                _databaseService.UpdateReceiveStatus(data.DrugDispenseipdId, 'F');

                onProcessed?.Invoke(new ProcessResult
                {
                    Success = false,
                    Message = ex.Message,
                    ApiResponse = null,
                    DispenseData = data,
                    ParsedMessage = hl7Message
                });
            }
        }

        private string ProcessNewOrder(DrugDispenseipd data, HL7Message hl7Message)
        {
            try
            {
                _logger.LogInfo($"Processing new order for prescription: {data.PrescId}");

                var apiUrl = ConHIS_Service_XPHL7.Configuration.AppConfig.ApiEndpoint;
                var apiMethod = "POST";
                var bodyObj = CreatePrescriptionBody(hl7Message, data);
                var bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.Indented);

                _logger.LogInfo($"API URL: {apiUrl}");
                _logger.LogInfo($"API Method: {apiMethod}");
                _logger.LogInfo($"API Body: {bodyJson}");

                var response = _apiService.SendToMiddlewareWithResponse(bodyObj);
                _logger.LogInfo($"API Response: {response}");

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
                            return $"Success: {firstResponse.Message}";
                        }
                        else
                        {
                            _logger.LogError($"Order processing failed: {data.PrescId}, Message: {firstResponse.Message}");
                            throw new Exception($"Order processing failed: {firstResponse.Message}");
                        }
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ProcessNewOrder for {data.PrescId}", ex);
                throw;
            }
        }

        private string ProcessReplaceOrder(DrugDispenseipd data, HL7Message hl7Message)
        {
            try
            {
                _logger.LogInfo($"Processing replace order for prescription: {data.PrescId}");

                var apiUrl = ConHIS_Service_XPHL7.Configuration.AppConfig.ApiEndpoint;
                var apiMethod = "POST";
                var bodyObj = CreatePrescriptionBody(hl7Message, data);
                var bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.Indented);

                _logger.LogInfo($"API URL: {apiUrl}");
                _logger.LogInfo($"API Method: {apiMethod}");
                _logger.LogInfo($"API Body: {bodyJson}");

                var response = _apiService.SendToMiddlewareWithResponse(bodyObj);
                _logger.LogInfo($"API Response: {response}");

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
                            return $"Success: {firstResponse.Message}";
                        }
                        else
                        {
                            _logger.LogError($"Order processing failed: {data.PrescId}, Message: {firstResponse.Message}");
                            throw new Exception($"Order processing failed: {firstResponse.Message}");
                        }
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ProcessReplaceOrder for {data.PrescId}", ex);
                throw;
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

            return new { data = new object[] { } };
        }
    }
}