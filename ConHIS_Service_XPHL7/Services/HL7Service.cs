using System;
using System.Text;
using ConHIS_Service_XPHL7.Models;

namespace ConHIS_Service_XPHL7.Services
{
    public class HL7Service
    {
        private const string FIELD_SEPARATOR = "|";
        private const string COMPONENT_SEPARATOR = "^" ;


       
        public HL7Message ParseHL7Message(string hl7Data)
        {
            try
            {
                var lines = hl7Data.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                var message = new HL7Message();

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // guard for very short lines
                    if (line.Length < 3) continue;

                    var segmentType = line.Substring(0, 3).ToUpperInvariant();
                    var fields = line.Split(FIELD_SEPARATOR[0]);
                    var COMPONENT = line.Split(COMPONENT_SEPARATOR[0]);

                    // Only parse MSH and PID segments; skip all other segments silently
                    switch (segmentType)
                    {
                        case "MSH":
                            message.MessageHeader = ParseMSH(fields, COMPONENT);
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
                            // Log unknown segments but don't throw
                            continue;
                    }
                }

                return message;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing HL7 message: {ex.Message}", ex);
            }
        }

        #region Parse Segments
        private MSH ParseMSH(string[] fields, string[] COMPONENT)
        {
            return new MSH
            {
                EncodingCharacters = GetField(fields, 1),
                SendingApplication = GetField(fields, 2),
                SendingFacility = GetField(fields, 3),
                ReceivingApplication = GetField(fields, 4),
                ReceivingFacility = GetField(fields, 5),
                MessageDateTime = ParseDateTime(GetField(fields, 6)),
                Security = GetField(fields, 7),
                MessageType = GetField(fields, 8),
                MessageControlID = GetField(fields, 9),
                ProcessingID = GetField(fields, 10),
                VersionID = GetField(fields, 11)
            };
        }

        private PID ParsePID(string[] fields)
        {
            // PID-5 Official Name
            var officialNameComponents = GetField(fields, 5).Split(COMPONENT_SEPARATOR[0]);
            // PID-9 Alias Name
            var aliasNameComponents = GetField(fields, 9).Split(COMPONENT_SEPARATOR[0]);
            // PID-11 Address
            var addressComponents = GetField(fields, 11).Split(COMPONENT_SEPARATOR[0]);

            return new PID
            {
                SetID = GetField(fields, 1),                        // PID-1
                PatientIDExternal = GetField(fields, 2),           // PID-2
                PatientIDInternal = GetField(fields, 3),           // PID-3
                AlternatePatientID = GetField(fields, 4),          // PID-4

                OfficialName = new PatientName                     // PID-5
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

                Mothermaidenname = GetField(fields, 6),            // PID-6
                DateOfBirth = ParseDateTime(GetField(fields, 7)),  // PID-7
                Sex = GetField(fields, 8),                         // PID-8

                AliasName = new PatientName                         // PID-9
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

                Race = GetField(fields, 10),                        // PID-10

                Address = new PatientAddress                        // PID-11
                {
                    StreetAddress = GetComponent(addressComponents, 0),
                    OtherDesignation = GetComponent(addressComponents, 1),
                    City = GetComponent(addressComponents, 2),
                    StateOrProvince = GetComponent(addressComponents, 3),
                    ZipOrPostalCode = GetComponent(addressComponents, 4)
                },

                Country = GetField(fields, 12),                     // PID-12
                PhoneNumberHome = GetField(fields, 13)              // PID-13
            };
        }

        private PV1 ParsePV1(string[] fields)
        {
            var locationComponents = GetField(fields, 3).Split(COMPONENT_SEPARATOR[0]);
            var admittingDoctorComponents = GetField(fields, 17).Split(COMPONENT_SEPARATOR[0]);
            var patientTypeComponents = GetField(fields, 18).Split(COMPONENT_SEPARATOR[0]);

            return new PV1
            {
                SetID = GetField(fields, 1),
                PatientClass = GetField(fields, 2),
                AssignedPatientLocation = new AssignedLocation
                {
                    PointOfCare = GetComponent(locationComponents, 0),
                    Room = GetComponent(locationComponents, 1),
                    Bed = GetComponent(locationComponents, 2)
                },
                AdmittingDoctor = new AdmittingDoctor
                {
                    ID = GetComponent(admittingDoctorComponents, 0),
                    LastName = GetComponent(admittingDoctorComponents, 1),
                    FirstName = GetComponent(admittingDoctorComponents, 2),
                    Prefix = GetComponent(admittingDoctorComponents, 4)
                },
                PatientType = new PatientType
                {
                    ID = GetComponent(patientTypeComponents, 0),
                    Name = GetComponent(patientTypeComponents, 1)
                }
                // ส่วน field อื่น ๆ ที่ไม่ใช้ ไม่ต้อง map จะได้เป็น default
            };
        }
        private ORC ParseORC(string[] fields)
        {
            var verifiedByComponents = GetField(fields, 11).Split(COMPONENT_SEPARATOR[0]);
            var orderingProviderComponents = GetField(fields, 12).Split(COMPONENT_SEPARATOR[0]);

            return new ORC
            {
                OrderControl = GetField(fields, 1),
                PlacerOrderNumber = GetField(fields, 2),
                FillerOrderNumber = GetField(fields, 3),
                PlacerGroupID = GetField(fields, 4),
                PlacerGroupName = GetField(fields, 5), // Not used
                OrderStatus ="0",
                ResponseFlag = "0",
                QuantityTiming = ParseInt(GetField(fields, 7)),
                Parent = GetField(fields, 8),
                TransactionDateTime = ParseDateTime(GetField(fields, 9)),
                EnteredBy = GetField(fields, 10),
                VerifiedBy = new VerifiedBy
                {
                    ID = GetComponent(verifiedByComponents, 0),
                    LastName = GetComponent(verifiedByComponents, 1),
                    FirstName = GetComponent(verifiedByComponents, 2),
                    MiddleName = GetComponent(verifiedByComponents, 3),
                    Prefix = GetComponent(verifiedByComponents, 4),
                    Suffix = GetComponent(verifiedByComponents, 5)
                },
                OrderingProvider = new OrderingProvider
                {
                    ID = GetComponent(orderingProviderComponents, 0),
                    LastName = GetComponent(orderingProviderComponents, 1),
                    FirstName = GetComponent(orderingProviderComponents, 2),
                    MiddleName = GetComponent(orderingProviderComponents, 3),
                    Prefix = GetComponent(orderingProviderComponents, 4),
                    Suffix = GetComponent(orderingProviderComponents, 5)
                },
                EnterersLocationID = GetField(fields, 13),
                EnterersLocationName = "", // Not used
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


        private AL1 ParseAL1(string[] fields)
        {
            return new AL1
            {
                SetID = GetField(fields, 1),
                AllergyTypeCode = GetField(fields, 2),
                AllergyName = GetField(fields, 3),
                AllergySeverity = GetField(fields, 4),       // AL1-4
                AllergyReaction = GetField(fields, 5),
                IdentificationDate = ParseDateTime(GetField(fields, 6))
            };
        }

        private RXD ParseRXE(string[] fields)
        {
            var drugComponents = GetField(fields, 2).Split(COMPONENT_SEPARATOR[0]);
            var staffComponents = GetField(fields, 5).Split(COMPONENT_SEPARATOR[0]);
            var usageUnitComponents = GetField(fields, 14).Split(COMPONENT_SEPARATOR[0]);
            var frequencyComponents = GetField(fields, 15).Split(COMPONENT_SEPARATOR[0]);
            var timeComponents = GetField(fields, 16).Split(COMPONENT_SEPARATOR[0]);
            var deptComponents = GetField(fields, 18).Split(COMPONENT_SEPARATOR[0]);
            var doctorComponents = GetField(fields, 19).Split(COMPONENT_SEPARATOR[0]);
            var substandComponents = GetField(fields, 20).Split(COMPONENT_SEPARATOR[0]);

            return new RXD
            {
                SetID = ParseInt(GetField(fields, 1)),
                Dispensegivecode = new Dispensegivecode
                {
                    Identifier = GetComponent(drugComponents, 0),
                    DrugName = GetComponent(drugComponents, 1),
                    DrugNamePrint = GetComponent(drugComponents, 2),
                    DrugNameThai = GetComponent(drugComponents, 3)
                },
                DateTimeDispensed = ParseDateTime(GetField(fields, 3)),
                ActualDispense = ParseInt(GetField(fields, 4)),
                Modifystaff = new Modifystaff
                {
                    StaffCode = GetComponent(staffComponents, 0),
                    StaffName = GetComponent(staffComponents, 1)
                },
                QTY = ParseInt(GetField(fields, 6)),
                Dose = ParseInt(GetField(fields, 7)),
                UsageCODE = GetField(fields, 8),
                UsageLine1 = GetField(fields, 9),
                UsageLine2 = GetField(fields, 10),
                UsageLine3 = GetField(fields, 11),
                UsageLine4 = GetField(fields, 12),
                DosageForm = GetField(fields, 13),
                UsageUnit = new UsageUnit
                {
                    Code = GetComponent(usageUnitComponents, 0),
                    Name = GetComponent(usageUnitComponents, 1),
                    UnitName = GetComponent(usageUnitComponents, 2)
                },
                Frequency = new FrequencyInfo
                {
                    FrequencyID = GetComponent(frequencyComponents, 0),
                    FrequencyName = GetComponent(frequencyComponents, 1)
                },
                Time = new TimeInfo
                {
                    TimeID = GetComponent(timeComponents, 0),
                    TimeName = GetComponent(timeComponents, 1)
                },
                StrengthUnit = GetField(fields, 17),
                Department = new DepartmentOrder
                {
                    DepartmentCode = GetComponent(deptComponents, 0),
                    DepartmentName = GetComponent(deptComponents, 1)
                },
                Doctor = new DoctorOrder
                {
                    DoctorCode = GetComponent(doctorComponents, 0),
                    DoctorName = GetComponent(doctorComponents, 1)
                },
                Substand = new SubstandInfo
                {
                    DrugProperty = GetComponent(substandComponents, 0),
                    LabelHelp = GetComponent(substandComponents, 1)
                },
                FinanceStatus = GetField(fields, 21),
                DrugType = GetField(fields, 23),
                TotalPrice = ParseDecimal(GetField(fields, 27)),
                 IsRXE = true
            };
        }
        private RXD ParseRXD(string[] fields)
        {
            var drugComponents = GetField(fields, 2).Split(COMPONENT_SEPARATOR[0]);
            var staffComponents = GetField(fields, 5).Split(COMPONENT_SEPARATOR[0]);
            var usageUnitComponents = GetField(fields, 14).Split(COMPONENT_SEPARATOR[0]);
            var frequencyComponents = GetField(fields, 15).Split(COMPONENT_SEPARATOR[0]);
            var timeComponents = GetField(fields, 16).Split(COMPONENT_SEPARATOR[0]);
            var deptComponents = GetField(fields, 18).Split(COMPONENT_SEPARATOR[0]);
            var doctorComponents = GetField(fields, 19).Split(COMPONENT_SEPARATOR[0]);
            var substandComponents = GetField(fields, 20).Split(COMPONENT_SEPARATOR[0]);

            return new RXD
            {
                SetID = ParseInt(GetField(fields, 1)),
                Dispensegivecode = new Dispensegivecode
                {
                    Identifier = GetComponent(drugComponents, 0),
                    DrugName = GetComponent(drugComponents, 1),
                    DrugNamePrint = GetComponent(drugComponents, 2),
                    DrugNameThai = GetComponent(drugComponents, 3)
                },
                DateTimeDispensed = ParseDateTime(GetField(fields, 3)),
                ActualDispense = ParseInt(GetField(fields, 4)),
                Modifystaff = new Modifystaff
                {
                    StaffCode = GetComponent(staffComponents, 0),
                    StaffName = GetComponent(staffComponents, 1)
                },
                QTY = ParseInt(GetField(fields, 6)),
                Dose = ParseInt(GetField(fields, 7)),
                UsageCODE = GetField(fields, 8),
                UsageLine1 = GetField(fields, 9),
                UsageLine2 = GetField(fields, 10),
                UsageLine3 = GetField(fields, 11),
                UsageLine4 = GetField(fields, 12),
                DosageForm = GetField(fields, 13),
                UsageUnit = new UsageUnit
                {
                    Code = GetComponent(usageUnitComponents, 0),
                    Name = GetComponent(usageUnitComponents, 1),
                    UnitName = GetComponent(usageUnitComponents, 2)
                },
                Frequency = new FrequencyInfo
                {
                    FrequencyID = GetComponent(frequencyComponents, 0),
                    FrequencyName = GetComponent(frequencyComponents, 1)
                },
                Time = new TimeInfo
                {
                    TimeID = GetComponent(timeComponents, 0),
                    TimeName = GetComponent(timeComponents, 1)
                },
                StrengthUnit = GetField(fields, 17),
                Department = new DepartmentOrder
                {
                    DepartmentCode = GetComponent(deptComponents, 0),
                    DepartmentName = GetComponent(deptComponents, 1)
                },
                Doctor = new DoctorOrder
                {
                    DoctorCode = GetComponent(doctorComponents, 0),
                    DoctorName = GetComponent(doctorComponents, 1)
                },
                Substand = new SubstandInfo
                {
                    DrugProperty = GetComponent(substandComponents, 0),
                    LabelHelp = GetComponent(substandComponents, 1)
                },
                FinanceStatus = GetField(fields, 21),
                DrugType = GetField(fields, 23),
                TotalPrice = ParseDecimal(GetField(fields, 27)),
                IsRXE = false
            };
        }


        private RXR ParseRXR(string[] fields)
        {
            return new RXR
            {
                Route = GetField(fields, 1),                  // RXR-1
                site = GetField(fields, 2),                   // RXR-2
                AdministrationDevice = GetField(fields, 3),   // RXR-3
                AdministrationMethod = GetField(fields, 4),   // RXR-4
                RoutingInstruction = GetField(fields, 5)      // RXR-5
            };
        }


        private NTE ParseNTE(string[] fields)
        {
            return new NTE
            {
                SetID = ParseInt(GetField(fields, 1)),        // NTE-1
                CommentType = GetField(fields, 2),           // NTE-2
                CommentNote = GetField(fields, 3)            // NTE-3
            };
        }

        #endregion

        

        #region Helper Methods
        private string GetField(string[] fields, int index)
        {
            return index < fields.Length ? fields[index] : "";
        }

        private string GetComponent(string[] components, int index)
        {
            return index < components.Length ? components[index] : "";
        }

        private DateTime ParseDateTime(string dateTimeStr)
        {
            if (string.IsNullOrWhiteSpace(dateTimeStr)) return DateTime.MinValue;

            if (DateTime.TryParseExact(dateTimeStr, "yyyyMMddHHmmss", null,
                System.Globalization.DateTimeStyles.None, out DateTime result))
                return result;

            if (DateTime.TryParseExact(dateTimeStr, "yyyyMMdd", null,
                System.Globalization.DateTimeStyles.None, out result))
                return result;

            return DateTime.MinValue;
        }

        private string FormatDateTime(DateTime? dateTime)
        {
            return (!dateTime.HasValue || dateTime == DateTime.MinValue) ? "" : dateTime.Value.ToString("yyyyMMddHHmmss");
        }

        private int ParseInt(string value)
        {
            return int.TryParse(value, out int result) ? result : 0;
        }

        private decimal ParseDecimal(string value)
        {
            return decimal.TryParse(value, out decimal result) ? result : 0;
        }
        #endregion
    }
}
