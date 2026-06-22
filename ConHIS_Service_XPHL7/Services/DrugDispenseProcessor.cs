using ConHIS_Service_XPHL7.Models;
using ConHIS_Service_XPHL7.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ConHIS_Service_XPHL7.Services
{
    public enum DispenseType
    {
        IPD,
        OPD
    }

    public class ProcessResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ApiResponse { get; set; }
        public HL7Message ParsedMessage { get; set; }
        public object DispenseData { get; set; }
        public DispenseType Type { get; set; }
        public DateTime? RecordDateTime { get; set; }
        public DateTime? DrugDispenseDatetime { get; set; }
    }

    public class DrugDispenseProcessor
    {
        private readonly DatabaseService _databaseService;
        private readonly HL7Service _hl7Service;
        // ⭐ เพิ่ม: HL7ServiceIPD สำหรับ parse IPD ด้วย model แยก
        private readonly HL7ServiceIPD _hl7ServiceIPD;
        private readonly ApiService _apiService;
        private readonly LogManager _logger = new LogManager();
        private readonly EncodingService _encodingService;
        private List<int> _pendingUpdatedIds = new List<int>();
        private object _pendingUpdateLock = new object();

        public DrugDispenseProcessor(DatabaseService databaseService, HL7Service hl7Service, ApiService apiService)
        {
            _databaseService = databaseService;
            _hl7Service = hl7Service;
            // ⭐ สร้าง HL7ServiceIPD ที่นี่ (ไม่ต้องรับจาก constructor เพราะไม่มี dependency)
            _hl7ServiceIPD = new HL7ServiceIPD();
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

        #region IPD Preview Processing

        /// <summary>
        /// ประมวลผล IPD Pending Orders แบบ Preview:
        ///   - Decode HL7
        ///   - Log Raw (hl7_raw_ipd)
        ///   - Parse ด้วย HL7ServiceIPD (model แยก)
        ///   - Log Parsed (hl7_parsed_ipd)
        ///   - อัปเดต DB → 'P'
        ///   - ไม่ส่ง API
        /// </summary>
        public void ProcessPendingIPDOrdersPreview(
            Action<string> logAction,
            Action<ProcessResult> onProcessed = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInfo("Start ProcessPendingIPDOrdersPreview");

            lock (_pendingUpdateLock)
            {
                _pendingUpdatedIds.Clear();
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var pendingData = _databaseService.GetPendingDispenseData();
                _logger.LogInfo($"[IPD Preview] Found {pendingData.Count} pending IPD orders");
                logAction($"[IPD Preview] Found {pendingData.Count} pending IPD orders");

                foreach (var data in pendingData)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        _logger.LogInfo($"[IPD Preview] Processing order {data.PrescId}");
                        ProcessSingleIPDOrderPreview(data, logAction, onProcessed, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInfo($"[IPD Preview] Cancelled for order {data.PrescId}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[IPD Preview] Error processing order {data.PrescId}", ex);
                        logAction($"[IPD Preview] Error: {ex.Message}");

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
                _logger.LogInfo("[IPD Preview] Cancelled by user");
            }
            catch (Exception ex)
            {
                _logger.LogError("[IPD Preview] Critical error in ProcessPendingIPDOrdersPreview", ex);
                logAction($"[IPD Preview] Critical error: {ex.Message}");
            }
        }

        private void ProcessSingleIPDOrderPreview(
            DrugDispenseipd data,
            Action<string> logAction,
            Action<ProcessResult> onProcessed,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // ── 1. Decode HL7 ──────────────────────────────────────────────
            string hl7String = _encodingService.DecodeHl7Data(data.Hl7Data, "OPD");

            if (string.IsNullOrWhiteSpace(hl7String))
            {
                var emptyMsg = $"[IPD Preview] Empty HL7 data for PrescId={data.PrescId}";
                _logger.LogWarning(emptyMsg);
                logAction(emptyMsg);
                _databaseService.UpdateReceiveStatus(data.DrugDispenseipdId, 'F');
                onProcessed?.Invoke(new ProcessResult
                {
                    Success = false,
                    Message = "Empty HL7 data",
                    DispenseData = data,
                    ParsedMessage = null,
                    Type = DispenseType.IPD,
                    DrugDispenseDatetime = data.DrugDispenseDatetime
                });
                return;
            }

            // ── 2. Log Raw ─────────────────────────────────────────────────
            try
            {
                var orderNoRaw = ExtractPlacerOrderNoFromRaw(hl7String);
                _logger.LogRawHL7Data(
                    data.DrugDispenseipdId.ToString(),
                    data.RecieveOrderType ?? "IPD",
                    orderNoRaw,
                    hl7String,
                    "hl7_raw_ipd");
                _logger.LogInfo($"[IPD Preview] Raw logged for ID={data.DrugDispenseipdId}");
            }
            catch (Exception rawEx)
            {
                _logger.LogWarning($"[IPD Preview] Failed to log raw HL7: {rawEx.Message}");
            }

            // ── 3. Parse HL7 ด้วย HL7ServiceIPD ───────────────────────────
            HL7MessageIPD hl7MessageIPD = null;
            HL7Message hl7MessageForUI = null;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                hl7MessageIPD = _hl7ServiceIPD.ParseHL7Message(hl7String);
                var orderNo = hl7MessageIPD?.CommonOrder?.PlacerOrderNumber;
                _logger.LogInfo($"[IPD Preview] Parsed OK - PrescId={data.PrescId}, OrderNo={orderNo}");

                // บันทึก Parsed log (ด้วย IPD model เต็ม)
                _logger.LogParsedHL7Data(
                    data.DrugDispenseipdId.ToString(),
                    hl7MessageIPD,
                    "hl7_parsed_ipd");

                logAction($"[IPD Preview] Parsed OK - OrderNo={orderNo}");

                // Map IPD model → shared HL7Message สำหรับ UI
                hl7MessageForUI = MapIPDToHL7Message(hl7MessageIPD);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInfo($"[IPD Preview] Cancelled during parse for PrescId={data.PrescId}");
                return;
            }
            catch (Exception parseEx)
            {
                var errMsg = $"[IPD Preview] Parse error for PrescId={data.PrescId}: {parseEx.Message}";
                _logger.LogError(errMsg, parseEx);
                _logger.LogReadError(data.DrugDispenseipdId.ToString(),
                    $"Parse Error: {parseEx.Message}\n{parseEx.StackTrace}");
                logAction(errMsg);
                _databaseService.UpdateReceiveStatus(data.DrugDispenseipdId, 'F');
                onProcessed?.Invoke(new ProcessResult
                {
                    Success = false,
                    Message = errMsg,
                    DispenseData = data,
                    ParsedMessage = null,
                    Type = DispenseType.IPD,
                    DrugDispenseDatetime = data.DrugDispenseDatetime
                });
                return;
            }

            // ── 4. ตรวจสอบ MSH ────────────────────────────────────────────
            if (hl7MessageIPD?.MessageHeader == null)
            {
                var errMsg = $"[IPD Preview] Invalid HL7 - MSH missing for PrescId={data.PrescId}";
                _logger.LogError(errMsg);
                logAction(errMsg);
                _databaseService.UpdateReceiveStatus(data.DrugDispenseipdId, 'F');
                onProcessed?.Invoke(new ProcessResult
                {
                    Success = false,
                    Message = errMsg,
                    DispenseData = data,
                    ParsedMessage = hl7MessageForUI,
                    Type = DispenseType.IPD,
                    DrugDispenseDatetime = data.DrugDispenseDatetime
                });
                return;
            }

            // ── 5. อัปเดต DB → 'P' (Preview) ──────────────────────────────
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _databaseService.UpdateReceiveIPDStatusToPreview(data.DrugDispenseipdId);
                _logger.LogInfo($"[IPD Preview] DB updated to 'P' for ID={data.DrugDispenseipdId}");

                lock (_pendingUpdateLock)
                {
                    _pendingUpdatedIds.Add(data.DrugDispenseipdId);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInfo($"[IPD Preview] Cancelled before DB update for PrescId={data.PrescId}");
                return;
            }
            catch (Exception dbEx)
            {
                _logger.LogError($"[IPD Preview] DB update failed for ID={data.DrugDispenseipdId}", dbEx);
                logAction($"[IPD Preview] Warning: DB update failed - {dbEx.Message}");
            }

            // ── 6. ส่งผลกลับ UI ───────────────────────────────────────────
            onProcessed?.Invoke(new ProcessResult
            {
                Success = true,
                Message = "Preview - Parsed & Logged (API not called)",
                ApiResponse = "Preview Only",
                DispenseData = data,
                ParsedMessage = hl7MessageForUI,
                Type = DispenseType.IPD,
                DrugDispenseDatetime = data.DrugDispenseDatetime
            });

            _logger.LogInfo($"[IPD Preview] Done for PrescId={data.PrescId}");
        }

        /// <summary>
        /// Map HL7MessageIPD → HL7Message (shared/OPD model) เพื่อให้ Form1 แสดงได้
        /// </summary>
        private HL7Message MapIPDToHL7Message(HL7MessageIPD src)
        {
            if (src == null) return null;
            var dest = new HL7Message();

            // MSH
            if (src.MessageHeader != null)
            {
                dest.MessageHeader = new MSH
                {
                    EncodingCharacters = src.MessageHeader.EncodingCharacters,
                    SendingApplication = src.MessageHeader.SendingApplication,
                    ReceivingApplication = src.MessageHeader.ReceivingApplication,
                    SendingFacility = src.MessageHeader.SendingFacility,
                    ReceivingFacility = src.MessageHeader.ReceivingFacility,
                    MessageDateTime = src.MessageHeader.MessageDateTime,
                    Security = src.MessageHeader.Security,
                    MessageType = src.MessageHeader.MessageType,
                    MessageControlID = src.MessageHeader.MessageControlID,
                    ProcessingID = src.MessageHeader.ProcessingID,
                    VersionID = src.MessageHeader.VersionID
                };
            }

            // PID
            if (src.PatientIdentification != null)
            {
                var pid = src.PatientIdentification;
                dest.PatientIdentification = new PID
                {
                    SetID = pid.SetID,
                    PatientIDExternal = pid.PatientIDExternal,
                    PatientIDInternal = pid.PatientIDInternal,
                    AlternatePatientID = pid.AlternatePatientID,
                    OfficialName = pid.OfficialName != null ? new PatientName
                    {
                        LastName = pid.OfficialName.LastName,
                        FirstName = pid.OfficialName.FirstName,
                        MiddleName = pid.OfficialName.MiddleName,
                        Suffix = pid.OfficialName.Suffix,
                        Prefix = pid.OfficialName.Prefix,
                        Degree = pid.OfficialName.Degree,
                        NameTypeCode = pid.OfficialName.NameTypeCode,
                        NameRepresentationCode = pid.OfficialName.NameRepresentationCode
                    } : new PatientName(),
                    DateOfBirth = pid.DateOfBirth,
                    Sex = pid.Sex,
                    PhoneNumberHome = pid.PhoneNumberHome,
                    Marital = pid.Marital,
                    Religion = pid.Religion
                };
            }

            // PV1
            if (src.PatientVisit != null)
            {
                var pv1 = src.PatientVisit;
                dest.PatientVisit = new PV1
                {
                    PatientClass = pv1.PatientClass,
                    AssignedPatientLocation = pv1.AssignedPatientLocation != null ? new AssignedLocation
                    {
                        PointOfCare = pv1.AssignedPatientLocation.PointOfCare,
                        Room = pv1.AssignedPatientLocation.Room,
                        Bed = pv1.AssignedPatientLocation.Bed
                    } : new AssignedLocation(),
                    VisitNumber = pv1.VisitNumber,
                    FinancialClass = pv1.FinancialClass != null ? new FinancialClass
                    {
                        ID = pv1.FinancialClass.ID,
                        Name = pv1.FinancialClass.Name
                    } : new FinancialClass(),
                    AdmitDateTime = pv1.AdmitDateTime,
                    AdmittingDoctor = pv1.AdmittingDoctor != null ? new AdmittingDoctor
                    {
                        ID = pv1.AdmittingDoctor.ID,
                        LastName = pv1.AdmittingDoctor.LastName,
                        FirstName = pv1.AdmittingDoctor.FirstName,
                        Prefix = pv1.AdmittingDoctor.Prefix
                    } : new AdmittingDoctor(),
                    PatientType = pv1.PatientType != null ? new PatientType
                    {
                        ID = pv1.PatientType.ID,
                        Name = pv1.PatientType.Name
                    } : new PatientType()
                };
            }

            // ORC
            if (src.CommonOrder != null)
            {
                var orc = src.CommonOrder;
                dest.CommonOrder = new ORC
                {
                    OrderControl = orc.OrderControl,
                    PlacerOrderNumber = orc.PlacerOrderNumber,
                    FillerOrderNumber = orc.FillerOrderNumber,
                    OrderStatus = orc.OrderStatus,
                    TransactionDateTime = orc.TransactionDateTime,
                    EnteredBy = orc.EnteredBy,
                    EnteringDevice = orc.EnteringDevice,
                    EnterersLocation = orc.EnterersLocation,
                    PlacerGroup = orc.PlacerGroup != null ? new PlacerGroup
                    {
                        ID = orc.PlacerGroup.ID,
                        Name = orc.PlacerGroup.Name
                    } : new PlacerGroup(),
                    VerifiedBy = orc.VerifiedBy != null ? new VerifiedBy
                    {
                        ID = orc.VerifiedBy.ID,
                        LastName = orc.VerifiedBy.LastName,
                        FirstName = orc.VerifiedBy.FirstName
                    } : new VerifiedBy(),
                    OrderingProvider = orc.OrderingProvider != null ? new OrderingProvider
                    {
                        ID = orc.OrderingProvider.ID,
                        Name = orc.OrderingProvider.Name
                    } : new OrderingProvider()
                };
            }

            // RXD — IPD: DrugNamePrint → DrugNameThai (ใช้เป็น f_orderitemnameTH)
            if (src.PharmacyDispense != null)
            {
                foreach (var rxdIPD in src.PharmacyDispense)
                {
                    dest.PharmacyDispense.Add(new RXD
                    {
                        IsRXE = rxdIPD.IsRXE,
                        QTY = rxdIPD.QTY,
                        Dispensegivecode = rxdIPD.Dispensegivecode != null ? new Dispensegivecode
                        {
                            Dispense = rxdIPD.Dispensegivecode.Dispense,
                            UniqID = rxdIPD.Dispensegivecode.UniqID,
                            RXD203 = rxdIPD.Dispensegivecode.RXD203,
                            Identifier = rxdIPD.Dispensegivecode.Identifier,
                            DrugName = rxdIPD.Dispensegivecode.DrugName,
                            // IPD: DrugNamePrint เป็น Thai name (ต่างจาก OPD ที่ใช้ DrugNameThai)
                            DrugNameThai = rxdIPD.Dispensegivecode.DrugNamePrint,
                            DrugNamePrint = rxdIPD.Dispensegivecode.DrugNamePrint,
                            DrugUnit = rxdIPD.Dispensegivecode.DrugUnit
                        } : new Dispensegivecode(),
                        DateTimeDispensed = rxdIPD.DateTimeDispensed,
                        RXD4 = rxdIPD.RXD4,
                        Dose = rxdIPD.Dose,
                        Dosageform = rxdIPD.Dosageform,
                        Strengthunit = rxdIPD.Strengthunit,
                        Departmentcode = rxdIPD.Departmentcode,
                        Departmentname = rxdIPD.Departmentname,
                        prioritycode = rxdIPD.prioritycode,
                        Actualdispense = rxdIPD.Actualdispense,
                        Modifystaff = rxdIPD.Modifystaff != null ? new Modifystaff
                        {
                            StaffCode = rxdIPD.Modifystaff.StaffCode,
                            StaffName = rxdIPD.Modifystaff.StaffName
                        } : new Modifystaff(),
                        Doctor = rxdIPD.Doctor != null ? new Doctor
                        {
                            ID = rxdIPD.Doctor.ID,
                            Name = rxdIPD.Doctor.Name
                        } : new Doctor(),
                        Usageunit = rxdIPD.Usageunit != null ? new Usageunit
                        {
                            ID = rxdIPD.Usageunit.ID,
                            Name = rxdIPD.Usageunit.Name
                        } : new Usageunit(),
                        Substand = rxdIPD.Substand != null ? new Substand
                        {
                            RXD701 = rxdIPD.Substand.RXD701,
                            Medicinalproperties = rxdIPD.Substand.Medicinalproperties,
                            Labelhelp = rxdIPD.Substand.Labelhelp,
                            RXD704 = rxdIPD.Substand.RXD704,
                            Usageline1 = rxdIPD.Substand.Usageline1,
                            Usageline2 = rxdIPD.Substand.Usageline2,
                            Usageline3 = rxdIPD.Substand.Usageline3,
                            Noteprocessing = rxdIPD.Substand.Noteprocessing
                        } : new Substand(),
                        Usagecode = rxdIPD.Usagecode != null ? new Usagecode
                        {
                            Instructioncode = rxdIPD.Usagecode.Instructioncode,
                            RXD3002 = rxdIPD.Usagecode.RXD3002,
                            RXD3003 = rxdIPD.Usagecode.RXD3003,
                            Frequencycode = rxdIPD.Usagecode.Frequencycode,
                            Frequencydesc = rxdIPD.Usagecode.Frequencydesc,
                            RXD3006 = rxdIPD.Usagecode.RXD3006,
                            RXD3007 = rxdIPD.Usagecode.RXD3007
                        } : new Usagecode(),
                        Orderunitcode = rxdIPD.Orderunitcode != null ? new Orderunitcode
                        {
                            Nameeng = rxdIPD.Orderunitcode.Nameeng,
                            Namethai = rxdIPD.Orderunitcode.Namethai
                        } : new Orderunitcode(),
                        RXD31 = rxdIPD.RXD31,
                        RXD32 = rxdIPD.RXD32,
                        RXD33 = rxdIPD.RXD33
                    });
                }
            }

            // RXR
            if (src.RouteInfo != null)
            {
                foreach (var rxrIPD in src.RouteInfo)
                {
                    dest.RouteInfo.Add(new RXR
                    {
                        Route = rxrIPD.Route,
                        site = rxrIPD.site,
                        AdministrationDevice = rxrIPD.AdministrationDevice,
                        AdministrationMethod = rxrIPD.AdministrationMethod,
                        RoutingInstruction = rxrIPD.RoutingInstruction
                    });
                }
            }

            // NTE
            if (src.Notes != null)
            {
                foreach (var nteIPD in src.Notes)
                {
                    dest.Notes.Add(new NTE
                    {
                        SetID = nteIPD.SetID,
                        CommentType = nteIPD.CommentType,
                        CommentNote = nteIPD.CommentNote
                    });
                }
            }

            return dest;
        }

        /// <summary>
        /// ดึง PlacerOrderNumber คร่าวๆ จาก raw HL7 string (ก่อน parse เต็ม)
        /// </summary>
        private string ExtractPlacerOrderNoFromRaw(string hl7String)
        {
            try
            {
                var lines = hl7String.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (line.StartsWith("ORC|", StringComparison.OrdinalIgnoreCase))
                    {
                        var fields = line.Split('|');
                        return fields.Length > 2 ? fields[2] : "unknown";
                    }
                }
            }
            catch { /* silent */ }
            return "unknown";
        }

        #endregion

        #region IPD Processing (ส่ง API — รองรับทั้ง NW และ RP)

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

            string hl7String = _encodingService.DecodeHl7Data(data.Hl7Data, "IPD");

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

            // ✅ แก้ไข: ใช้ OrderControl จาก HL7 โดยตรง
            // RecieveOrderType ใน DB เป็น "IPD"/"OPD" ไม่ใช่ "NW"/"RP"
            // ดังนั้นต้องอ่าน OrderControl จาก parsed HL7 เสมอ
            var orderControl = hl7Message.CommonOrder?.OrderControl;
            _logger.LogInfo($"OrderControl (from HL7): {orderControl} for IPD prescription ID: {data.PrescId}");

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
                    lock (_pendingUpdateLock) { _pendingUpdatedIds.Add(data.DrugDispenseipdId); }
                }
                else if (orderControl == "RP")
                {
                    apiResponse = ProcessReplaceOrder(data, hl7Message, DispenseType.IPD);
                    success = true;
                    _databaseService.UpdateReceiveStatus(data.DrugDispenseipdId, 'Y');
                    lock (_pendingUpdateLock) { _pendingUpdatedIds.Add(data.DrugDispenseipdId); }
                }
                else
                {
                    // OrderControl ที่ไม่รองรับ — mark F และ log
                    var warnMsg = $"Unsupported OrderControl '{orderControl}' for IPD prescription ID: {data.PrescId}";
                    _logger.LogWarning(warnMsg);
                    logAction(warnMsg);
                    _databaseService.UpdateReceiveStatus(data.DrugDispenseipdId, 'F');
                }

                onProcessed?.Invoke(new ProcessResult
                {
                    Success = success,
                    Message = success ? "Processed successfully" : $"Unsupported OrderControl: {orderControl}",
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

            // OPD: คงเดิม — ใช้ RecieveOrderType จาก DB ก่อน ถ้าไม่มีค่อยใช้จาก HL7
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
                    lock (_pendingUpdateLock) { _pendingUpdatedIds.Add(data.DrugDispenseopdId); }
                }
                else if (orderControl == "RP")
                {
                    _logger.LogInfo($"OPD RP order - mark F without calling API: {data.PrescId}");
                    _databaseService.UpdateReceiveOpdStatus(data.DrugDispenseopdId, 'F');
                    success = false;
                    apiResponse = "RP not supported for OPD";
                }
                else
                {
                    _logger.LogWarning($"OPD unsupported OrderControl '{orderControl}' for PrescId={data.PrescId} - mark F");
                    _databaseService.UpdateReceiveOpdStatus(data.DrugDispenseopdId, 'F');
                    success = false;
                    apiResponse = $"Unsupported OrderControl: {orderControl}";
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

                int dispenseId = type == DispenseType.IPD
                    ? ((DrugDispenseipd)data).DrugDispenseipdId
                    : ((DrugDispenseopd)data).DrugDispenseopdId;

                var orderNo = hl7Message?.CommonOrder?.PlacerOrderNumber ?? prescId;

                _logger.LogInfo($"Processing new order for {type} prescription: {prescId}");

                var bodyObj = type == DispenseType.IPD
                    ? CreatePrescriptionBodyIPD(hl7Message, data, type)
                    : CreatePrescriptionBody(hl7Message, data, type);

                // ⭐ บันทึก JSON body ก่อนส่ง API
                _logger.LogApiJsonRequest(
                    dispenseId.ToString(),
                    type.ToString(),   // "IPD" หรือ "OPD"
                    orderNo,
                    bodyObj);

                var bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.Indented);
                _logger.LogInfo($"API URL: {ConHIS_Service_XPHL7.Configuration.AppConfig.ApiEndpoint}");
                _logger.LogInfo($"API Body: {bodyJson}");

                var response = _apiService.SendToMiddlewareWithResponse(bodyObj);
                _logger.LogInfo($"API Response: {response}");

                if (!string.IsNullOrEmpty(response))
                {
                    var responseArray = JsonConvert.DeserializeObject<ApiResponse[]>(response);
                    if (responseArray != null && responseArray.Length > 0)
                    {
                        var firstResponse = responseArray[0];
                        if (firstResponse.Status)
                        {
                            _logger.LogInfo($"Successfully processed {type} order: {prescId}");
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

                int dispenseId = type == DispenseType.IPD
                    ? ((DrugDispenseipd)data).DrugDispenseipdId
                    : ((DrugDispenseopd)data).DrugDispenseopdId;

                var orderNo = hl7Message?.CommonOrder?.PlacerOrderNumber ?? prescId;

                _logger.LogInfo($"Processing replace order for {type} prescription: {prescId}");

                var bodyObj = type == DispenseType.IPD
                    ? CreatePrescriptionBodyIPD(hl7Message, data, type)
                    : CreatePrescriptionBody(hl7Message, data, type);

                // ⭐ บันทึก JSON body ก่อนส่ง API
                _logger.LogApiJsonRequest(
                    dispenseId.ToString(),
                    type.ToString(),   // "IPD" หรือ "OPD"
                    orderNo,
                    bodyObj);

                var bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.Indented);
                _logger.LogInfo($"API URL: {ConHIS_Service_XPHL7.Configuration.AppConfig.ApiEndpoint}");
                _logger.LogInfo($"API Body: {bodyJson}");

                var response = _apiService.SendToMiddlewareWithResponse(bodyObj);
                _logger.LogInfo($"API Response: {response}");

                if (!string.IsNullOrEmpty(response))
                {
                    var responseArray = JsonConvert.DeserializeObject<ApiResponse[]>(response);
                    if (responseArray != null && responseArray.Length > 0)
                    {
                        var firstResponse = responseArray[0];
                        if (firstResponse.Status)
                        {
                            _logger.LogInfo($"Successfully processed {type} order: {prescId}");
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

                    var poc = result?.PatientVisit?.AssignedPatientLocation?.PointOfCare?.Trim();
                    var warddesc = result?.PatientVisit?.AssignedPatientLocation?.Room?.Trim();
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
                        f_prioritycode = !string.IsNullOrEmpty(d?.prioritycode) ? SafeSubstring(d.prioritycode, 10) : null as string,
                        f_prioritydesc = !string.IsNullOrEmpty(d?.prioritycode) ? SafeSubstring(d.prioritycode, 50) : null as string,
                        f_hn = result?.PatientIdentification?.PatientIDExternal ?? null as string,
                        f_an = result?.PatientVisit?.VisitNumber ?? null as string,
                        f_vn = result?.PatientVisit?.VisitNumber ?? null as string,
                        f_title = result?.PatientIdentification?.OfficialName?.Suffix?.Trim() ?? null as string,
                        f_patientname = result?.PatientIdentification?.OfficialName != null
                                    ? SafeJoin(
                                        result.PatientIdentification.OfficialName.FirstName,
                                        result.PatientIdentification.OfficialName.MiddleName,
                                        result.PatientIdentification.OfficialName.LastName)
                                    : result?.CommonOrder?.EnteredBy ?? null as string,
                        f_sex = result?.PatientIdentification?.Sex ?? null as string,
                        f_patientdob = FormatDate(result?.PatientIdentification?.DateOfBirth, "yyyy-MM-dd"),
                        f_wardcode = string.IsNullOrWhiteSpace(poc) ? null as string : poc,
                        f_warddesc = string.IsNullOrWhiteSpace(warddesc) ? null as string : warddesc,
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
                        f_itemidentify = string.IsNullOrWhiteSpace(d?.Substand?.RXD701) && string.IsNullOrWhiteSpace(d?.Substand?.Medicinalproperties)
                                    ? null as string
                                    : SafeJoin(d?.Substand?.RXD701, d?.Substand?.Medicinalproperties),
                        f_itemlotno = null as string,
                        f_itemlotexpire = null as string,
                        f_instructioncode = d?.Substand?.RXD704 ?? null as string,
                        f_instructiondesc = instructiondesc.Any() ? string.Join(" ", instructiondesc) : null as string,
                        f_frequencycode = string.IsNullOrWhiteSpace(d?.Usagecode?.Frequencycode) ? null as string : d.Usagecode.Frequencycode,
                        f_frequencydesc = string.IsNullOrWhiteSpace(d?.Usagecode?.Frequencydesc) ? null as string : d.Usagecode.Frequencydesc,
                        f_timecode = null as string,
                        f_timedesc = null as string,
                        f_frequencytime = null as string,
                        f_dosagedispense = null as string,
                        f_dayofweek = null as string,
                        f_noteprocessing = !string.IsNullOrWhiteSpace(d?.Substand?.Noteprocessing)
                                    ? d.Substand.Noteprocessing
                                    : !string.IsNullOrWhiteSpace(d?.RXD33) ? d.RXD33 : null as string,
                        f_prn = "0",
                        f_stat = "0",
                        f_comment = null as string,
                        f_tomachineno = r?.AdministrationDevice ??
                                        (!string.IsNullOrEmpty(d?.Actualdispense) &&
                                         d.Actualdispense.IndexOf("proud", StringComparison.OrdinalIgnoreCase) >= 0 ? 2 : 0),
                        f_ipd_order_recordno = null as string,
                        f_status = result?.CommonOrder?.OrderControl == "NW" ? "0" :
                                   result?.CommonOrder?.OrderControl == "RP" ? "1" : "0",
                        f_io_flag= type == DispenseType.IPD ? "I" : "O",
                        f_dispensestatus = "0",
                    };
                })
                .ToArray();

            return new { data = prescriptions };
        }

        private object CreatePrescriptionBodyIPD(HL7Message result, object data, DispenseType type)
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

                    var instructiondescText = instructiondesc.Any() ? string.Join(" ", instructiondesc) : null;
                    var dosageValue = ExtractDosage(instructiondescText);
                    var poc = result?.PatientVisit?.AssignedPatientLocation?.PointOfCare?.Trim();
                    var warddesc = result?.PatientVisit?.AssignedPatientLocation?.Room?.Trim();
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
                        f_prioritycode = !string.IsNullOrEmpty(d?.prioritycode) ? SafeSubstring(d.prioritycode, 10) : null as string,
                        f_prioritydesc = !string.IsNullOrEmpty(d?.prioritycode) ? SafeSubstring(d.prioritycode, 50) : null as string,
                        f_hn = result?.PatientIdentification?.PatientIDExternal ?? null as string,
                        f_an = result?.PatientVisit?.VisitNumber ?? null as string,
                        f_vn = result?.PatientVisit?.VisitNumber ?? null as string,
                        f_title = result?.PatientIdentification?.OfficialName?.Suffix?.Trim() ?? null as string,
                        f_patientname = result?.PatientIdentification?.OfficialName != null
                                    ? SafeJoin(
                                        result.PatientIdentification.OfficialName.FirstName,
                                        result.PatientIdentification.OfficialName.MiddleName,
                                        result.PatientIdentification.OfficialName.LastName)
                                    : result?.CommonOrder?.EnteredBy ?? null as string,
                        f_sex = result?.PatientIdentification?.Sex ?? null as string,
                        f_patientdob = FormatDate(result?.PatientIdentification?.DateOfBirth, "yyyy-MM-dd"),
                        f_wardcode = string.IsNullOrWhiteSpace(poc) ? null as string : poc,
                        f_warddesc = string.IsNullOrWhiteSpace(warddesc) ? null as string : warddesc,
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
                        f_orderitemnameTH = d?.Dispensegivecode?.DrugNamePrint ?? null as string,
                        f_orderitemnamegeneric = null as string,
                        f_orderqty = d?.QTY ?? 0,
                        f_orderunitcode = d?.Dispensegivecode?.DrugUnit ?? null as string,
                        f_orderunitdesc = d?.Dispensegivecode?.DrugUnit ?? null as string,
                        f_dosage = dosageValue ??  0,
                        f_dosageunit = d?.Usageunit?.Name ?? null as string,
                        f_dosagetext = d?.Strengthunit ?? null as string,
                        f_drugformcode = d?.Dosageform ?? null as string,
                        f_drugformdesc = null as string,
                        f_HAD = "0",
                        f_narcoticFlg = "0",
                        f_psychotropic = "0",
                        f_binlocation = null as string,
                        f_itemidentify = string.IsNullOrWhiteSpace(d?.Substand?.RXD701) && string.IsNullOrWhiteSpace(d?.Substand?.Medicinalproperties)
                                    ? null as string
                                    : SafeJoin(d?.Substand?.RXD701, d?.Substand?.Medicinalproperties),
                        f_itemlotno = null as string,
                        f_itemlotexpire = null as string,
                        f_instructioncode = d?.Substand?.RXD704 ?? null as string,
                        f_instructiondesc = instructiondesc.Any() ? string.Join(" ", instructiondesc) : null as string,
                        f_frequencycode = string.IsNullOrWhiteSpace(d?.Usagecode?.Frequencycode) ? null as string : d.Usagecode.Frequencycode,
                        f_frequencydesc = string.IsNullOrWhiteSpace(d?.Usagecode?.Frequencydesc) ? null as string : d.Usagecode.Frequencydesc,
                        f_timecode = null as string,
                        f_timedesc = null as string,
                        f_frequencytime = null as string,
                        f_dosagedispense = null as string,
                        f_dayofweek = null as string,
                        f_noteprocessing = !string.IsNullOrWhiteSpace(d?.Substand?.Noteprocessing)
                                    ? d.Substand.Noteprocessing
                                    : !string.IsNullOrWhiteSpace(d?.RXD33) ? d.RXD33 : null as string,
                        f_prn = "0",
                        f_stat = "0",
                        f_comment = result?.CommonOrder?.PlacerGroup?.ID ?? null as string,
                        f_tomachineno = (instructiondescText != null &&
                 (instructiondescText.Contains("มีอาการ") || instructiondescText.Contains("เวลามีอาการ")))
                    ? 0
                    : (r?.AdministrationDevice ??
                       (!string.IsNullOrEmpty(d?.Actualdispense) &&
                        d.Actualdispense.IndexOf("proud", StringComparison.OrdinalIgnoreCase) >= 0 ? 2 : 0)),
                        f_ipd_order_recordno = null as string,
                        f_status = result?.CommonOrder?.OrderControl == "NW" ? "0" :
                                   result?.CommonOrder?.OrderControl == "RP" ? "1" : "0",
                        f_io_flag= type == DispenseType.IPD ? "I" : "O",
                        f_dispensestatus = ((dosageValue == null || dosageValue == 0.0 || dosageValue == 0) ? 2 : 0).ToString(),
                    };
                })
                .ToArray();

            return new { data = prescriptions };
        }

        #endregion
        private static double? ExtractDosage(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            if (text.Contains("จากนั้น"))
                return null;

            var matchPos = Regex.Match(text, @"ครั้งละ\s*");

            if (!matchPos.Success)
            {
                var mDirect = Regex.Match(text.Trim(), @"^รับประทานละ\s*(\d+)\s*เม็ด");
                if (mDirect.Success)
                    return double.Parse(mDirect.Groups[1].Value);
                return null;
            }

            string after = text.Substring(matchPos.Index + matchPos.Length);

            if (Regex.IsMatch(after, @"^ครึ่ง-1\s*เม็ด")) return 0.5;
            if (Regex.IsMatch(after, @"^ครึ่ง\s*เม็ด")) return 0.5;
            if (Regex.IsMatch(after, @"^ครึ่ง\s+")) return 0.5;

            var mHalf = Regex.Match(after, @"^(\d+)\s*เม็ดครึ่ง");
            if (mHalf.Success)
                return double.Parse(mHalf.Groups[1].Value) + 0.5;

            if (Regex.IsMatch(after, @"^1/8"))
                return 0.125;

            var mFrac = Regex.Match(after, @"^(\d+)/(\d+)");
            if (mFrac.Success)
                return Math.Round(double.Parse(mFrac.Groups[1].Value) / double.Parse(mFrac.Groups[2].Value), 4);

            var mRange = Regex.Match(after, @"^(\d+)-(\d+)\s*เม็ด");
            if (mRange.Success)
                return double.Parse(mRange.Groups[1].Value);

            var mNum = Regex.Match(after, @"^(\d+(?:\.\d+)?)\s*เม็ด");
            if (mNum.Success)
                return double.Parse(mNum.Groups[1].Value);

            var mNumDay = Regex.Match(after, @"^(\d+(?:\.\d+)?)\s*เม็ดวันละ");
            if (mNumDay.Success)
                return double.Parse(mNumDay.Groups[1].Value);

            if (Regex.IsMatch(after, @"^\s*เม็ด"))
                return 1.0;

            return 0.0;
        }
    }
}