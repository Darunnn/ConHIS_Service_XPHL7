using System;
using System.Windows.Forms;
using System.Data;
using ConHIS_Service_XPHL7.Models;

namespace ConHIS_Service_XPHL7
{
    public partial class HL7DetailForm : Form
    {
        private HL7Message _hl7Message;

        public HL7DetailForm(HL7Message hl7Message, string orderNo)
        {
            _hl7Message = hl7Message;
            InitializeComponent();

            this.Text = $"HL7 Message Details - Order: {orderNo}";

            LoadData();
        }

        private void LoadData()
        {
            // MSH Tab
            var mshGrid = CreateDataGridView();
            tabMSH.Controls.Add(mshGrid);
            LoadMSH(mshGrid);

            // PID Tab
            var pidGrid = CreateDataGridView();
            tabPID.Controls.Add(pidGrid);
            LoadPID(pidGrid);

            // PV1 Tab
            var pv1Grid = CreateDataGridView();
            tabPV1.Controls.Add(pv1Grid);
            LoadPV1(pv1Grid);

            // ORC Tab
            var orcGrid = CreateDataGridView();
            tabORC.Controls.Add(orcGrid);
            LoadORC(orcGrid);

            // AL1 Tab
            var al1Grid = CreateDataGridView();
            tabAL1.Controls.Add(al1Grid);
            LoadAL1(al1Grid);

            // RXD Tab
            var rxdGrid = CreateDataGridView();
            tabRXD.Controls.Add(rxdGrid);
            LoadRXD(rxdGrid);

            // RXR Tab
            var rxrGrid = CreateDataGridView();
            tabRXR.Controls.Add(rxrGrid);
            LoadRXR(rxrGrid);

            // NTE Tab
            var nteGrid = CreateDataGridView();
            tabNTE.Controls.Add(nteGrid);
            LoadNTE(nteGrid);
        }

        private DataGridView CreateDataGridView()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            return grid;
        }

        private DataTable CreateFieldValueTable()
        {
            var table = new DataTable();
            table.Columns.Add("Field", typeof(string));
            table.Columns.Add("Value", typeof(string));
            return table;
        }

        private void LoadMSH(DataGridView grid)
        {
            var table = CreateFieldValueTable();
            var msh = _hl7Message?.MessageHeader;

            if (msh != null)
            {
                table.Rows.Add("Encoding Characters", msh.EncodingCharacters);
                table.Rows.Add("Sending Application", msh.SendingApplication);
                table.Rows.Add("Receiving Application", msh.ReceivingApplication);
                table.Rows.Add("Sending Facility", msh.SendingFacility);
                table.Rows.Add("Receiving Facility", msh.ReceivingFacility);
                table.Rows.Add("Message DateTime", msh.MessageDateTime.ToString("yyyy-MM-dd HH:mm:ss"));
                table.Rows.Add("Message Type", msh.MessageType);
                table.Rows.Add("Message Control ID", msh.MessageControlID);
                table.Rows.Add("Processing ID", msh.ProcessingID);
                table.Rows.Add("Version ID", msh.VersionID);
            }

            grid.DataSource = table;
        }

        private void LoadPID(DataGridView grid)
        {
            var table = CreateFieldValueTable();
            var pid = _hl7Message?.PatientIdentification;

            if (pid != null)
            {
                table.Rows.Add("Set ID", pid.SetID);
                table.Rows.Add("Patient ID External (HN)", pid.PatientIDExternal);
                table.Rows.Add("Patient ID Internal", pid.PatientIDInternal);
                table.Rows.Add("Alternate Patient ID", pid.AlternatePatientID);

                if (pid.OfficialName != null)
                {
                    table.Rows.Add("Prefix", pid.OfficialName.Prefix);
                    table.Rows.Add("First Name", pid.OfficialName.FirstName);
                    table.Rows.Add("Last Name", pid.OfficialName.LastName);
                    table.Rows.Add("Middle Name", pid.OfficialName.MiddleName);
                }

                table.Rows.Add("Date of Birth", pid.DateOfBirth?.ToString("yyyy-MM-dd"));
                table.Rows.Add("Sex", pid.Sex);

                if (pid.Address != null)
                {
                    table.Rows.Add("Street Address", pid.Address.StreetAddress);
                    table.Rows.Add("City", pid.Address.City);
                    table.Rows.Add("State/Province", pid.Address.StateOrProvince);
                    table.Rows.Add("Zip/Postal Code", pid.Address.ZipOrPostalCode);
                }

                table.Rows.Add("Phone Number Home", pid.PhoneNumberHome);
                table.Rows.Add("Marital Status", pid.Marital);
                table.Rows.Add("Religion", pid.Religion);

                if (pid.Nationality != null)
                {
                    table.Rows.Add("Nationality", pid.Nationality.Nationality1);
                    table.Rows.Add("Nationality Name", pid.Nationality.NameNationality);
                }
            }

            grid.DataSource = table;
        }

        private void LoadPV1(DataGridView grid)
        {
            var table = CreateFieldValueTable();
            var pv1 = _hl7Message?.PatientVisit;

            if (pv1 != null)
            {
                table.Rows.Add("Set ID", pv1.SetID);
                table.Rows.Add("Patient Class", pv1.PatientClass);

                if (pv1.AssignedPatientLocation != null)
                {
                    table.Rows.Add("Point of Care (Ward Code)", pv1.AssignedPatientLocation.PointOfCare);
                    table.Rows.Add("Room (Ward Desc)", pv1.AssignedPatientLocation.Room);
                    table.Rows.Add("Bed", pv1.AssignedPatientLocation.Bed);
                }

                if (pv1.AttendingDoctor != null)
                {
                    table.Rows.Add("Attending Doctor ID", pv1.AttendingDoctor.ID);
                    table.Rows.Add("Attending Doctor Name", pv1.AttendingDoctor.Name);
                }

                if (pv1.AdmittingDoctor != null)
                {
                    table.Rows.Add("Admitting Doctor ID", pv1.AdmittingDoctor.ID);
                    table.Rows.Add("Admitting Doctor Name", $"{pv1.AdmittingDoctor.Prefix} {pv1.AdmittingDoctor.FirstName} {pv1.AdmittingDoctor.LastName}".Trim());
                }

                if (pv1.PatientType != null)
                {
                    table.Rows.Add("Patient Type ID", pv1.PatientType.ID);
                    table.Rows.Add("Patient Type Name", pv1.PatientType.Name);
                }

                table.Rows.Add("Visit Number (VN)", pv1.VisitNumber);

                if (pv1.FinancialClass != null)
                {
                    table.Rows.Add("Financial Class ID", pv1.FinancialClass.ID);
                    table.Rows.Add("Financial Class Name", pv1.FinancialClass.Name);
                }

                table.Rows.Add("Admit DateTime", pv1.AdmitDateTime?.ToString("yyyy-MM-dd HH:mm:ss"));
                table.Rows.Add("Discharge DateTime", pv1.DischargeDateTime?.ToString("yyyy-MM-dd HH:mm:ss"));
            }

            grid.DataSource = table;
        }

        private void LoadORC(DataGridView grid)
        {
            var table = CreateFieldValueTable();
            var orc = _hl7Message?.CommonOrder;

            if (orc != null)
            {
                table.Rows.Add("Order Control", orc.OrderControl);
                table.Rows.Add("Placer Order Number", orc.PlacerOrderNumber);
                table.Rows.Add("Filler Order Number", orc.FillerOrderNumber);

                if (orc.PlacerGroup != null)
                {
                    table.Rows.Add("Placer Group ID", orc.PlacerGroup.ID);
                    table.Rows.Add("Placer Group Name", orc.PlacerGroup.Name);
                }

                table.Rows.Add("Order Status", orc.OrderStatus);
                table.Rows.Add("Response Flag", orc.ResponseFlag);
                table.Rows.Add("Quantity/Timing", orc.QuantityTiming);
                table.Rows.Add("Transaction DateTime", orc.TransactionDateTime?.ToString("yyyy-MM-dd HH:mm:ss"));
                table.Rows.Add("Entered By", orc.EnteredBy);

                if (orc.VerifiedBy != null)
                {
                    table.Rows.Add("Verified By ID", orc.VerifiedBy.ID);
                    table.Rows.Add("Verified By Name", $"{orc.VerifiedBy.Prefix} {orc.VerifiedBy.FirstName} {orc.VerifiedBy.LastName}".Trim());
                }

                if (orc.OrderingProvider != null)
                {
                    table.Rows.Add("Ordering Provider ID", orc.OrderingProvider.ID);
                    table.Rows.Add("Ordering Provider Name", orc.OrderingProvider.Name);
                }

                table.Rows.Add("Entering Device", orc.EnteringDevice);
                table.Rows.Add("Order Effective DateTime", orc.OrderEffectiveDateTime?.ToString("yyyy-MM-dd HH:mm:ss"));
            }

            grid.DataSource = table;
        }

        private void LoadAL1(DataGridView grid)
        {
            var table = new DataTable();
            table.Columns.Add("Set ID", typeof(string));
            table.Columns.Add("Allergy Type", typeof(string));
            table.Columns.Add("Allergy Name", typeof(string));
            table.Columns.Add("Severity", typeof(string));
            table.Columns.Add("Reaction", typeof(string));
            table.Columns.Add("Identification Date", typeof(string));

            if (_hl7Message?.Allergies != null)
            {
                foreach (var al1 in _hl7Message.Allergies)
                {
                    table.Rows.Add(
                        al1.SetID,
                        al1.AllergyTypeCode,
                        al1.AllergyName,
                        al1.AllergySeverity,
                        al1.AllergyReaction,
                        al1.IdentificationDate?.ToString("yyyy-MM-dd")
                    );
                }
            }

            grid.DataSource = table;
        }

        private void LoadRXD(DataGridView grid)
        {
            var table = new DataTable();
            table.Columns.Add("QTY", typeof(string));
            table.Columns.Add("Drug Code", typeof(string));
            table.Columns.Add("Drug Name", typeof(string));
            table.Columns.Add("DateTime Dispensed", typeof(string));
            table.Columns.Add("Staff", typeof(string));
            table.Columns.Add("Dose", typeof(string));
            table.Columns.Add("Usage Unit", typeof(string));
            table.Columns.Add("Doctor", typeof(string));
            table.Columns.Add("Prescription Date", typeof(string));
            table.Columns.Add("Department", typeof(string));

            if (_hl7Message?.PharmacyDispense != null)
            {
                foreach (var rxd in _hl7Message.PharmacyDispense)
                {
                    string drugCode = rxd.Dispensegivecode?.Dispense ?? "";
                    string drugName = rxd.Dispensegivecode?.DrugName ?? rxd.Dispensegivecode?.DrugNamePrint ?? "";
                    string staff = rxd.Modifystaff != null ? $"{rxd.Modifystaff.StaffCode} - {rxd.Modifystaff.StaffName}" : "";
                    string usageUnit = rxd.Usageunit != null ? $"{rxd.Usageunit.ID} - {rxd.Usageunit.Name}" : "";
                    string doctor = rxd.Doctor != null ? $"{rxd.Doctor.ID} - {rxd.Doctor.Name}" : "";
                    string department = $"{rxd.Departmentcode} - {rxd.Departmentname}";

                    table.Rows.Add(
                        rxd.QTY,
                        drugCode,
                        drugName,
                        rxd.DateTimeDispensed?.ToString("yyyy-MM-dd HH:mm:ss"),
                        staff,
                        rxd.Dose,
                        usageUnit,
                        doctor,
                        rxd.Prescriptiondate?.ToString("yyyy-MM-dd HH:mm:ss"),
                        department
                    );
                }
            }

            grid.DataSource = table;
        }

        private void LoadRXR(DataGridView grid)
        {
            var table = new DataTable();
            table.Columns.Add("Route", typeof(string));
            table.Columns.Add("Site", typeof(string));
            table.Columns.Add("Administration Device", typeof(string));
            table.Columns.Add("Administration Method", typeof(string));
            table.Columns.Add("Routing Instruction", typeof(string));

            if (_hl7Message?.RouteInfo != null)
            {
                foreach (var rxr in _hl7Message.RouteInfo)
                {
                    table.Rows.Add(
                        rxr.Route,
                        rxr.site,
                        rxr.AdministrationDevice,
                        rxr.AdministrationMethod,
                        rxr.RoutingInstruction
                    );
                }
            }

            grid.DataSource = table;
        }

        private void LoadNTE(DataGridView grid)
        {
            var table = new DataTable();
            table.Columns.Add("Set ID", typeof(string));
            table.Columns.Add("Comment Type", typeof(string));
            table.Columns.Add("Comment Note", typeof(string));

            if (_hl7Message?.Notes != null)
            {
                foreach (var nte in _hl7Message.Notes)
                {
                    table.Rows.Add(
                        nte.SetID,
                        nte.CommentType,
                        nte.CommentNote
                    );
                }
            }

            grid.DataSource = table;
        }
    }
}