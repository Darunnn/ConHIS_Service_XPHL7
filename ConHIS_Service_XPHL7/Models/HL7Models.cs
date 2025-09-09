using System;
using System.Collections.Generic;

namespace ConHIS_Service_XPHL7.Models
{
    // Root HL7 Message
    public class HL7Message
    {
        public MSH MessageHeader { get; set; }
        public PID PatientIdentification { get; set; }
        public PV1 PatientVisit { get; set; }
        public ORC CommonOrder { get; set; }
        public List<AL1> Allergies { get; set; } = new List<AL1>();
        public List<RXD> PharmacyDispense { get; set; } = new List<RXD>();
        public List<RXR> RouteInfo { get; set; } = new List<RXR>();
        public List<NTE> Notes { get; set; } = new List<NTE>();
    }

    // ================== MSH ==================
    public class MSH
    {
        public string EncodingCharacters { get; set; } = "^\\&"; // MSH-2
        public string SendingApplication { get; set; } = "DRUGORDER"; // MSH-3
        public string SendingFacility { get; set; } = "BMS-HOSxP";    // MSH-4
        public string ReceivingApplication { get; set; } = "DrugDispense"; // MSH-5
        public string ReceivingFacility { get; set; }   // MSH-6
        public DateTime MessageDateTime { get; set; }   // MSH-7
        public string MessageType { get; set; }         // MSH-8 (ORM^O01, etc.)
        public string MessageControlID { get; set; }    // MSH-9
        public string ProcessingID { get; set; } = "P"; // MSH-10
        public string VersionID { get; set; } = "2.3";  // MSH-11
    }

    // ================== PID ==================
    public class PID
    {
        public string SetID { get; set; } = "1";              // PID-1
        public string PatientIDExternal { get; set; }         // PID-2 HN
        public string PatientIDInternal { get; set; }         // PID-3 HN
        public string AlternatePatientID { get; set; }        // PID-4 (Not Used)

        public PatientName OfficialName { get; set; } = new PatientName(); // PID-5 (Not Used)
        public string Mothermaidenname { get; set; } // PID-6(Not Used)
        public DateTime? DateOfBirth { get; set; }            // PID-7
        public string Sex { get; set; }          // PID-8 (Not Used)
        public PatientName AliasName { get; set; } = new PatientName();    // PID-9
        public  string Race { get; set; }                // PID-10 (Not Used)
        public PatientAddress Address { get; set; } = new PatientAddress();  // PID-11
        public string Country { get; set; }               // PID-12
        public string PhoneNumberHome { get; set; }           // PID-13
    }

    public class PatientName
    {
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string Suffix { get; set; }
        public string Prefix { get; set; }
        public string Degree { get; set; }
        public string NameTypeCode { get; set; }
        public string NameRepresentationCode { get; set; }
    }

    public class PatientAddress
    {
        public string StreetAddress { get; set; }
        public string OtherDesignation { get; set; }
        public string City { get; set; }
        public string StateOrProvince { get; set; }
        public string ZipOrPostalCode { get; set; }
        
    }

    // ================== PV1 ==================
    /// <summary>
    /// PV1 – Patient Visit Segment
    /// </summary>
    public class PV1
    {
        // 1 Set ID – PV1
        public string SetID { get; set; } = "1";

        // 2 Patient Class (O – Outpatient)
        public string PatientClass { get; set; }

        // 3 Assigned Patient Location
        public AssignedLocation AssignedPatientLocation { get; set; } = new AssignedLocation();

        // 4 Admission Type (Not Used)
        public string AdmissionType { get; set; }

        // 5 Preadmit Number (Not Used)
        public string PreadmitNumber { get; set; }

        // 6 Prior Patient Location (Not Used)
        public string PriorPatientLocation { get; set; }

        // 7 Attending Doctor (Not Used, ไม่ต้องสร้าง XCN เต็ม ๆ)
        public AttendingDoctor AttendingDoctor { get; set; } = new AttendingDoctor();

        // 8 Referring Doctor (Not Used)
        public string ReferringDoctor { get; set; }

        // 9 Consulting Doctor (Not Used)
        public string ConsultingDoctor { get; set; }

        // 10 Hospital Service (Not Used)
        public string HospitalService { get; set; }

        // 11 Temporary Location (Not Used)
        public string TemporaryLocation { get; set; }

        // 12 Preadmit Test Indicator (Not Used)
        public string PreadmitTestIndicator { get; set; }

        // 13 Readmission Indicator (Not Used)
        public string ReadmissionIndicator { get; set; }

        // 14 Admit Source (Not Used)
        public string AdmitSource { get; set; }

        // 15 Ambulatory Status (Not Used)
        public string AmbulatoryStatus { get; set; }

        // 16 VIP Indicator (Not Used)
        public string VIPIndicator { get; set; }

        // 17 Admitting Doctor (ใช้จริง → composite XCN)
        public AdmittingDoctor AdmittingDoctor { get; set; } = new AdmittingDoctor();


        // 18 Patient Type (ใช้จริง → composite)
        public PatientType PatientType { get; set; } = new PatientType();

        // 19 Visit Number (Not Used)
        public string VisitNumber { get; set; }

        // 20–52 ทั้งหมด Not Used (เก็บเป็น string/DateTime/decimal? แบบสั้น ๆ)
        public string FinancialClass { get; set; }
        public string ChargePriceIndicator { get; set; }
        public string CourtesyCode { get; set; }
        public string CreditRating { get; set; }
        public string ContractCode { get; set; }
        public DateTime? ContractEffectiveDate { get; set; }
        public decimal? ContractAmount { get; set; }
        public string ContractPeriod { get; set; }
        public string InterestCode { get; set; }
        public string TransferToBadDebtCode { get; set; }
        public DateTime? TransferToBadDebtDate { get; set; }
        public string BadDebtAgencyCode { get; set; }
        public decimal? BadDebtTransferAmount { get; set; }
        public decimal? BadDebtRecoveryAmount { get; set; }
        public string DeleteAccountIndicator { get; set; }
        public DateTime? DeleteAccountDate { get; set; }
        public string DischargeDisposition { get; set; }
        public string DischargedToLocation { get; set; }
        public string DietType { get; set; }
        public string ServicingFacility { get; set; }
        public string BedStatus { get; set; }
        public string AccountStatus { get; set; }
        public string PendingLocation { get; set; }
        public string PriorTemporaryLocation { get; set; }
        public DateTime? AdmitDateTime { get; set; }
        public DateTime? DischargeDateTime { get; set; }
        public decimal? CurrentPatientBalance { get; set; }
        public decimal? TotalCharges { get; set; }
        public decimal? TotalAdjustments { get; set; }
        public decimal? TotalPayments { get; set; }
        public string AlternateVisitID { get; set; }
        public string VisitIndicator { get; set; }
        public string OtherHealthcareProvider { get; set; }
    }

 
    public class AssignedLocation
    {
        // 3.1 Point Of Care
        public string PointOfCare { get; set; }   // Depcode
                                               // 3.2 Room (Not Used)
        public string Room { get; set; }
        // 3.3 Bed (Not Used)
        public string Bed { get; set; }
    }

    public class AttendingDoctor
    {
        public string ID { get; set; }        // 7.1
        public string LastName { get; set; }  // 7.2
        public string FirstName { get; set; } // 7.3
        public string MiddleName { get; set; } // 7.4
        public string Suffix { get; set; }    // 7.5
        public string Prefix { get; set; }    // 7.6
    }
    public class AdmittingDoctor
    {
        public string ID { get; set; }        // 17.1
        public string LastName { get; set; }  // 17.2
        public string FirstName { get; set; } // 17.3
        public string MiddleName { get; set; } // 17.4
        public string Suffix { get; set; }    // 17.5
        public string Prefix { get; set; }    // 17.6
    }

    public class PatientType
    {
        public string ID { get; set; }     // pttype
        public string Name { get; set; }   // pttype.name
    }


    // Replace target-typed object creation with explicit type
    public class ORC
    {
        // 1 Order Control (NW, RP)
        public string OrderControl { get; set; }

        // 2 Placer Order Number (presc_id)
        public string PlacerOrderNumber { get; set; }

        // 3 Filler Order Number (VN)
        public string FillerOrderNumber { get; set; }

        // 4 Placer Group (Not Used)
        public string PlacerGroupID { get; set; }
        public string PlacerGroupName { get; set; }

        // 5 Order Status (Dispensestatus) - Default = 0
        public string OrderStatus { get; set; } = "0";

        // 6 Response Flag (Status) - Default = 0
        public string ResponseFlag { get; set; } = "0";

        // 7 Quantity/Timing (จำนวนยาที่สั่ง)
        public int QuantityTiming { get; set; }

        // 8 Parent (Not Used)
        public string Parent { get; set; }

        // 9 Date/Time of Transaction (วันที่ใบสั่งยา)
        public DateTime? TransactionDateTime { get; set; }

        // 10 Entered By (Not Used)
        public string EnteredBy { get; set; }

        // 11 Verified By (ผู้บันทึกจัดยา)
        public VerifiedBy VerifiedBy { get; set; } = new VerifiedBy();

        // 12 Ordering Provider (Not Used)
        public OrderingProvider OrderingProvider { get; set; } = new OrderingProvider();

        // 13 Enterer’s Location (Not Used)
        public string EnterersLocationID { get; set; }
        public string EnterersLocationName { get; set; }

        // 14 Call Back Phone Number (Not Used)
        public string CallBackPhoneNumber { get; set; }

        // 15 Order Effective Date/Time (Not Used)
        public DateTime? OrderEffectiveDateTime { get; set; }

        // 16 Order Control Code Reason (Not Used)
        public string OrderControlCodeReason { get; set; }

        // 17 Entering Organization (Not Used)
        public string EnteringOrganization { get; set; }

        // 18 Entering Device (Computer name)
        public string EnteringDevice { get; set; }

        // 19 Action By (Not Used)
        public string ActionBy { get; set; }

        // 20 Advanced Code Beneficiary Notice (Not Used)
        public string AdvancedCodeBeneficiaryNotice { get; set; }

        // 21 Ordering Facility Name (Not Used)
        public string OrderingFacilityName { get; set; }

        // 22 Ordering Facility Address (Not Used)
        public string OrderingFacilityAddress { get; set; }
    }

    public class VerifiedBy
    {
        public string ID { get; set; }        // 11.1
        public string LastName { get; set; }  // 11.2
        public string FirstName { get; set; } // 11.3
        public string MiddleName { get; set; } // 11.4
        public string Suffix { get; set; }    // 11.5
        public string Prefix { get; set; }    // 11.6
    }
    public class OrderingProvider
    {
        public string ID { get; set; }        // 12.1
        public string LastName { get; set; }  // 12.2
        public string FirstName { get; set; } // 12.3
        public string MiddleName { get; set; } // 12.4
        public string Suffix { get; set; }    // 12.5
        public string Prefix { get; set; }    // 12.6
    }
    // ================== AL1 ==================
    public class AL1
    {
        public string SetID { get; set; }                     // AL1-1 HN
        public string AllergyTypeCode { get; set; } = "DA";   // AL1-2
        public string AllergyName { get; set; }               // AL1-3 Drug
        public string AllergySeverity { get; set; }           // AL1-4 Severity
        public string AllergyReaction { get; set; }           // AL1-5 Symptom
        public DateTime? IdentificationDate { get; set; }     // AL1-6
    }

    // ================== RXD ==================
    public class RXD
    {
        public int SetID { get; set; }                        // RXD-1: Set ID
        public Dispensegivecode Dispensegivecode { get; set; } = new Dispensegivecode(); // RXD-2: Drug ID / Dispense Give Code

        public DateTime? DateTimeDispensed { get; set; }      // RXD-3: Date/Time Dispensed
        public int ActualDispense { get; set; }              // RXD-4: Actual Dispense
        public Modifystaff Modifystaff { get; set; } = new Modifystaff(); // RXD-5: Modified Staff
        public int QTY { get; set; }                          // RXD-6: Quantity Dispensed
        public int Dose { get; set; }                         // RXD-7: Dose Amount
        public string UsageCODE { get; set; }                 // RXD-8: Usage Code

        public string UsageLine1 { get; set; }                // RXD-9: Usage Line 1
        public string UsageLine2 { get; set; }                // RXD-10: Usage Line 2
        public string UsageLine3 { get; set; }                // RXD-11: Usage Line 3
        public string UsageLine4 { get; set; }                // RXD-12: Usage Line 4
        public string DosageForm { get; set; }                // RXD-13: Dosage Form

        public UsageUnit UsageUnit { get; set; } = new UsageUnit();        // RXD-14: Usage Unit
        public FrequencyInfo Frequency { get; set; } = new FrequencyInfo(); // RXD-15: Frequency
        public TimeInfo Time { get; set; } = new TimeInfo();               // RXD-16: Time
        public string StrengthUnit { get; set; }                           // RXD-17: Strength Unit

        public DepartmentOrder Department { get; set; } = new DepartmentOrder(); // RXD-18: Department Order
        public DoctorOrder Doctor { get; set; } = new DoctorOrder();             // RXD-19: Doctor Order

        public SubstandInfo Substand { get; set; } = new SubstandInfo();         // RXD-20: Substand (Drug Property / Label Help)

        public string FinanceStatus { get; set; } // RXD-21: Finance Status (Y = Clear, N = Not Clear)
        public string LightProtect { get; set; } // RXD-22: Light Protect (Not Used)
        public string DrugType { get; set; }     // RXD-23: Drug Type (ED/NED)
        public string DispensePackageMethod { get; set; } // RXD-24: Dispense Package Method (Not Used)
        public DateTime? StartDatetime { get; set; } // RXD-25: Start Datetime (Not Used)
        public DateTime? EndDatetime { get; set; }   // RXD-26: End Datetime (Not Used)
        public decimal TotalPrice { get; set; }      // RXD-27: Total Price
    }

    public class Dispensegivecode
    {
        public string Identifier { get; set; }   // RXD-2.1: Drug Identifier
        public string DrugName { get; set; }     // RXD-2.2: Drug Name
        public string DrugNamePrint { get; set; } // RXD-2.3: Drug Name Print
        public string DrugNameThai { get; set; } // RXD-2.4: Drug Name Thai
    }

    public class Modifystaff
    {
        public string StaffCode { get; set; }   // RXD-5.1: Staff Code
        public string StaffName { get; set; }   // RXD-5.2: Staff Name
    }

    public class UsageUnit
    {
        public string Code { get; set; }     // RXD-14.1: Usage Unit Code
        public string Name { get; set; }     // RXD-14.2: Usage Unit Name
        public string UnitName { get; set; } // RXD-14.3: Unit Name
    }

    public class FrequencyInfo
    {
        public string FrequencyID { get; set; }   // RXD-15.1: Frequency ID
        public string FrequencyName { get; set; } // RXD-15.2: Frequency Name
    }

    public class TimeInfo
    {
        public string TimeID { get; set; }   // RXD-16.1: Time ID
        public string TimeName { get; set; } // RXD-16.2: Time Name
    }

    public class DepartmentOrder
    {
        public string DepartmentCode { get; set; } // RXD-18.1: Department Code
        public string DepartmentName { get; set; } // RXD-18.2: Department Name
    }

    public class DoctorOrder
    {
        public string DoctorCode { get; set; } // RXD-19.1: Doctor Code
        public string DoctorName { get; set; } // RXD-19.2: Doctor Name
    }

    public class SubstandInfo
    {
        public string DrugProperty { get; set; } // RXD-20.1: Drug Property (สรรพคุณยา)
        public string LabelHelp { get; set; }    // RXD-20.2: Label Help (ฉลากช่วย)
    }

    // ================== RXR ==================
    public class RXR
    {
        public string Route { get; set; }                     // RXR-1 Drug ID
        public string site { get; set; }                      // RXR-2 Site
        public string AdministrationDevice { get; set; }      // RXR-3 Status Device
        public string AdministrationMethod { get; set; }      // RXR-4 Method
        public string RoutingInstruction { get; set; }        // RXR-5 Instruction
    }

    // ================== NTE ==================
    public class NTE
    {
        public int SetID { get; set; }                        // NTE-1
        public string CommentType { get; set; }               // NTE-2 (ช่วงเวลาใช้ยา)
        public string CommentNote { get; set; }               // NTE-3 (หมายเหตุ)
    }
}
