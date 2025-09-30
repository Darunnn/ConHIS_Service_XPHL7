using ConHIS_Service_XPHL7.Models;
using ConHIS_Service_XPHL7.Services;
using ConHIS_Service_XPHL7.Utils;
using ConHIS_Service_XPHL7.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
                // ตรวจสอบว่าไฟล์มีอยู่หรือไม่
                if (!File.Exists(filePath))
                {
                    var errorMsg = $"HL7 file not found: {filePath}";
                    _logger.LogError(errorMsg);
                    _logger.LogReadError(fileName, errorMsg);
                    return null;
                }

                // อ่านข้อมูลจากไฟล์
                var hl7RawData = File.ReadAllText(filePath, Encoding.UTF8);
                _logger.LogInfo($"Successfully read HL7 file: {filePath}, Length: {hl7RawData.Length} characters");

                // บันทึก raw data
                _logger.LogRawHL7Data(fileName, "FILE_PROCESS", hl7RawData);

                // แปลงข้อมูล HL7
                var parsedMessage = _hl7Service.ParseHL7Message(hl7RawData);
                _logger.LogInfo($"Successfully parsed HL7 message for file: {fileName}");

                // บันทึก parsed data
                _logger.LogParsedHL7Data(fileName, parsedMessage);

                // Log ข้อมูลสำคัญ
                LogParsedMessageSummary(fileName, parsedMessage);

                return parsedMessage;
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error processing HL7 file {filePath}: {ex.Message}";
                _logger.LogError(errorMsg, ex);
                _logger.LogReadError(fileName, errorMsg);
                return null;
            }
        }

        /// <summary>
        /// Log ข้อมูลสำคัญของข้อความที่ประมวลผลแล้ว
        /// </summary>
        private void LogParsedMessageSummary(string fileName, HL7Message message)
        {
            if (message == null) return;

            var summary = new StringBuilder();
            summary.AppendLine($"=== HL7 Message Summary for {fileName} ===");

            // ข้อมูลผู้ป่วย
            if (message.PatientIdentification != null)
            {
                summary.AppendLine($"Patient HN: {message.PatientIdentification.PatientIDInternal}");
                summary.AppendLine($"Patient Name: {message.PatientIdentification.OfficialName?.FirstName} {message.PatientIdentification.OfficialName?.LastName}");
                summary.AppendLine($"DOB: {message.PatientIdentification.DateOfBirth:yyyy-MM-dd}");
                summary.AppendLine($"Sex: {message.PatientIdentification.Sex}");
            }

            // ข้อมูลการเยี่ยม
            if (message.PatientVisit != null)
            {
                summary.AppendLine($"Visit Number: {message.PatientVisit.VisitNumber}");
                summary.AppendLine($"Patient Class: {message.PatientVisit.PatientClass}");
            }

            // ข้อมูลยา
            if (message.PharmacyDispense != null && message.PharmacyDispense.Count > 0)
            {
                summary.AppendLine($"Total Drugs: {message.PharmacyDispense.Count}");
                foreach (var drug in message.PharmacyDispense.Take(5)) // แสดงเฉพาะ 5 ตัวแรก
                {
                    summary.AppendLine($"  - Drug: {drug.Dispensegivecode?.DrugName} (Qty: {drug.QTY})");
                }
            }

            // ข้อมูลแพ้ยา
            if (message.Allergies != null && message.Allergies.Count > 0)
            {
                summary.AppendLine($"Allergies: {message.Allergies.Count}");
                foreach (var allergy in message.Allergies)
                {
                    summary.AppendLine($"  - Allergy: {allergy.AllergyName} (Severity: {allergy.AllergySeverity})");
                }
            }

            summary.AppendLine("=== End Summary ===");

            _logger.LogInfo(summary.ToString());
        }

        /// <summary>
        /// Log ข้อมูล JSON ที่จะส่งไป API
        /// </summary>
        private void LogApiRequestData(string fileName, string jsonData)
        {
            try
            {
                var appFolder = AppDomain.CurrentDomain.BaseDirectory ?? Environment.CurrentDirectory;
                var apiLogDir = Path.Combine(appFolder, "api_request");
                Directory.CreateDirectory(apiLogDir);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var apiLogPath = Path.Combine(apiLogDir, $"api_request_{fileName}_{timestamp}.json");

                File.WriteAllText(apiLogPath, jsonData, Encoding.UTF8);
                _logger.LogInfo($"API request JSON saved to: {apiLogPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save API request JSON: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// อ่านและแสดงข้อมูล HL7 แบบ Raw
        /// </summary>
        /// <param name="filePath">เส้นทางไฟล์</param>
        /// <returns>ข้อมูล HL7 แบบ Raw</returns>
        public string ReadHL7RawData(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            _logger.LogInfo($"Reading raw HL7 data from: {filePath}");

            try
            {
                if (!File.Exists(filePath))
                {
                    var errorMsg = $"File not found: {filePath}";
                    _logger.LogError(errorMsg);
                    return null;
                }

                var rawData = File.ReadAllText(filePath, Encoding.UTF8);
                _logger.LogInfo($"Successfully read {rawData.Length} characters from {fileName}");

                return rawData;
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error reading file {filePath}: {ex.Message}";
                _logger.LogError(errorMsg, ex);
                _logger.LogReadError(fileName, errorMsg);
                return null;
            }
        }

        /// <summary>
        /// ประมวลผลและส่งไฟล์ HL7 ไปยัง API พร้อมส่งผลลัพธ์กลับ
        /// </summary>
        /// <param name="filePath">เส้นทางไฟล์</param>
        /// <param name="sendToApi">ส่งไป API หรือไม่</param>
        
        /// <summary>
        /// สร้าง Summary Message สำหรับแสดงผล
        /// </summary>
        public string CreateSummaryMessage(HL7Message result)
        {
            string FormatDate(DateTime? dt, string fmt)
            {
                return (dt.HasValue && dt.Value != DateTime.MinValue) ? dt.Value.ToString(fmt) : null;
            }

            DateTime? headerDt = result?.MessageHeader != null
                ? (DateTime?)result.MessageHeader.MessageDateTime
                : null;

            // คำนวณจำนวนใบยาทั้งหมด
            int totalPrescriptions = result?.PharmacyDispense?.Count() ?? 0;

            // ✅ map ทุก PharmacyDispense พร้อม seq numbering
            var prescriptions = result?.PharmacyDispense?
                .Select((d, index) =>
                {
                    var r = result?.RouteInfo?.ElementAtOrDefault(index);
                    var n = result?.Notes?.ElementAtOrDefault(index);

                    return new
                    {
                        UniqID = $"{d?.Dispensegivecode?.UniqID ?? ""}-{FormatDate(d?.Prescriptiondate, "yyyyMMdd") ?? ""}",
                        f_prescriptionno = result?.CommonOrder?.PlacerOrderNumber ?? "",
                        f_seq = n?.SetID ?? (index + 1),
                        f_seqmax = totalPrescriptions,
                        f_prescriptiondate = FormatDate(d?.Prescriptiondate, "yyyyMMdd"),
                        f_ordercreatedate = FormatDate(result?.CommonOrder.TransactionDateTime, "yyyy-MM-dd HH:mm:ss"),
                        f_ordertargetdate = FormatDate(headerDt, "yyyy-MM-dd"),
                        f_ordertargettime = (string)null,
                        f_doctorcode = d?.Doctor?.ID ?? "",
                        f_doctorname = d?.Doctor?.Name ?? "",
                        f_useracceptby = (d?.Modifystaff != null)
                            ? string.Join(" ", new[] { d.Modifystaff.StaffCode, d.Modifystaff.StaffName }.Where(x => !string.IsNullOrWhiteSpace(x)))
                            : result?.CommonOrder?.OrderingProvider.Name ?? "",
                        f_orderacceptdate = FormatDate(result?.CommonOrder.TransactionDateTime, "yyyy-MM-dd HH:mm:ss"),
                        f_orderacceptfromip = (string)null,
                        f_pharmacylocationcode = !string.IsNullOrEmpty(d?.Departmentcode)
                            ? d.Departmentcode.Substring(0, Math.Min(d.Departmentcode.Length, 20))
                            : (!string.IsNullOrEmpty(result?.CommonOrder?.EnterersLocation)
                                ? result.CommonOrder.EnterersLocation.Substring(0, Math.Min(result.CommonOrder.EnterersLocation.Length, 20))
                                : ""),
                        f_pharmacylocationdesc = !string.IsNullOrEmpty(d?.Departmentname)
                            ? d.Departmentname.Substring(0, Math.Min(d.Departmentname.Length, 100))
                            : (!string.IsNullOrEmpty(result?.CommonOrder?.EnterersLocation)
                                ? result.CommonOrder.EnterersLocation.Substring(0, Math.Min(result.CommonOrder.EnterersLocation.Length, 100))
                                : ""),
                        f_prioritycode = !string.IsNullOrEmpty(d?.prioritycode)
                            ? d.prioritycode.Substring(0, Math.Min(d.prioritycode.Length, 10))
                            : d?.RXD31 ?? "",
                        f_prioritydesc = !string.IsNullOrEmpty(d?.prioritycode)
                            ? d.prioritycode.Substring(0, Math.Min(d.prioritycode.Length, 50))
                            : "",
                        f_hn = result?.PatientIdentification?.PatientIDExternal ?? "",
                        f_an = result?.PatientVisit?.VisitNumber ?? "",
                        f_vn = result?.PatientVisit?.VisitNumber ?? "",
                        f_title = result?.PatientIdentification?.OfficialName?.Suffix?.Trim() ?? "",
                        f_patientname = (result?.PatientIdentification?.OfficialName != null)
                            ? string.Join(" ", new[] {
                           result.PatientIdentification.OfficialName.FirstName,
                           result.PatientIdentification.OfficialName.MiddleName,
                           result.PatientIdentification.OfficialName.LastName
                              }.Where(x => !string.IsNullOrWhiteSpace(x)))
                            : result?.CommonOrder?.EnteredBy ?? "",
                        f_sex = result?.PatientIdentification?.Sex ?? "",
                        f_patientdob = FormatDate(result?.PatientIdentification?.DateOfBirth, "yyyy-MM-dd"),
                        f_wardcode = result?.PatientVisit?.AssignedPatientLocation?.PointOfCare ?? "",
                        f_warddesc = "",
                        f_roomcode = "",
                        f_roomdesc = "",
                        f_bedcode = (string)null,
                        f_beddesc = (string)null,
                        f_right = result?.PatientVisit?.FinancialClass != null
                            ? $"{result.PatientVisit.FinancialClass.ID} {result.PatientVisit.FinancialClass.Name}"
                            : null,
                        f_drugallergy = (string)null,
                        f_diagnosis = (string)null,
                        f_orderitemcode = d?.Dispensegivecode?.Identifier ?? "",
                        f_orderitemname = d?.Dispensegivecode?.DrugName ?? "",
                        f_orderitemnameTH = d?.Dispensegivecode?.DrugNameThai ?? "",
                        f_orderitemnamegeneric = "",
                        f_orderqty = d?.QTY ?? 0,
                        f_orderunitcode = d?.Usageunit?.ID ?? "",
                        f_orderunitdesc = d?.Usageunit?.Name ?? "",
                        f_dosage = d?.Dose ?? 0,
                        f_dosageunit = d?.Usageunit?.Name ?? "",
                        f_dosagetext = d?.Strengthunit ?? null,
                        f_drugformcode = d?.Dosageform ?? "",
                        f_drugformdesc = "",
                        f_HAD = "0",
                        f_narcoticFlg = "0",
                        f_psychotropic = "0",
                        f_binlocation = (string)null,
                        f_itemidentify = (d?.Substand != null)
                            ? $"{d.Substand.RXD701} {d.Substand.Medicinalproperties} {d.Substand.Labelhelp}".Trim()
                            : null,
                        f_itemlotno = (string)null,
                        f_itemlotexpire = (string)null,
                        f_instructioncode = d?.Usagecode?.Instructioncode ?? "",
                        f_instructiondesc = "",
                        f_frequencycode = d?.Usagecode?.Frequencycode ?? "",
                        f_frequencydesc = d?.Usagecode?.Frequencydesc ?? "",
                        f_timecode = "",
                        f_timedesc = "",
                        f_frequencytime = "",
                        f_dosagedispense = "",
                        f_dayofweek = (string)null,
                        f_noteprocessing = !string.IsNullOrWhiteSpace(d?.Substand?.Noteprocessing)
                            ? d.Substand.Noteprocessing
                            : !string.IsNullOrWhiteSpace(d?.RXD33)
                                ? d.RXD33
                                : null,
                        f_prn = "0",
                        f_stat = "0",
                        f_comment = (string)null,
                        f_tomachineno = r?.AdministrationDevice
                            ?? (!string.IsNullOrEmpty(d?.Actualdispense) &&
                                d.Actualdispense.IndexOf("proud", StringComparison.OrdinalIgnoreCase) >= 0
                                    ? "2"
                                    : "0"),
                        f_ipd_order_recordno = (string)null,
                        f_status = result?.CommonOrder?.OrderControl == "NW" ? "0" :
                                   result?.CommonOrder?.OrderControl == "RP" ? "1" : "0",
                    };
                })
                .ToArray();

            // 👉 ถ้า prescriptions ไม่มีข้อมูล → ไปอ่านจากไฟล์ txt
            if (prescriptions == null || prescriptions.Length == 0)
            {
                string txtPath = "prescriptions.txt";
                if (File.Exists(txtPath))
                {
                    return File.ReadAllText(txtPath);
                }
                else
                {
                    return "{\"data\": []}";
                }
            }

            // ✅ ถ้ามี prescriptions ให้ serialize เป็น JSON
            var payload = new { data = prescriptions };
            return JsonConvert.SerializeObject(payload, Formatting.Indented);
        }

        public void Dispose()
        {
            _apiService?.Dispose();
        }
    }

    /// <summary>
    /// คลาสสำหรับเก็บผลลัพธ์การทดสอบไฟล์ HL7 พร้อมข้อมูล API
    /// </summary>
   
}