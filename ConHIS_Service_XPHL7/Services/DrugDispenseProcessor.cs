using ConHIS_Service_XPHL7.Models;
using ConHIS_Service_XPHL7.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ConHIS_Service_XPHL7.Services
{
    // เพิ่ม enum สำหรับระบุประเภท
    public enum DispenseType
    {
        IPD,
        OPD
    }

    // เพิ่ม class สำหรับส่งข้อมูลกลับไปแสดงบน Grid
    public class ProcessResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ApiResponse { get; set; }
        public HL7Message ParsedMessage { get; set; }
        public object DispenseData { get; set; } // เปลี่ยนเป็น object เพื่อรองรับทั้ง IPD และ OPD
        public DispenseType Type { get; set; }
        public DateTime? RecordDateTime { get; set; }
        public DateTime? DrugDispenseDatetime { get; set; }  // จาก drug_dispense_datetime
        
    }

    public class DrugDispenseProcessor
    {
        private readonly DatabaseService _databaseService;
        private readonly HL7Service _hl7Service;
        private readonly ApiService _apiService;
        private readonly LogManager _logger = new LogManager();
        private readonly EncodingService _encodingService;
        private List<int> _pendingUpdatedIds = new List<int>();
        private object _pendingUpdateLock = new object();

        public DrugDispenseProcessor(DatabaseService databaseService, HL7Service hl7Service, ApiService apiService)
        {
            _databaseService = databaseService;
            _hl7Service = hl7Service;
            _apiService = apiService;
            _encodingService = EncodingService.FromConnectionConfig(
            msg => _logger.LogInfo(msg)
        );
        }

        public class ApiResponse
        {
            public string UniqID { get; set; }
            public bool Status { get; set; }
            public string Message { get; set; }
        }

        #region IPD Processing

        public void ProcessPendingOrders(
            Action<string> logAction,
            Action<ProcessResult> onProcessed = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInfo("Start ProcessPendingOrders (IPD)");

            lock (_pendingUpdateLock)
            {
                _pendingUpdatedIds.Clear();
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var pendingData = _databaseService.GetPendingDispenseData();
                _logger.LogInfo($"Found {pendingData.Count} pending IPD orders");
                logAction($"Found {pendingData.Count} pending IPD orders");

                foreach (var data in pendingData)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        _logger.LogInfo($"Processing IPD order {data.PrescId}");
                        ProcessSingleOrder(data, logAction, onProcessed, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInfo($"Processing cancelled for IPD order {data.PrescId}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error processing IPD order {data.PrescId}", ex);
                        logAction($"Error processing IPD order {data.PrescId}: {ex.Message}");

                        onProcessed?.Invoke(new ProcessResult
                        {
                            Success = false,
                            Message = ex.Message,
                            DispenseData = data,
                            ParsedMessage = null,
                            Type = DispenseType.IPD,
                            DrugDispenseDatetime = data.DrugDispenseDatetime
                        });
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInfo("ProcessPendingOrders (IPD): Cancelled by user");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in ProcessPendingOrders (IPD)", ex);
                logAction($"Error in ProcessPendingOrders (IPD): {ex.Message}");
            }
        }

        private void ProcessSingleOrder(
            DrugDispenseipd data,
            Action<string> logAction,
            Action<ProcessResult> onProcessed,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Convert byte array to string

            string hl7String = _encodingService.DecodeHl7Data(data.Hl7Data);



            // Parse HL7
            HL7Message hl7Message = null;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                hl7Message = _hl7Service.ParseHL7Message(hl7String);
                var orderNo = hl7Message?.CommonOrder?.PlacerOrderNumber;
                _logger.LogInfo($"HL7 raw data saved to hl7_raw/hl7_data_raw_{data.PrescId}.txt");
                _logger.LogRawHL7Data(data.DrugDispenseipdId.ToString(), data.RecieveOrderType?.ToString(), orderNo, hl7String, "hl7_raw_ipd");
                _logger.LogInfo($"Parsed HL7 message for IPD prescription ID: {data.PrescId}");
                _logger.LogParsedHL7Data(data.DrugDispenseipdId.ToString(), hl7Message, "hl7_parsed_ipd");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInfo($"Processing cancelled during HL7 parsing for IPD order {data.PrescId}");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to write HL7 raw data file for IPD PrescId {data.PrescId}: {ex.Message}");
                var errorMsg = $"Error parsing HL7 for IPD prescription ID: {data.PrescId} - {ex.Message}";

                _databaseService.UpdateReceiveStatus(data.DrugDispenseipdId, 'F');
                _logger.LogError(errorMsg, ex);
                logAction(errorMsg);

                onProcessed?.Invoke(new ProcessResult
                {
                    Success = false,
                    Message = errorMsg,
                    DispenseData = data,
                    ParsedMessage = null,
                    Type = DispenseType.IPD,
                    DrugDispenseDatetime = data.DrugDispenseDatetime
                });
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Check message format
            if (hl7Message == null || hl7Message.MessageHeader == null)
            {
                var errorFields = new List<string>();
                if (hl7Message == null) errorFields.Add("HL7Message");
                if (hl7Message != null && hl7Message.MessageHeader == null) errorFields.Add("MSH segment");
                var errorMsg = $"Invalid HL7 format for IPD prescription ID: {data.PrescId}. Error fields: {string.Join(", ", errorFields)}";

                _databaseService.UpdateReceiveStatus(data.DrugDispenseipdId, 'F');
                _logger.LogError(errorMsg);
                logAction(errorMsg);

                onProcessed?.Invoke(new ProcessResult
                {
                    Success = false,
                    Message = errorMsg,
                    DispenseData = data,
                    ParsedMessage = hl7Message,
                    Type = DispenseType.IPD,
                    DrugDispenseDatetime = data.DrugDispenseDatetime

                });
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Determine order type
            var orderControl = !string.IsNullOrEmpty(data.RecieveOrderType) ? data.RecieveOrderType : hl7Message.CommonOrder?.OrderControl;
            _logger.LogInfo($"OrderControl: {orderControl} for IPD prescription ID: {data.PrescId}");

            string apiResponse = null;
            bool success = false;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (orderControl == "NW")
                {
                    apiResponse = ProcessNewOrder(data, hl7Message, DispenseType.IPD);
                    success = true;

                    _databaseService.UpdateReceiveStatus(data.DrugDispenseipdId, 'Y');
                    lock (_pendingUpdateLock)
                    {
                        _pendingUpdatedIds.Add(data.DrugDispenseipdId);
                    }
                }
                else if (orderControl == "RP")
                {
                    apiResponse = ProcessReplaceOrder(data, hl7Message, DispenseType.IPD);
                    success = true;

                    _databaseService.UpdateReceiveStatus(data.DrugDispenseipdId, 'Y');
                    lock (_pendingUpdateLock)
                    {
                        _pendingUpdatedIds.Add(data.DrugDispenseipdId);
                    }
                }

                onProcessed?.Invoke(new ProcessResult
                {
                    Success = success,
                    Message = success ? "Processed successfully" : apiResponse,
                    ApiResponse = apiResponse,
                    DispenseData = data,
                    ParsedMessage = hl7Message,
                    Type = DispenseType.IPD,
                    DrugDispenseDatetime = data.DrugDispenseDatetime
                });
            }
            catch (OperationCanceledException)
            {
                _logger.LogInfo($"Processing cancelled for IPD prescription ID: {data.PrescId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing IPD order: {data.PrescId}", ex);
                _databaseService.UpdateReceiveStatus(data.DrugDispenseipdId, 'F');

                onProcessed?.Invoke(new ProcessResult
                {
                    Success = false,
                    Message = ex.Message,
                    ApiResponse = null,
                    DispenseData = data,
                    ParsedMessage = hl7Message,
                    Type = DispenseType.IPD,
                    DrugDispenseDatetime = data.DrugDispenseDatetime
                });
            }
        }

        #endregion

        #region OPD Processing

        public void ProcessPendingOpdOrders(
            Action<string> logAction,
            Action<ProcessResult> onProcessed = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInfo("Start ProcessPendingOrders (OPD)");

            lock (_pendingUpdateLock)
            {
                _pendingUpdatedIds.Clear();
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var pendingData = _databaseService.GetPendingDispenseOpdData();
                _logger.LogInfo($"Found {pendingData.Count} pending OPD orders");
                logAction($"Found {pendingData.Count} pending OPD orders");

                foreach (var data in pendingData)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        _logger.LogInfo($"Processing OPD order {data.PrescId}");
                        ProcessSingleOpdOrder(data, logAction, onProcessed, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInfo($"Processing cancelled for OPD order {data.PrescId}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error processing OPD order {data.PrescId}", ex);
                        logAction($"Error processing OPD order {data.PrescId}: {ex.Message}");

                        onProcessed?.Invoke(new ProcessResult
                        {
                            Success = false,
                            Message = ex.Message,
                            DispenseData = data,
                            ParsedMessage = null,
                            Type = DispenseType.OPD,
                            DrugDispenseDatetime = data.DrugDispenseDatetime
                        });
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInfo("ProcessPendingOrders (OPD): Cancelled by user");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in ProcessPendingOrders (OPD)", ex);
                logAction($"Error in ProcessPendingOrders (OPD): {ex.Message}");
            }
        }

        private void ProcessSingleOpdOrder(
            DrugDispenseopd data,
            Action<string> logAction,
            Action<ProcessResult> onProcessed,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();


            string hl7String = _encodingService.DecodeHl7Data(data.Hl7Data);

            // Parse HL7
            HL7Message hl7Message = null;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                hl7Message = _hl7Service.ParseHL7Message(hl7String);
                var orderNo = hl7Message?.CommonOrder?.PlacerOrderNumber;
                _logger.LogInfo($"HL7 raw data saved to hl7_raw/hl7_data_raw_opd_{data.PrescId}.log");
                _logger.LogRawHL7Data(data.DrugDispenseopdId.ToString(), data.RecieveOrderType?.ToString(), orderNo, hl7String, "hl7_raw_opd");
                _logger.LogInfo($"Parsed HL7 message for OPD prescription ID: {data.PrescId}");
                _logger.LogParsedHL7Data(data.DrugDispenseopdId.ToString(), hl7Message, "hl7_parsed_opd");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInfo($"Processing cancelled during HL7 parsing for OPD order {data.PrescId}");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to write HL7 raw data file for OPD PrescId {data.PrescId}: {ex.Message}");
                var errorMsg = $"Error parsing HL7 for OPD prescription ID: {data.PrescId} - {ex.Message}";

                _databaseService.UpdateReceiveOpdStatus(data.DrugDispenseopdId, 'F');
                _logger.LogError(errorMsg, ex);
                logAction(errorMsg);

                onProcessed?.Invoke(new ProcessResult
                {
                    Success = false,
                    Message = errorMsg,
                    DispenseData = data,
                    ParsedMessage = null,
                    Type = DispenseType.OPD,
                    DrugDispenseDatetime = data.DrugDispenseDatetime
                });
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Check message format
            if (hl7Message == null || hl7Message.MessageHeader == null)
            {
                var errorFields = new List<string>();
                if (hl7Message == null) errorFields.Add("HL7Message");
                if (hl7Message != null && hl7Message.MessageHeader == null) errorFields.Add("MSH segment");
                var errorMsg = $"Invalid HL7 format for OPD prescription ID: {data.PrescId}. Error fields: {string.Join(", ", errorFields)}";

                _databaseService.UpdateReceiveOpdStatus(data.DrugDispenseopdId, 'F');
                _logger.LogError(errorMsg);
                logAction(errorMsg);

                onProcessed?.Invoke(new ProcessResult
                {
                    Success = false,
                    Message = errorMsg,
                    DispenseData = data,
                    ParsedMessage = hl7Message,
                    Type = DispenseType.OPD,
                    DrugDispenseDatetime = data.DrugDispenseDatetime
                });
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Determine order type
            var orderControl = !string.IsNullOrEmpty(data.RecieveOrderType) ? data.RecieveOrderType : hl7Message.CommonOrder?.OrderControl;
            _logger.LogInfo($"OrderControl: {orderControl} for OPD prescription ID: {data.PrescId}");

            string apiResponse = null;
            bool success = false;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (orderControl == "NW")
                {
                    apiResponse = ProcessNewOrder(data, hl7Message, DispenseType.OPD);
                    success = true;

                    _databaseService.UpdateReceiveOpdStatus(data.DrugDispenseopdId, 'Y');
                    lock (_pendingUpdateLock)
                    {
                        _pendingUpdatedIds.Add(data.DrugDispenseopdId);
                    }
                }
                else if (orderControl == "RP")
                {
                    apiResponse = ProcessReplaceOrder(data, hl7Message, DispenseType.OPD);
                    success = true;

                    _databaseService.UpdateReceiveOpdStatus(data.DrugDispenseopdId, 'Y');
                    lock (_pendingUpdateLock)
                    {
                        _pendingUpdatedIds.Add(data.DrugDispenseopdId);
                    }
                }

                onProcessed?.Invoke(new ProcessResult
                {
                    Success = success,
                    Message = success ? "Processed successfully" : apiResponse,
                    ApiResponse = apiResponse,
                    DispenseData = data,
                    ParsedMessage = hl7Message,
                    Type = DispenseType.OPD,
                    DrugDispenseDatetime = data.DrugDispenseDatetime
                });
            }
            catch (OperationCanceledException)
            {
                _logger.LogInfo($"Processing cancelled for OPD prescription ID: {data.PrescId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing OPD order: {data.PrescId}", ex);
                _databaseService.UpdateReceiveOpdStatus(data.DrugDispenseopdId, 'F');

                onProcessed?.Invoke(new ProcessResult
                {
                    Success = false,
                    Message = ex.Message,
                    ApiResponse = null,
                    DispenseData = data,
                    ParsedMessage = hl7Message,
                    Type = DispenseType.OPD,
                    DrugDispenseDatetime = data.DrugDispenseDatetime
                });
            }
        }

        #endregion

        #region Shared Processing Methods

        private string ProcessNewOrder(object data, HL7Message hl7Message, DispenseType type)
        {
            try
            {
                string prescId = type == DispenseType.IPD
                    ? ((DrugDispenseipd)data).PrescId
                    : ((DrugDispenseopd)data).PrescId;

                _logger.LogInfo($"Processing new order for {type} prescription: {prescId}");

                var apiUrl = ConHIS_Service_XPHL7.Configuration.AppConfig.ApiEndpoint;
                var apiMethod = "POST";
                var bodyObj = CreatePrescriptionBody(hl7Message, data, type);
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
                            _logger.LogInfo($"Successfully processed {type} order: {prescId}, UniqID: {firstResponse.UniqID}");
                            return $"Success: {firstResponse.Message}";
                        }
                        else
                        {
                            _logger.LogError($"{type} order processing failed: {prescId}, Message: {firstResponse.Message}");
                            throw new Exception($"Order processing failed: {firstResponse.Message}");
                        }
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                string prescId = type == DispenseType.IPD
                    ? ((DrugDispenseipd)data).PrescId
                    : ((DrugDispenseopd)data).PrescId;
                _logger.LogError($"Error in ProcessNewOrder for {type} {prescId}", ex);
                throw;
            }
        }

        private string ProcessReplaceOrder(object data, HL7Message hl7Message, DispenseType type)
        {
            try
            {
                string prescId = type == DispenseType.IPD
                    ? ((DrugDispenseipd)data).PrescId
                    : ((DrugDispenseopd)data).PrescId;

                _logger.LogInfo($"Processing replace order for {type} prescription: {prescId}");

                var apiUrl = ConHIS_Service_XPHL7.Configuration.AppConfig.ApiEndpoint;
                var apiMethod = "POST";
                var bodyObj = CreatePrescriptionBody(hl7Message, data, type);
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
                            _logger.LogInfo($"Successfully processed {type} order: {prescId}, UniqID: {firstResponse.UniqID}");
                            return $"Success: {firstResponse.Message}";
                        }
                        else
                        {
                            _logger.LogError($"{type} order processing failed: {prescId}, Message: {firstResponse.Message}");
                            throw new Exception($"Order processing failed: {firstResponse.Message}");
                        }
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                string prescId = type == DispenseType.IPD
                    ? ((DrugDispenseipd)data).PrescId
                    : ((DrugDispenseopd)data).PrescId;
                _logger.LogError($"Error in ProcessReplaceOrder for {type} {prescId}", ex);
                throw;
            }
        }

        private object CreatePrescriptionBody(HL7Message result, object data, DispenseType type)
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
                    var instructiondesc = new[] {
                     d?.Substand?.Usageline1,
                 d?.Substand?.Usageline2,
                     d?.Substand?.Usageline3
                    }.Where(x => !string.IsNullOrWhiteSpace(x));
                    return new
                    {
                        UniqID = $"{d?.Dispensegivecode?.UniqID ?? ""}-{DateTime.Now.ToString("yyyyMMdd")}",
                        f_prescriptionno = result?.CommonOrder?.PlacerOrderNumber,
                        f_seq = n?.SetID ?? 0,
                        f_seqmax = totalPrescriptions,
                        f_prescriptiondate = (FormatDate(result?.MessageHeader.MessageDateTime, "yyyyMMdd") ?? DateTime.Now.ToString("yyyyMMdd")),
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
                        f_orderacceptdate = FormatDate(result?.MessageHeader?.MessageDateTime, "yyyy-MM-dd HH:mm:ss"),
                        f_orderacceptfromip = null as string,
                        f_pharmacylocationcode = !string.IsNullOrEmpty(d?.Departmentcode)
    ? d.Departmentcode.Split('^')[0]
    : !string.IsNullOrEmpty(result?.CommonOrder?.EnterersLocation)
        ? result.CommonOrder.EnterersLocation.Split('^')[0]
        : null as string,

                        f_pharmacylocationdesc = !string.IsNullOrEmpty(d?.Departmentname)
    ? SafeSubstring(d.Departmentname, 100)
    : !string.IsNullOrEmpty(result?.CommonOrder?.EnterersLocation) && result.CommonOrder.EnterersLocation.Contains('^')
        ? result.CommonOrder.EnterersLocation.Split('^')[1]
        : null as string,
                        f_prioritycode = !string.IsNullOrEmpty(d?.prioritycode)
                                    ? SafeSubstring(d.prioritycode, 10)
                                    : null as string,
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
                        f_orderunitcode = d?.Dispensegivecode?.DrugUnit ?? null as string,
                        f_orderunitdesc = d?.Dispensegivecode?.DrugUnit ?? null as string,
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
                                                 string.IsNullOrWhiteSpace(d?.Substand?.Medicinalproperties)

                                    ? null as string
                                    : SafeJoin(d?.Substand?.RXD701, d?.Substand?.Medicinalproperties),
                        f_itemlotno = null as string,
                        f_itemlotexpire = null as string,
                        f_instructioncode = d?.Substand?.RXD704 ?? null as string,

                        f_instructiondesc = instructiondesc.Any()
                                            ? string.Join(" ", instructiondesc)
                                            : null as string,
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

            return new { data = prescriptions };
        }

        #endregion
    }
}