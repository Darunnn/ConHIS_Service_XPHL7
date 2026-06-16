using System;
using System.Collections.Generic;

namespace ConHIS_Service_XPHL7.Models
{
    // Root HL7 Message - IPD
    public class HL7MessageIPD
    {
        public MSH_IPD MessageHeader { get; set; }
        public PID_IPD PatientIdentification { get; set; }
        public PV1_IPD PatientVisit { get; set; }
        public ORC_IPD CommonOrder { get; set; }
        public List<AL1_IPD> Allergies { get; set; } = new List<AL1_IPD>();
        public List<RXD_IPD> PharmacyDispense { get; set; } = new List<RXD_IPD>();
        public List<RXR_IPD> RouteInfo { get; set; } = new List<RXR_IPD>();
        public List<NTE_IPD> Notes { get; set; } = new List<NTE_IPD>();
    }

    // ================== MSH - IPD ==================
    public class MSH_IPD
    {
        public string EncodingCharacters { get; set; }   // MSH-1
        public string SendingApplication { get; set; }   // MSH-2
        public string ReceivingApplication { get; set; } // MSH-3
        public string SendingFacility { get; set; }      // MSH-4
        public string ReceivingFacility { get; set; }    // MSH-5
        public DateTime MessageDateTime { get; set; }    // MSH-6 f_import_createtime*
        public string Security { get; set; }             // MSH-7 (Not Used)
        public string MessageType { get; set; }          // MSH-8 (ORM^O01, etc.)
        public string MessageControlID { get; set; }     // MSH-9
        public string ProcessingID { get; set; }         // MSH-10
        public string VersionID { get; set; }            // MSH-11
        public string MSH12 { get; set; }
        public string MSH13 { get; set; }
        public string MSH14 { get; set; }
        public string MSH15 { get; set; }
        public string MSH16 { get; set; }
        public string MSH17 { get; set; }
        public string MSH18 { get; set; }
    }

    // ================== PID - IPD ==================
    public class PID_IPD
    {
        public int SetID { get; set; }                                          // PID-1
        public string PatientIDExternal { get; set; }                          // PID-2 f_hn
        public string PatientIDInternal { get; set; }                          // PID-3 f_hn
        public string AlternatePatientID { get; set; }                         // PID-4 f_patient_idcard*
        public PatientName_IPD OfficialName { get; set; } = new PatientName_IPD(); // PID-5
        public string Mothermaidenname { get; set; }                           // PID-6
        public DateTime? DateOfBirth { get; set; }                             // PID-7 f_patientdob
        public string Sex { get; set; }                                        // PID-8 f_sex
        public PatientName_IPD AliasName { get; set; } = new PatientName_IPD(); // PID-9
        public string Race { get; set; }                                       // PID-10
        public PatientAddress_IPD Address { get; set; } = new PatientAddress_IPD(); // PID-11
        public string Country { get; set; }                                    // PID-12
        public string PhoneNumberHome { get; set; }                            // PID-13
        public string PID14 { get; set; }
        public string PID15 { get; set; }
        public string Marital { get; set; }                                    // PID-16
        public string Religion { get; set; }                                   // PID-17
        public string PID18 { get; set; }
        public string PID19 { get; set; }
        public string PID20 { get; set; }
        public string PID21 { get; set; }
        public string PID22 { get; set; }
        public string PID23 { get; set; }
        public string PID24 { get; set; }
        public string PID25 { get; set; }
        public string PID26 { get; set; }
        public string PID27 { get; set; }
        public Nationality_IPD Nationality { get; set; } = new Nationality_IPD(); // PID-28
        public string PID29 { get; set; }
        public string PID30 { get; set; }
    }

    public class PatientName_IPD
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

    public class PatientAddress_IPD
    {
        public string StreetAddress { get; set; }
        public string OtherDesignation { get; set; }
        public string City { get; set; }
        public string StateOrProvince { get; set; }
        public string ZipOrPostalCode { get; set; }
    }

    public class Nationality_IPD
    {
        public string Nationality1 { get; set; }
        public string NameNationality { get; set; }
    }

    // ================== PV1 - IPD ==================
    public class PV1_IPD
    {
        public string SetID { get; set; } = "1";
        public string PatientClass { get; set; }
        public AssignedLocation_IPD AssignedPatientLocation { get; set; } = new AssignedLocation_IPD();
        public string AdmissionType { get; set; }
        public string PreadmitNumber { get; set; }
        public string PriorPatientLocation { get; set; }
        public AttendingDoctor_IPD AttendingDoctor { get; set; } = new AttendingDoctor_IPD();
        public ReferringDoctor_IPD ReferringDoctor { get; set; } = new ReferringDoctor_IPD();
        public string ConsultingDoctor { get; set; }
        public string HospitalService { get; set; }
        public string TemporaryLocation { get; set; }
        public string PreadmitTestIndicator { get; set; }
        public string ReadmissionIndicator { get; set; }
        public string AdmitSource { get; set; }
        public string AmbulatoryStatus { get; set; }
        public string VIPIndicator { get; set; }
        public AdmittingDoctor_IPD AdmittingDoctor { get; set; } = new AdmittingDoctor_IPD();
        public PatientType_IPD PatientType { get; set; } = new PatientType_IPD();
        public string VisitNumber { get; set; }  // f_vn / f_an
        public FinancialClass_IPD FinancialClass { get; set; } = new FinancialClass_IPD(); // PV1-20
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
        public DateTime? AdmitDateTime { get; set; }   // PV1-44 f_admitdate*
        public DateTime? DischargeDateTime { get; set; }
        public decimal? CurrentPatientBalance { get; set; }
        public decimal? TotalCharges { get; set; }
        public decimal? TotalAdjustments { get; set; }
        public decimal? TotalPayments { get; set; }
        public string AlternateVisitID { get; set; }
        public string VisitIndicator { get; set; }
        public string OtherHealthcareProvider { get; set; }
    }

    public class AssignedLocation_IPD
    {
        public string PointOfCare { get; set; }  // 3.1 f_wardcode
        public string Room { get; set; }          // 3.2 f_warddesc
        public string Bed { get; set; }           // 3.3
    }

    public class AttendingDoctor_IPD
    {
        public string ID { get; set; }
        public string Name { get; set; }
    }

    public class ReferringDoctor_IPD
    {
        public string ID { get; set; }
        public string Name { get; set; }
    }

    public class AdmittingDoctor_IPD
    {
        public string ID { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string Suffix { get; set; }
        public string Prefix { get; set; }
    }

    public class PatientType_IPD
    {
        public string ID { get; set; }
        public string Name { get; set; }
    }

    public class FinancialClass_IPD
    {
        public string ID { get; set; }
        public string Name { get; set; }
    }

    // ================== ORC - IPD ==================
    public class ORC_IPD
    {
        public string OrderControl { get; set; }         // f_status (NW, RP)
        public string PlacerOrderNumber { get; set; }    // f_prescriptionno
        public string FillerOrderNumber { get; set; }
        public PlacerGroup_IPD PlacerGroup { get; set; } = new PlacerGroup_IPD();
        public string OrderStatus { get; set; }
        public string ResponseFlag { get; set; }
        public int QuantityTiming { get; set; }
        public string Parent { get; set; }
        public DateTime? TransactionDateTime { get; set; } // f_ordercreatedate
        public string EnteredBy { get; set; }              // f_patientname (*ถ้า PID ไม่มีส่งมา)
        public VerifiedBy_IPD VerifiedBy { get; set; } = new VerifiedBy_IPD();
        public OrderingProvider_IPD OrderingProvider { get; set; } = new OrderingProvider_IPD();
        public string EnterersLocation { get; set; }
        public string CallBackPhoneNumber { get; set; }
        public DateTime? OrderEffectiveDateTime { get; set; }
        public string OrderControlCodeReason { get; set; }
        public string EnteringOrganization { get; set; }
        public string EnteringDevice { get; set; }
        public string ActionBy { get; set; }
        public string AdvancedCodeBeneficiaryNotice { get; set; }
        public string OrderingFacilityName { get; set; }
        public string OrderingFacilityAddress { get; set; }
    }

    public class PlacerGroup_IPD
    {
        public string ID { get; set; }
        public string Name { get; set; }
    }

    public class VerifiedBy_IPD
    {
        public string ID { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string Suffix { get; set; }
        public string Prefix { get; set; }
    }

    public class OrderingProvider_IPD
    {
        public string ID { get; set; }
        public string OrderingProvider1 { get; set; }
        public string Name { get; set; }               // f_useracceptby (*ถ้า RXE ไม่มีส่งมา)
        public string OrderingProvider3 { get; set; }
        public string OrderingProvider4 { get; set; }
        public string OrderingProvider5 { get; set; }
        public string OrderingProvider6 { get; set; }
        public string OrderingProvider7 { get; set; }
        public string OrderingProvider8 { get; set; }
        public string OrderingProvider9 { get; set; }
        public string OrderingProvider10 { get; set; }
        public string OrderingProvider11 { get; set; }
        public string OrderingProvider12 { get; set; }
        public string OrderingProvider13 { get; set; }
        public string OrderingProvider14 { get; set; }
    }

    // ================== AL1 - IPD ==================
    public class AL1_IPD
    {
        public string SetID { get; set; }
        public string AllergyTypeCode { get; set; } = "DA";
        public string AllergyName { get; set; }
        public string AllergySeverity { get; set; }
        public string AllergyReaction { get; set; }
        public DateTime? IdentificationDate { get; set; }
    }

    // ================== RXD - IPD ==================
    // IPD: ใช้ DrugNamePrint สำหรับ f_orderitemnameTH (ต่างจาก OPD ที่ใช้ DrugNameThai)
    public class RXD_IPD
    {
        public bool IsRXE { get; set; } = false;
        public int QTY { get; set; }
        public Dispensegivecode_IPD Dispensegivecode { get; set; } = new Dispensegivecode_IPD();
        public DateTime? DateTimeDispensed { get; set; }
        public int RXD4 { get; set; }
        public Modifystaff_IPD Modifystaff { get; set; } = new Modifystaff_IPD();
        public string Dosageform { get; set; }
        public Substand_IPD Substand { get; set; } = new Substand_IPD();
        public string Actualdispense { get; set; }
        public string prioritycode { get; set; }
        public int Dose { get; set; }
        public Usageunit_IPD Usageunit { get; set; } = new Usageunit_IPD();
        public string RXD12 { get; set; }
        public string RXD13 { get; set; }
        public Doctor_IPD Doctor { get; set; } = new Doctor_IPD();
        public string RXD15 { get; set; }
        public string RXD16 { get; set; }
        public string RXD17 { get; set; }
        public string RXD18 { get; set; }
        public string RXD19 { get; set; }
        public string RXD20 { get; set; }
        public string RXD21 { get; set; }
        public string RXD22 { get; set; }
        public string RXD23 { get; set; }
        public string RXD24 { get; set; }
        public string RXD25 { get; set; }
        public string Strengthunit { get; set; }     // RXD-26 dosagetext
        public string Departmentcode { get; set; }   // RXD-27 f_pharmacylocationcode
        public string Departmentname { get; set; }   // RXD-28 f_pharmacylocationdesc
        public Orderunitcode_IPD Orderunitcode { get; set; } = new Orderunitcode_IPD();
        public Usagecode_IPD Usagecode { get; set; } = new Usagecode_IPD();
        public string RXD31 { get; set; }
        public string RXD32 { get; set; }
        public string RXD33 { get; set; }
    }

    public class Dispensegivecode_IPD
    {
        public string Dispense { get; set; }        // RXD-2.1
        public string UniqID { get; set; }          // RXD-2.2
        public string RXD203 { get; set; }          // RXD-2.3
        public string Identifier { get; set; }      // RXD-2.4
        public string DrugName { get; set; }        // RXD-2.5
        /// <summary>
        /// IPD: ใช้ DrugNamePrint เป็น f_orderitemnameTH
        /// (OPD ใช้ DrugNameThai แทน)
        /// </summary>
        public string DrugNamePrint { get; set; }   // RXD-2.6
        public string DrugNameThai { get; set; }    // RXD-2.7 (เก็บไว้แต่ IPD ไม่ได้ใช้ใน f_orderitemnameTH)
        public string DrugUnit { get; set; }
    }

    public class Modifystaff_IPD
    {
        public string StaffCode { get; set; }
        public string StaffName { get; set; }
    }

    public class Substand_IPD
    {
        public string RXD701 { get; set; }
        public string Medicinalproperties { get; set; }
        public string Labelhelp { get; set; }
        public string RXD704 { get; set; }
        public string Usageline1 { get; set; }
        public string Usageline2 { get; set; }
        public string Usageline3 { get; set; }
        public string Noteprocessing { get; set; }
    }

    public class Usageunit_IPD
    {
        public string ID { get; set; }
        public string Name { get; set; }
    }

    public class Doctor_IPD
    {
        public string ID { get; set; }
        public string Name { get; set; }
    }

    public class Orderunitcode_IPD
    {
        public string Nameeng { get; set; }
        public string Namethai { get; set; }
    }

    public class Usagecode_IPD
    {
        public string Instructioncode { get; set; }
        public string RXD3002 { get; set; }
        public string RXD3003 { get; set; }
        public string Frequencycode { get; set; }
        public string Frequencydesc { get; set; }
        public string RXD3006 { get; set; }
        public string RXD3007 { get; set; }
    }

    // ================== RXR - IPD ==================
    public class RXR_IPD
    {
        public string Route { get; set; }
        public string site { get; set; }
        public int AdministrationDevice { get; set; }
        public string AdministrationMethod { get; set; }
        public string RoutingInstruction { get; set; }
    }

    // ================== NTE - IPD ==================
    public class NTE_IPD
    {
        public int SetID { get; set; }
        public string CommentType { get; set; }
        public string CommentNote { get; set; }
    }
}