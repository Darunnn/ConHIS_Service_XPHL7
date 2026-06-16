using ConHIS_Service_XPHL7.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConHIS_Service_XPHL7.Services
{
    public class HL7ServiceIPD
    {
        private const string FIELD_SEPARATOR = "|";
        private const string COMPONENT_SEPARATOR = "^";

        public HL7MessageIPD ParseHL7Message(string hl7Data)
        {
            try
            {
                var lines = hl7Data.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                var message = new HL7MessageIPD();

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line.Length < 3) continue;

                    var segmentType = line.Substring(0, 3).ToUpperInvariant();
                    var fields = line.Split(FIELD_SEPARATOR[0]);

                    switch (segmentType)
                    {
                        case "MSH":
                            message.MessageHeader = ParseMSH(fields);
                            break;
                        case "PID":
                            message.PatientIdentification = ParsePID(fields);
                            break;
                        case "PV1":
                            message.PatientVisit = ParsePV1(fields);
                            break;
                        case "ORC":
                            message.CommonOrder = ParseORC(fields);
                            break;
                        case "AL1":
                            var allergy = ParseAL1(fields);
                            if (allergy != null)
                                message.Allergies.Add(allergy);
                            break;
                        case "RXE":
                            var rxe = ParseRXE(fields);
                            if (rxe != null)
                                message.PharmacyDispense.Add(rxe);
                            break;
                        case "RXD":
                            var dispense = ParseRXD(fields);
                            if (dispense != null)
                                message.PharmacyDispense.Add(dispense);
                            break;
                        case "RXR":
                            var route = ParseRXR(fields);
                            if (route != null)
                                message.RouteInfo.Add(route);
                            break;
                        case "NTE":
                            var note = ParseNTE(fields);
                            if (note != null)
                                message.Notes.Add(note);
                            break;
                        default:
                            continue;
                    }
                }

                return message;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing IPD HL7 message: {ex.Message}", ex);
            }
        }

        #region Parse Segments

        private MSH_IPD ParseMSH(string[] fields)
        {
            return new MSH_IPD
            {
                EncodingCharacters = GetField(fields, 1),
                SendingApplication = GetField(fields, 2),
                ReceivingApplication = GetField(fields, 3),
                SendingFacility = GetField(fields, 4),
                ReceivingFacility = GetField(fields, 5),
                MessageDateTime = (DateTime)ParseDateTime(GetField(fields, 6)),
                Security = GetField(fields, 7),
                MessageType = GetField(fields, 8),
                MessageControlID = GetField(fields, 9),
                ProcessingID = GetField(fields, 10),
                VersionID = GetField(fields, 11),
                MSH12 = GetField(fields, 12),
                MSH13 = GetField(fields, 13),
                MSH14 = GetField(fields, 14),
                MSH15 = GetField(fields, 15),
                MSH16 = GetField(fields, 16),
                MSH17 = GetField(fields, 17),
                MSH18 = GetField(fields, 18),
            };
        }

        private PID_IPD ParsePID(string[] fields)
        {
            var officialNameComponents = GetField(fields, 5).Split(COMPONENT_SEPARATOR[0]);
            var aliasNameComponents = GetField(fields, 9).Split(COMPONENT_SEPARATOR[0]);
            var addressComponents = GetField(fields, 11).Split(COMPONENT_SEPARATOR[0]);
            var nationalityComponents = GetField(fields, 28).Split(COMPONENT_SEPARATOR[0]);

            return new PID_IPD
            {
                SetID = int.TryParse(GetField(fields, 1), out var setId) ? setId : 1,
                PatientIDExternal = GetField(fields, 2),
                PatientIDInternal = GetField(fields, 3),
                AlternatePatientID = GetField(fields, 4),

                OfficialName = new PatientName_IPD
                {
                    LastName = GetComponent(officialNameComponents, 0),
                    FirstName = GetComponent(officialNameComponents, 1),
                    MiddleName = GetComponent(officialNameComponents, 2),
                    Suffix = GetComponent(officialNameComponents, 3),
                    Prefix = GetComponent(officialNameComponents, 4),
                    Degree = GetComponent(officialNameComponents, 5),
                    NameTypeCode = GetComponent(officialNameComponents, 6),
                    NameRepresentationCode = GetComponent(officialNameComponents, 7)
                },

                Mothermaidenname = GetField(fields, 6),
                DateOfBirth = ParseDateTime(GetField(fields, 7)),
                Sex = GetField(fields, 8),

                AliasName = new PatientName_IPD
                {
                    LastName = GetComponent(aliasNameComponents, 0),
                    FirstName = GetComponent(aliasNameComponents, 1),
                    MiddleName = GetComponent(aliasNameComponents, 2),
                    Suffix = GetComponent(aliasNameComponents, 3),
                    Prefix = GetComponent(aliasNameComponents, 4),
                    Degree = GetComponent(aliasNameComponents, 5),
                    NameTypeCode = GetComponent(aliasNameComponents, 6),
                    NameRepresentationCode = GetComponent(aliasNameComponents, 7)
                },

                Race = GetField(fields, 10),

                Address = new PatientAddress_IPD
                {
                    StreetAddress = GetComponent(addressComponents, 0),
                    OtherDesignation = GetComponent(addressComponents, 1),
                    City = GetComponent(addressComponents, 2),
                    StateOrProvince = GetComponent(addressComponents, 3),
                    ZipOrPostalCode = GetComponent(addressComponents, 4)
                },

                Country = GetField(fields, 12),
                PhoneNumberHome = GetField(fields, 13),
                PID14 = GetField(fields, 14),
                PID15 = GetField(fields, 15),
                Marital = GetField(fields, 16),
                Religion = GetField(fields, 17),
                PID18 = GetField(fields, 18),
                PID19 = GetField(fields, 19),
                PID20 = GetField(fields, 20),
                PID21 = GetField(fields, 21),
                PID22 = GetField(fields, 22),
                PID23 = GetField(fields, 23),
                PID24 = GetField(fields, 24),
                PID25 = GetField(fields, 25),
                PID26 = GetField(fields, 26),
                PID27 = GetField(fields, 27),

                Nationality = new Nationality_IPD
                {
                    Nationality1 = GetComponent(nationalityComponents, 0),
                    NameNationality = GetComponent(nationalityComponents, 1)
                },

                PID29 = GetField(fields, 29),
                PID30 = GetField(fields, 30),
            };
        }

        private PV1_IPD ParsePV1(string[] fields)
        {
            var locationComponents = GetField(fields, 3).Split(COMPONENT_SEPARATOR[0]);
            var attendingDoctorComponents = GetField(fields, 7).Split(COMPONENT_SEPARATOR[0]);
            var referringDoctorComponents = GetField(fields, 8).Split(COMPONENT_SEPARATOR[0]);
            var admittingDoctorComponents = GetField(fields, 17).Split(COMPONENT_SEPARATOR[0]);
            var patientTypeComponents = GetField(fields, 18).Split(COMPONENT_SEPARATOR[0]);
            var financialClassComponents = GetField(fields, 20).Split(COMPONENT_SEPARATOR[0]);

            return new PV1_IPD
            {
                SetID = GetField(fields, 1),
                PatientClass = GetField(fields, 2),

                AssignedPatientLocation = new AssignedLocation_IPD
                {
                    PointOfCare = GetComponent(locationComponents, 0),
                    Room = GetComponent(locationComponents, 1),
                    Bed = GetComponent(locationComponents, 2)
                },

                AttendingDoctor = new AttendingDoctor_IPD
                {
                    ID = GetComponent(attendingDoctorComponents, 0),
                    Name = GetComponent(attendingDoctorComponents, 1),
                },

                ReferringDoctor = new ReferringDoctor_IPD
                {
                    ID = GetComponent(referringDoctorComponents, 0),
                    Name = GetComponent(referringDoctorComponents, 1),
                },

                AdmittingDoctor = new AdmittingDoctor_IPD
                {
                    ID = GetComponent(admittingDoctorComponents, 0),
                    LastName = GetComponent(admittingDoctorComponents, 1),
                    FirstName = GetComponent(admittingDoctorComponents, 2),
                    Prefix = GetComponent(admittingDoctorComponents, 4)
                },

                PatientType = new PatientType_IPD
                {
                    ID = GetComponent(patientTypeComponents, 0),
                    Name = GetComponent(patientTypeComponents, 1)
                },

                VisitNumber = GetField(fields, 19),

                FinancialClass = new FinancialClass_IPD
                {
                    ID = GetComponent(financialClassComponents, 0),
                    Name = GetComponent(financialClassComponents, 1)
                },

                AdmitDateTime = ParseDateTime(GetField(fields, 44))
            };
        }

        private ORC_IPD ParseORC(string[] fields)
        {
            var placerGroupComponents = GetField(fields, 4).Split(COMPONENT_SEPARATOR[0]);
            var verifiedByComponents = GetField(fields, 11).Split(COMPONENT_SEPARATOR[0]);
            var orderingProviderComponents = GetField(fields, 12).Split(COMPONENT_SEPARATOR[0]);

            string raw = GetField(fields, 13);
            string[] enterersLocationComponents;

            if (raw.Contains("^"))
            {
                enterersLocationComponents = raw.Split('^');
            }
            else
            {
                int spaceIndex = raw.IndexOf(' ');
                enterersLocationComponents = spaceIndex > 0
                    ? new[] { raw.Substring(0, spaceIndex), raw.Substring(spaceIndex + 1) }
                    : new[] { raw };
            }

            return new ORC_IPD
            {
                OrderControl = GetField(fields, 1),
                PlacerOrderNumber = GetField(fields, 2),
                FillerOrderNumber = GetField(fields, 3),

                PlacerGroup = new PlacerGroup_IPD
                {
                    ID = GetComponent(placerGroupComponents, 0),
                    Name = GetComponent(placerGroupComponents, 1),
                },

                OrderStatus = string.IsNullOrWhiteSpace(GetField(fields, 5)) ? "0" : GetField(fields, 5),
                ResponseFlag = string.IsNullOrWhiteSpace(GetField(fields, 6)) ? "0" : GetField(fields, 6),
                QuantityTiming = ParseInt(GetField(fields, 7)),
                Parent = GetField(fields, 8),
                TransactionDateTime = ParseDateTime(GetField(fields, 9)),
                EnteredBy = GetField(fields, 10),

                VerifiedBy = new VerifiedBy_IPD
                {
                    ID = GetComponent(verifiedByComponents, 0),
                    LastName = GetComponent(verifiedByComponents, 1),
                    FirstName = GetComponent(verifiedByComponents, 2),
                    MiddleName = GetComponent(verifiedByComponents, 3),
                    Prefix = GetComponent(verifiedByComponents, 4),
                    Suffix = GetComponent(verifiedByComponents, 5)
                },

                OrderingProvider = new OrderingProvider_IPD
                {
                    ID = GetComponent(orderingProviderComponents, 0),
                    OrderingProvider1 = GetComponent(orderingProviderComponents, 1),
                    Name = GetComponent(orderingProviderComponents, 2),
                    OrderingProvider3 = GetComponent(orderingProviderComponents, 3),
                    OrderingProvider4 = GetComponent(orderingProviderComponents, 4),
                    OrderingProvider5 = GetComponent(orderingProviderComponents, 5),
                    OrderingProvider6 = GetComponent(orderingProviderComponents, 6),
                    OrderingProvider7 = GetComponent(orderingProviderComponents, 7),
                    OrderingProvider8 = GetComponent(orderingProviderComponents, 8),
                    OrderingProvider9 = GetComponent(orderingProviderComponents, 9),
                    OrderingProvider10 = GetComponent(orderingProviderComponents, 10),
                    OrderingProvider11 = GetComponent(orderingProviderComponents, 11),
                    OrderingProvider12 = GetComponent(orderingProviderComponents, 12),
                    OrderingProvider13 = GetComponent(orderingProviderComponents, 13),
                    OrderingProvider14 = GetComponent(orderingProviderComponents, 14)
                },

                EnterersLocation = GetField(fields, 13),
                CallBackPhoneNumber = GetField(fields, 14),
                OrderEffectiveDateTime = ParseDateTime(GetField(fields, 15)),
                OrderControlCodeReason = GetField(fields, 16),
                EnteringOrganization = GetField(fields, 17),
                EnteringDevice = GetField(fields, 18),
                ActionBy = GetField(fields, 19),
                AdvancedCodeBeneficiaryNotice = GetField(fields, 20),
                OrderingFacilityName = GetField(fields, 21),
                OrderingFacilityAddress = GetField(fields, 22)
            };
        }

        private AL1_IPD ParseAL1(string[] fields)
        {
            return new AL1_IPD
            {
                SetID = GetField(fields, 1),
                AllergyTypeCode = GetField(fields, 2),
                AllergyName = GetField(fields, 3),
                AllergySeverity = GetField(fields, 4),
                AllergyReaction = GetField(fields, 5),
                IdentificationDate = ParseDateTime(GetField(fields, 6))
            };
        }

        /// <summary>
        /// Parse RXE segment → RXD_IPD
        /// IPD: DrugNamePrint (index 1 ของ drugParts) → ใช้เป็น f_orderitemnameTH
        /// </summary>
        private RXD_IPD ParseRXE(string[] fields)
        {
            return ParseRXDOrRXE(fields, isRXE: true);
        }

        /// <summary>
        /// Parse RXD segment → RXD_IPD
        /// </summary>
        private RXD_IPD ParseRXD(string[] fields)
        {
            return ParseRXDOrRXE(fields, isRXE: false);
        }

        /// <summary>
        /// Logic การ parse RXE/RXD ร่วมกัน
        /// </summary>
        private RXD_IPD ParseRXDOrRXE(string[] fields, bool isRXE)
        {
            var drugComponents = GetField(fields, 2).Split(COMPONENT_SEPARATOR[0]);
            var rawDrugName = GetComponent(drugComponents, 4) ?? "";
            var drugParts = rawDrugName
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToArray();

            string drugName = drugParts.Length > 0 ? drugParts[0] : "";
            string drugNamePrint = drugParts.Length > 1 ? drugParts[1] : "";
            string drugNameThai = drugParts.Length > 2 ? drugParts[2] : "";
            string drugUnit = "";
            string cleanedDrugName = drugName;

            if (!string.IsNullOrEmpty(drugName))
            {
                var knownUnits = new[]
                {
                    "ขวด", "เม็ด", "แคปซูล", "ซอง", "หลอด", "กล่อง",
                    "แผง", "ชิ้น", "ขนาด", "วอน", "ลูก", "ก้อน", "ถุง", "VIAL"
                };

                var parts = drugName
                    .Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                if (parts.Count > 0)
                {
                    var lastPartClean = parts[parts.Count - 1].Trim().TrimEnd('.', ';', ':');

                    if (knownUnits.Any(u => lastPartClean.Equals(u, StringComparison.OrdinalIgnoreCase)))
                    {
                        drugUnit = lastPartClean;
                        parts.RemoveAt(parts.Count - 1);
                        cleanedDrugName = string.Join(" ", parts).Trim();
                    }
                    else
                    {
                        cleanedDrugName = drugName.Trim();
                    }
                }
            }

            var staffComponents = GetField(fields, 5).Split(COMPONENT_SEPARATOR[0]);

            // รวม field 7 ที่อาจถูกแยกโดย pipe (Medicinal properties)
            var adjustedFields = BuildAdjustedFields(fields);
            var adjustedFieldsArray = adjustedFields.ToArray();

            var substandComponents = adjustedFieldsArray[7].Split(COMPONENT_SEPARATOR[0]);
            var usageParts = (GetComponent(substandComponents, 4) ?? "")
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToArray();

            var usageUnitComponents = GetField(adjustedFieldsArray, 11).Split(COMPONENT_SEPARATOR[0]);
            var doctorComponents = GetField(adjustedFieldsArray, 14).Split(COMPONENT_SEPARATOR[0]);
            var orderUnitcodeComponents = GetField(adjustedFieldsArray, 29).Split(COMPONENT_SEPARATOR[0]);

            var departmentName = GetField(adjustedFieldsArray, 28);
            var rxd31 = GetField(adjustedFieldsArray, 31);

            if (departmentName == "Continue")
            {
                rxd31 = departmentName;
                departmentName = "";
            }

            var usagecodeField = GetField(adjustedFieldsArray, 30);
            string rxd33 = GetField(adjustedFieldsArray, 33);
            string[] usagecodeComponents;

            if (usagecodeField.Contains("^"))
            {
                usagecodeComponents = usagecodeField.Split('^');
            }
            else if (usagecodeField.Contains(";"))
            {
                usagecodeComponents = usagecodeField.Split(';');
            }
            else
            {
                rxd33 = usagecodeField;
                usagecodeComponents = Array.Empty<string>();
                usagecodeField = null;
            }

            return new RXD_IPD
            {
                IsRXE = isRXE,
                QTY = ParseInt(GetField(adjustedFieldsArray, 1)),

                Dispensegivecode = new Dispensegivecode_IPD
                {
                    Dispense = GetComponent(drugComponents, 0),
                    UniqID = GetComponent(drugComponents, 1),
                    RXD203 = GetComponent(drugComponents, 2),
                    Identifier = GetComponent(drugComponents, 3),
                    DrugName = cleanedDrugName,
                    DrugNamePrint = drugNamePrint,
                    DrugNameThai = drugNameThai,
                    DrugUnit = drugUnit
                },

                DateTimeDispensed = ParseDateTime(GetField(adjustedFieldsArray, 3)),
                RXD4 = ParseInt(GetField(adjustedFieldsArray, 4)),

                Modifystaff = new Modifystaff_IPD
                {
                    StaffCode = GetComponent(staffComponents, 0),
                    StaffName = GetComponent(staffComponents, 1)
                },

                Dosageform = GetField(adjustedFieldsArray, 6),

                Substand = new Substand_IPD
                {
                    RXD701 = GetComponent(substandComponents, 0),
                    Medicinalproperties = GetComponent(substandComponents, 1),
                    Labelhelp = GetComponent(substandComponents, 2),
                    RXD704 = GetComponent(substandComponents, 3),
                    Usageline1 = usageParts.Length > 0 ? usageParts[0] : "",
                    Usageline2 = usageParts.Length > 1 ? usageParts[1] : "",
                    Usageline3 = usageParts.Length > 2 ? usageParts[2] : "",
                    Noteprocessing = GetComponent(substandComponents, 5)
                },

                Actualdispense = GetField(adjustedFieldsArray, 8),
                prioritycode = GetField(adjustedFieldsArray, 9),
                Dose = ParseInt(GetField(adjustedFieldsArray, 10)),

                Usageunit = new Usageunit_IPD
                {
                    ID = GetComponent(usageUnitComponents, 0),
                    Name = GetComponent(usageUnitComponents, 1)
                },

                RXD12 = GetField(adjustedFieldsArray, 12),
                RXD13 = GetField(adjustedFieldsArray, 13),

                Doctor = new Doctor_IPD
                {
                    ID = GetComponent(doctorComponents, 0),
                    Name = GetComponent(doctorComponents, 1)
                },

                RXD15 = GetField(adjustedFieldsArray, 15),
                RXD16 = GetField(adjustedFieldsArray, 16),
                RXD17 = GetField(adjustedFieldsArray, 17),
                RXD18 = GetField(adjustedFieldsArray, 18),
                RXD19 = GetField(adjustedFieldsArray, 19),
                RXD20 = GetField(adjustedFieldsArray, 20),
                RXD21 = GetField(adjustedFieldsArray, 21),
                RXD22 = GetField(adjustedFieldsArray, 22),
                RXD23 = GetField(adjustedFieldsArray, 23),
                RXD24 = GetField(adjustedFieldsArray, 24),
                RXD25 = GetField(adjustedFieldsArray, 25),

                Strengthunit = GetField(adjustedFieldsArray, 26),
                Departmentcode = GetField(adjustedFieldsArray, 27),
                Departmentname = departmentName,

                Orderunitcode = new Orderunitcode_IPD
                {
                    Nameeng = GetComponent(orderUnitcodeComponents, 0),
                    Namethai = GetComponent(orderUnitcodeComponents, 1)
                },

                Usagecode = new Usagecode_IPD
                {
                    Instructioncode = GetComponent(usagecodeComponents, 0),
                    RXD3002 = GetComponent(usagecodeComponents, 1),
                    RXD3003 = GetComponent(usagecodeComponents, 2),
                    Frequencycode = GetComponent(usagecodeComponents, 3),
                    Frequencydesc = GetComponent(usagecodeComponents, 4),
                    RXD3006 = GetComponent(usagecodeComponents, 5),
                    RXD3007 = GetComponent(usagecodeComponents, 6)
                },

                RXD31 = rxd31,
                RXD32 = GetField(adjustedFieldsArray, 32),
                RXD33 = rxd33,
            };
        }

        private RXR_IPD ParseRXR(string[] fields)
        {
            return new RXR_IPD
            {
                Route = GetField(fields, 1),
                site = GetField(fields, 2),
                AdministrationDevice = ParseInt(GetField(fields, 3)),
                AdministrationMethod = GetField(fields, 4),
                RoutingInstruction = GetField(fields, 5)
            };
        }

        private NTE_IPD ParseNTE(string[] fields)
        {
            return new NTE_IPD
            {
                SetID = ParseInt(GetField(fields, 1)),
                CommentType = GetField(fields, 2),
                CommentNote = GetField(fields, 3)
            };
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// รวม field 7 ที่อาจถูกตัดโดย pipe (Medicinal properties มี pipe อยู่ข้างใน)
        /// หยุดรวมเมื่อเจอ field ที่เป็น "valid field 8 or later" เช่น PROUD หรือตัวเลขล้วน
        /// </summary>
        private List<string> BuildAdjustedFields(string[] fields)
        {
            var adjustedFields = new List<string>(fields);

            if (fields.Length <= 7)
                return adjustedFields;

            string field7Combined = GetField(fields, 7);
            int fieldIndex = 8;

            while (fieldIndex < fields.Length &&
                   !string.IsNullOrEmpty(GetField(fields, fieldIndex)) &&
                   !IsValidField8OrLater(GetField(fields, fieldIndex)))
            {
                field7Combined += GetField(fields, fieldIndex);
                fieldIndex++;
            }

            adjustedFields.Clear();
            for (int i = 0; i <= 6; i++)
                adjustedFields.Add(GetField(fields, i));

            adjustedFields.Add(field7Combined);

            for (int i = fieldIndex; i < fields.Length; i++)
                adjustedFields.Add(GetField(fields, i));

            return adjustedFields;
        }

        /// <summary>
        /// ตัดสินว่า field ที่ index >= 8 เป็น field จริง (ไม่ใช่ส่วนต่อของ field 7)
        /// 
        /// กฎ:
        ///   - empty → ถือว่าเป็น field จริง (หยุดรวม)
        ///   - ขึ้นต้นด้วย "^"       → ยังเป็นส่วนของ field 7 (component ที่ถูกตัด)
        ///   - มี "^^"               → ยังเป็นส่วนของ field 7
        ///   - URL (http/https)      → ยังเป็นส่วนของ field 7 (เช่น label ยาที่มี link)
        ///   - ขึ้นต้นด้วย "ยา"      → ยังเป็นส่วนของ field 7 (ชื่อกลุ่มยา)
        ///   - มีคำไทยบอกวิธีกิน    → ยังเป็นส่วนของ field 7
        ///   - "PROUD" หรือตัวเลขล้วน → field จริง (หยุดรวม)
        /// </summary>
        private bool IsValidField8OrLater(string fieldValue)
        {
            if (string.IsNullOrEmpty(fieldValue))
                return true;

            // ยังเป็นส่วนของ field 7 (component ที่ถูกตัดโดย pipe)
            if (fieldValue.StartsWith("^"))
                return false;

            if (fieldValue.Contains("^^"))
                return false;

            // URL ที่ฝังใน field 7 (เช่น link ข้อมูลยา)
            if (fieldValue.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                fieldValue.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return false;

            // ชื่อกลุ่มยาภาษาไทย เช่น "ยาเบาหวาน", "ยาลดบวม"
            if (fieldValue.StartsWith("ยา"))
                return false;

            // คำบอกวิธีรับประทาน
            if (fieldValue.Contains("รับประทาน") ||
                fieldValue.Contains("เม็ด") ||
                fieldValue.Contains("ครั้ง"))
                return false;

            // field จริง: PROUD หรือตัวเลขล้วน (QTY, Dose ฯลฯ)
            if (fieldValue.Equals("PROUD", StringComparison.OrdinalIgnoreCase) ||
                fieldValue.All(char.IsDigit))
                return true;

            return true;
        }

        private string GetField(string[] fields, int index)
        {
            return index < fields.Length ? fields[index] : "";
        }

        private string GetComponent(string[] components, int index)
        {
            return index < components.Length ? components[index] : "";
        }

        /// <summary>
        /// Parse datetime string รองรับ format ที่ส่งมาจริง:
        ///   - yyyyMMddHHmmss         เช่น 20260612100145
        ///   - yyyyMMdd               เช่น 20260612
        ///   - yyyyMMddHH:mm:ss       เช่น 2026061210:01:45  ← format ที่พบในข้อมูลจริง
        /// </summary>
        private DateTime? ParseDateTime(string dateTimeStr)
        {
            if (string.IsNullOrWhiteSpace(dateTimeStr))
                return null;

            var provider = System.Globalization.CultureInfo.InvariantCulture;

            // แก้ไข: เพิ่ม format yyyyMMddHH:mm:ss ที่พบในข้อมูลจริง เช่น "2026061210:01:45"
            string[] formats =
            {
                "yyyyMMddHHmmss",
                "yyyyMMdd",
                "yyyyMMddHH:mm:ss",
                "yyyyMMddHH':'mm':'ss"
            };

            if (DateTime.TryParseExact(dateTimeStr, formats, provider,
                System.Globalization.DateTimeStyles.None, out DateTime result))
            {
                return result;
            }

            // Fallback: ลอง normalize "2026061210:01:45" → "20260612100145" แล้ว parse ใหม่
            var normalized = dateTimeStr.Replace(":", "");
            if (DateTime.TryParseExact(normalized, "yyyyMMddHHmmss", provider,
                System.Globalization.DateTimeStyles.None, out DateTime fallback))
            {
                return fallback;
            }

            return null;
        }

        private int ParseInt(string value)
        {
            return int.TryParse(value, out int result) ? result : 0;
        }

        #endregion
    }
}