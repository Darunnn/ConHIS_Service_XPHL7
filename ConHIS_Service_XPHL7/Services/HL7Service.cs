using ConHIS_Service_XPHL7.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
                 

                    // Only parse MSH and PID segments; skip all other segments silently
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
        private MSH ParseMSH(string[] fields)
        {
            return new MSH
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

        private PID ParsePID(string[] fields)
        {
            // PID-5 Official Name
            var officialNameComponents = GetField(fields, 5).Split(COMPONENT_SEPARATOR[0]);
            // PID-9 Alias Name
            var aliasNameComponents = GetField(fields, 9).Split(COMPONENT_SEPARATOR[0]);
            // PID-11 Address
            var addressComponents = GetField(fields, 11).Split(COMPONENT_SEPARATOR[0]);
            // PID-28 Nationality
            var nationalityComponents = GetField(fields, 28).Split(COMPONENT_SEPARATOR[0]);
            return new PID
            {
                SetID = int.TryParse(GetField(fields, 1), out var setId) ? setId : 1,                        // PID-1
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
                PhoneNumberHome = GetField(fields, 13),              // PID-13
                PID14 = GetField(fields, 14),                     // PID-14
                PID15 = GetField(fields, 15),                     // PID-15
                Marital = GetField(fields, 16),               // PID-16
                Religion = GetField(fields, 17),                    // PID-17
                PID18 = GetField(fields, 18),                     // PID-18
                PID19 = GetField(fields, 19),                     // PID-19
                PID20 = GetField(fields, 20),                     // PID-20
                PID21 = GetField(fields, 21),                     // PID-21
                PID22 = GetField(fields, 22),                     // PID-22
                PID23 = GetField(fields, 23),                     // PID-23
                PID24 = GetField(fields, 24),                     // PID-24
                PID25 = GetField(fields, 25),                     // PID-25
                PID26 = GetField(fields, 26),                     // PID-26
                PID27 = GetField(fields, 27),                     // PID-27
                
                Nationality = new Nationality                   // PID-28
                {
                    Nationality1 = GetComponent(nationalityComponents, 0),
                    NameNationality = GetComponent(nationalityComponents, 1)
                },

                PID29 = GetField(fields, 29),                     // PID-29
                PID30 = GetField(fields, 30),                     // PID-30

            };
        }

        private PV1 ParsePV1(string[] fields)
        {
            var locationComponents = GetField(fields, 3).Split(COMPONENT_SEPARATOR[0]);
            var attendingDoctorComponents = GetField(fields, 7).Split(COMPONENT_SEPARATOR[0]);
            var referringDoctorComponents = GetField(fields, 8).Split(COMPONENT_SEPARATOR[0]);
            var admittingDoctorComponents = GetField(fields, 17).Split(COMPONENT_SEPARATOR[0]);
            var patientTypeComponents = GetField(fields, 18).Split(COMPONENT_SEPARATOR[0]);
            var FinancialClassComponents = GetField(fields, 20).Split(COMPONENT_SEPARATOR[0]);
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
                AttendingDoctor = new AttendingDoctor
                {
                    ID = GetComponent(attendingDoctorComponents, 0),
                    Name = GetComponent(attendingDoctorComponents, 1),
                },
                ReferringDoctor = new ReferringDoctor
                {
                    ID = GetComponent(referringDoctorComponents, 0),
                    Name = GetComponent(referringDoctorComponents, 1),
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
                },
                VisitNumber = GetField(fields, 19),
                FinancialClass = new FinancialClass
                {
                    ID = GetComponent(FinancialClassComponents, 0),
                    Name = GetComponent(FinancialClassComponents, 1)
                },
                AdmitDateTime = ParseDateTime(GetField(fields, 44))

            };
        }
        private ORC ParseORC(string[] fields)
        {
            var   PlacerGroupComponents = GetField(fields,4).Split(COMPONENT_SEPARATOR[0]);
            var verifiedByComponents = GetField(fields, 11).Split(COMPONENT_SEPARATOR[0]);
            var orderingProviderComponents = GetField(fields, 12).Split(COMPONENT_SEPARATOR[0]);
            string raw = GetField(fields, 13);
            string[] enterersLocationComponents;

            // เช็คว่ามี ^ หรือไม่
            if (raw.Contains("^"))
            {
                enterersLocationComponents = raw.Split('^');
            }
            else
            {
                // แยกด้วยช่องว่างตัวแรก
                int spaceIndex = raw.IndexOf(' ');
                if (spaceIndex > 0)
                {
                    enterersLocationComponents = new string[]
                    {
            raw.Substring(0, spaceIndex),       // ID
            raw.Substring(spaceIndex + 1)       // Name
                    };
                }
                else
                {
                   
                    enterersLocationComponents = new string[] { raw };
                }
            }
            return new ORC
            {
                OrderControl = GetField(fields, 1),
                PlacerOrderNumber = GetField(fields, 2),
                FillerOrderNumber = GetField(fields, 3),
                PlacerGroup = new PlacerGroup
                {
                    ID= GetComponent(PlacerGroupComponents, 0),
                    Name = GetComponent(PlacerGroupComponents, 1),
                },
                OrderStatus = string.IsNullOrWhiteSpace(GetField(fields, 5)) ? "0" : GetField(fields, 5),
                ResponseFlag = string.IsNullOrWhiteSpace(GetField(fields, 6)) ? "0" : GetField(fields, 6),
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

        // แก้ไขเฉพาะจุดที่มีปัญหา - ใน ParseRXE และ ParseRXD methods

        private RXD ParseRXE(string[] fields)
        {
            var drugComponents = GetField(fields, 2).Split(COMPONENT_SEPARATOR[0]);
            var staffComponents = GetField(fields, 5).Split(COMPONENT_SEPARATOR[0]);

            // **การแก้ไขหลัก: รวม field 7 ที่ถูกแยกโดย pipe**
            string field7Combined = "";
            List<string> adjustedFields = new List<string>(fields);

            // หา field 7 และรวมกับ field ที่ถัดไป หากมี pipe ใน medicinal properties
            if (fields.Length > 7)
            {
                field7Combined = GetField(fields, 7);

                // ถ้า field 8 ไม่ใช่ตัวเลขหรือ keyword ที่คาดหวัง แสดงว่าเป็นส่วนต่อของ field 7
                int fieldIndex = 8;
                while (fieldIndex < fields.Length &&
                       !string.IsNullOrEmpty(GetField(fields, fieldIndex)) &&
                       !IsValidField8OrLater(GetField(fields, fieldIndex)))
                {
                    // รวม field นี้เข้ากับ field 7
                    field7Combined += GetField(fields, fieldIndex);
                    fieldIndex++;
                }

                // สร้าง array ใหม่โดยรวม field 7 และปรับลำดับที่เหลือ
                adjustedFields.Clear();
                for (int i = 0; i <= 6; i++)
                {
                    adjustedFields.Add(GetField(fields, i));
                }
                adjustedFields.Add(field7Combined); // field 7 ที่รวมแล้ว

                // เพิ่ม field ที่เหลือ
                for (int i = fieldIndex; i < fields.Length; i++)
                {
                    adjustedFields.Add(GetField(fields, i));
                }
            }

            // ใช้ adjustedFields แทน fields
            var adjustedFieldsArray = adjustedFields.ToArray();

            // แยก components ของ field 7 ที่แก้ไขแล้ว
            var SubstandComponents = adjustedFieldsArray[7].Split(COMPONENT_SEPARATOR[0]);
            var usageParts = (GetComponent(SubstandComponents, 4) ?? "")
                            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim())
                            .ToArray();

            var usageUnitComponents = GetField(adjustedFieldsArray, 11).Split(COMPONENT_SEPARATOR[0]);
            var DoctorComponents = GetField(adjustedFieldsArray, 14).Split(COMPONENT_SEPARATOR[0]);
            var OrderunitcodeComponents = GetField(adjustedFieldsArray, 29).Split(COMPONENT_SEPARATOR[0]);
            var UsagecodeComponents = GetField(adjustedFieldsArray, 30).Split(COMPONENT_SEPARATOR[0]);

            return new RXD
            {
                QTY = ParseInt(GetField(adjustedFieldsArray, 1)),
                Dispensegivecode = new Dispensegivecode
                {
                    Dispense = GetComponent(drugComponents, 0),
                    UniqID = GetComponent(drugComponents, 1),
                    RXD203 = GetComponent(drugComponents, 2),
                    Identifier = GetComponent(drugComponents, 3),
                    DrugName = GetComponent(drugComponents, 4),
                    DrugNamePrint = GetComponent(drugComponents, 5),
                    DrugNameThai = GetComponent(drugComponents, 6)
                },
                DateTimeDispensed = ParseDateTime(GetField(adjustedFieldsArray, 3)),
                ActualDispense = ParseInt(GetField(adjustedFieldsArray, 4)),
                Modifystaff = new Modifystaff
                {
                    StaffCode = GetComponent(staffComponents, 0),
                    StaffName = GetComponent(staffComponents, 1)
                },
                Dosageform = GetField(adjustedFieldsArray, 6),
                Substand = new Substand
                {
                    RXD701 = GetComponent(SubstandComponents, 0),
                    Medicinalproperties = GetComponent(SubstandComponents, 1), // ตอนนี้จะได้ "รีชมพูแผงทึบ"
                    Labelhelp = GetComponent(SubstandComponents, 2),
                    RXD704 = GetComponent(SubstandComponents, 3),
                    Usageline1 = usageParts.Length > 0 ? usageParts[0] : "",
                    Usageline2 = usageParts.Length > 1 ? usageParts[1] : "",
                    Usageline3 = usageParts.Length > 2 ? usageParts[2] : "",
                    Noteprocessing = GetComponent(SubstandComponents, 5)
                },
                Actualdispense = GetField(adjustedFieldsArray, 8),
                prioritycode = GetField(adjustedFieldsArray, 9),
                Dose = ParseInt(GetField(adjustedFieldsArray, 10)),
                Usageunit = new Usageunit
                {
                    ID = GetComponent(usageUnitComponents, 0),
                    Name = GetComponent(usageUnitComponents, 1)
                },
                RXD12 = GetField(adjustedFieldsArray, 12),
                RXD13 = GetField(adjustedFieldsArray, 13),
                Doctor = new Doctor
                {
                    ID = GetComponent(DoctorComponents, 0),
                    Name = GetComponent(DoctorComponents, 1)
                },
                RXD15 = GetField(adjustedFieldsArray, 15),
                RXD16 = GetField(adjustedFieldsArray, 16),
                RXD17 = GetField(adjustedFieldsArray, 17),
                Prescriptiondate = ParseDateTime(GetField(adjustedFieldsArray, 18)),
                RXD19 = GetField(adjustedFieldsArray, 19),
                RXD20 = GetField(adjustedFieldsArray, 20),
                RXD21 = GetField(adjustedFieldsArray, 21),
                RXD22 = GetField(adjustedFieldsArray, 22),
                RXD23 = GetField(adjustedFieldsArray, 23),
                RXD24 = GetField(adjustedFieldsArray, 24),
                RXD25 = GetField(adjustedFieldsArray, 25),
                Strengthunit = GetField(adjustedFieldsArray, 26),
                Departmentcode = GetField(adjustedFieldsArray, 27),
                Departmentname = GetField(adjustedFieldsArray, 28),
                Orderunitcode = new Orderunitcode
                {
                    Nameeng = GetComponent(OrderunitcodeComponents, 0),
                    Namethai = GetComponent(OrderunitcodeComponents, 1)
                },
                Usagecode = new Usagecode
                {
                    Instructioncode = GetComponent(UsagecodeComponents, 0),
                    RXD3002 = GetComponent(UsagecodeComponents, 1),
                    RXD3003 = GetComponent(UsagecodeComponents, 2),
                    Frequencycode = GetComponent(UsagecodeComponents, 3),
                    Frequencydesc = GetComponent(UsagecodeComponents, 4),
                    RXD3006 = GetComponent(UsagecodeComponents, 5),
                    RXD3007 = GetComponent(UsagecodeComponents, 6)
                },
                IsRXE = true
            };
        }
        private RXD ParseRXD(string[] fields)
        {
            var drugComponents = GetField(fields, 2).Split(COMPONENT_SEPARATOR[0]);
            var staffComponents = GetField(fields, 5).Split(COMPONENT_SEPARATOR[0]);

            // **การแก้ไขหลัก: รวม field 7 ที่ถูกแยกโดย pipe**
            string field7Combined = "";
            List<string> adjustedFields = new List<string>(fields);

            // หา field 7 และรวมกับ field ที่ถัดไป หากมี pipe ใน medicinal properties
            if (fields.Length > 7)
            {
                field7Combined = GetField(fields, 7);

                // ถ้า field 8 ไม่ใช่ตัวเลขหรือ keyword ที่คาดหวัง แสดงว่าเป็นส่วนต่อของ field 7
                int fieldIndex = 8;
                while (fieldIndex < fields.Length &&
                       !string.IsNullOrEmpty(GetField(fields, fieldIndex)) &&
                       !IsValidField8OrLater(GetField(fields, fieldIndex)))
                {
                    // รวม field นี้เข้ากับ field 7
                    field7Combined += GetField(fields, fieldIndex);
                    fieldIndex++;
                }

                // สร้าง array ใหม่โดยรวม field 7 และปรับลำดับที่เหลือ
                adjustedFields.Clear();
                for (int i = 0; i <= 6; i++)
                {
                    adjustedFields.Add(GetField(fields, i));
                }
                adjustedFields.Add(field7Combined); // field 7 ที่รวมแล้ว

                // เพิ่ม field ที่เหลือ
                for (int i = fieldIndex; i < fields.Length; i++)
                {
                    adjustedFields.Add(GetField(fields, i));
                }
            }

            // ใช้ adjustedFields แทน fields
            var adjustedFieldsArray = adjustedFields.ToArray();

            // แยก components ของ field 7 ที่แก้ไขแล้ว
            var SubstandComponents = adjustedFieldsArray[7].Split(COMPONENT_SEPARATOR[0]);
            var usageParts = (GetComponent(SubstandComponents, 4) ?? "")
                            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim())
                            .ToArray();

            var usageUnitComponents = GetField(adjustedFieldsArray, 11).Split(COMPONENT_SEPARATOR[0]);
            var DoctorComponents = GetField(adjustedFieldsArray, 14).Split(COMPONENT_SEPARATOR[0]);
            var OrderunitcodeComponents = GetField(adjustedFieldsArray, 29).Split(COMPONENT_SEPARATOR[0]);
            var UsagecodeComponents = GetField(adjustedFieldsArray, 30).Split(COMPONENT_SEPARATOR[0]);

            return new RXD
            {
                QTY = ParseInt(GetField(adjustedFieldsArray, 1)),
                Dispensegivecode = new Dispensegivecode
                {
                    Dispense = GetComponent(drugComponents, 0),
                    UniqID = GetComponent(drugComponents, 1),
                    RXD203 = GetComponent(drugComponents, 2),
                    Identifier = GetComponent(drugComponents, 3),
                    DrugName = GetComponent(drugComponents, 4),
                    DrugNamePrint = GetComponent(drugComponents, 5),
                    DrugNameThai = GetComponent(drugComponents, 6)
                },
                DateTimeDispensed = ParseDateTime(GetField(adjustedFieldsArray, 3)),
                ActualDispense = ParseInt(GetField(adjustedFieldsArray, 4)),
                Modifystaff = new Modifystaff
                {
                    StaffCode = GetComponent(staffComponents, 0),
                    StaffName = GetComponent(staffComponents, 1)
                },
                Dosageform = GetField(adjustedFieldsArray, 6),
                Substand = new Substand
                {
                    RXD701 = GetComponent(SubstandComponents, 0),
                    Medicinalproperties = GetComponent(SubstandComponents, 1), // ตอนนี้จะได้ "รีชมพูแผงทึบ"
                    Labelhelp = GetComponent(SubstandComponents, 2),
                    RXD704 = GetComponent(SubstandComponents, 3),
                    Usageline1 = usageParts.Length > 0 ? usageParts[0] : "",
                    Usageline2 = usageParts.Length > 1 ? usageParts[1] : "",
                    Usageline3 = usageParts.Length > 2 ? usageParts[2] : "",
                    Noteprocessing = GetComponent(SubstandComponents, 5)
                },
                Actualdispense = GetField(adjustedFieldsArray, 8),
                prioritycode = GetField(adjustedFieldsArray, 9),
                Dose = ParseInt(GetField(adjustedFieldsArray, 10)),
                Usageunit = new Usageunit
                {
                    ID = GetComponent(usageUnitComponents, 0),
                    Name = GetComponent(usageUnitComponents, 1)
                },
                RXD12 = GetField(adjustedFieldsArray, 12),
                RXD13 = GetField(adjustedFieldsArray, 13),
                Doctor = new Doctor
                {
                    ID = GetComponent(DoctorComponents, 0),
                    Name = GetComponent(DoctorComponents, 1)
                },
                RXD15 = GetField(adjustedFieldsArray, 15),
                RXD16 = GetField(adjustedFieldsArray, 16),
                RXD17 = GetField(adjustedFieldsArray, 17),
                Prescriptiondate = ParseDateTime(GetField(adjustedFieldsArray, 18)),
                RXD19 = GetField(adjustedFieldsArray, 19),
                RXD20 = GetField(adjustedFieldsArray, 20),
                RXD21 = GetField(adjustedFieldsArray, 21),
                RXD22 = GetField(adjustedFieldsArray, 22),
                RXD23 = GetField(adjustedFieldsArray, 23),
                RXD24 = GetField(adjustedFieldsArray, 24),
                RXD25 = GetField(adjustedFieldsArray, 25),
                Strengthunit = GetField(adjustedFieldsArray, 26),
                Departmentcode = GetField(adjustedFieldsArray, 27),
                Departmentname = GetField(adjustedFieldsArray, 28),
                Orderunitcode = new Orderunitcode
                {
                    Nameeng = GetComponent(OrderunitcodeComponents, 0),
                    Namethai = GetComponent(OrderunitcodeComponents, 1)
                },
                Usagecode = new Usagecode
                {
                    Instructioncode = GetComponent(UsagecodeComponents, 0),
                    RXD3002 = GetComponent(UsagecodeComponents, 1),
                    RXD3003 = GetComponent(UsagecodeComponents, 2),
                    Frequencycode = GetComponent(UsagecodeComponents, 3),
                    Frequencydesc = GetComponent(UsagecodeComponents, 4),
                    RXD3006 = GetComponent(UsagecodeComponents, 5),
                    RXD3007 = GetComponent(UsagecodeComponents, 6)
                },
                IsRXE = true
            };
        }

        // Helper method เพื่อตรวจสอบว่า field ที่กำลังดูเป็น field ใหม่จริงหรือไม่
        private bool IsValidField8OrLater(string fieldValue)
        {
            if (string.IsNullOrEmpty(fieldValue)) return true;

            // ตรวจสอบรูปแบบที่คาดหวังสำหรับ field 8+ ของ RXE/RXD
            // เช่น field 8 มักจะเป็นค่าว่างหรือตัวเลข
            // field 9 มักจะเป็น "PROUD" หรือ priority code อื่น

            // หาก field นี้มี ^ แสดงว่าน่าจะเป็นส่วนต่อของ field 7
            if (fieldValue.StartsWith("^")) return false;

            // หาก field นี้มีรูปแบบของ component (มี ^^) แสดงว่าน่าจะเป็นส่วนต่อของ field 7  
            if (fieldValue.Contains("^^")) return false;

            // หาก field นี้เป็น "PROUD" หรือคำที่คาดหวัง แสดงว่าเป็น field ใหม่
            if (fieldValue.Equals("PROUD") || fieldValue.All(char.IsDigit)) return true;

            // หาก field นี้มีรูปแบบของ usage instruction แสดงว่าน่าจะเป็นส่วนต่อของ field 7
            if (fieldValue.Contains("รับประทาน") || fieldValue.Contains("เม็ด") || fieldValue.Contains("ครั้ง")) return false;

            return true;
        }

        // ใช้ method เดียวกันสำหรับ ParseRXD โดยเปลี่ยน IsRXE = false
        



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

        private DateTime? ParseDateTime(string dateTimeStr)
        {
            if (string.IsNullOrWhiteSpace(dateTimeStr))
                return null;

            var provider = System.Globalization.CultureInfo.InvariantCulture;

            string[] formats =
            {
                     "yyyyMMddHHmmss",   // เช่น 20240304084624
                      "yyyyMMdd",         // เช่น 20240304
                     "yyyyMMddHH:mm:ss"  // เช่น 2024030408:46:24
                 };

            if (DateTime.TryParseExact(dateTimeStr, formats, provider,
                System.Globalization.DateTimeStyles.None, out DateTime result))
            {
                return result;
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
