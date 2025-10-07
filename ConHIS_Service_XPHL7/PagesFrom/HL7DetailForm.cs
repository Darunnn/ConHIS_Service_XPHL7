using System;
using System.Windows.Forms;
using System.Data;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using ConHIS_Service_XPHL7.Models;

namespace ConHIS_Service_XPHL7
{
    public partial class HL7DetailForm : Form
    {
        private HL7Message _hl7Message;
        private Utils.LogManager _logManager;

        public HL7DetailForm(HL7Message hl7Message, string orderNo)
        {
            _hl7Message = hl7Message;
            _logManager = new Utils.LogManager();
            InitializeComponent();

            this.Text = $"HL7 Message Details - Order: {orderNo}";

            LoadData();
        }

        private void LoadData()
        {
            // MSH Tab
            var mshGrid = CreateDataGridView();
            tabMSH.Controls.Add(mshGrid);
            LoadObject(mshGrid, _hl7Message?.MessageHeader);

            // PID Tab
            var pidGrid = CreateDataGridView();
            tabPID.Controls.Add(pidGrid);
            LoadObject(pidGrid, _hl7Message?.PatientIdentification);

            // PV1 Tab
            var pv1Grid = CreateDataGridView();
            tabPV1.Controls.Add(pv1Grid);
            LoadObject(pv1Grid, _hl7Message?.PatientVisit);

            // ORC Tab
            var orcGrid = CreateDataGridView();
            tabORC.Controls.Add(orcGrid);
            LoadObject(orcGrid, _hl7Message?.CommonOrder);

            // AL1 Tab
            var al1Grid = CreateDataGridView();
            tabAL1.Controls.Add(al1Grid);
            LoadCollection(al1Grid, _hl7Message?.Allergies);

            // RXD Tab
            var rxdGrid = CreateDataGridView();
            tabRXD.Controls.Add(rxdGrid);
            LoadCollection(rxdGrid, _hl7Message?.PharmacyDispense);

            // RXR Tab
            var rxrGrid = CreateDataGridView();
            tabRXR.Controls.Add(rxrGrid);
            LoadCollection(rxrGrid, _hl7Message?.RouteInfo);

            // NTE Tab
            var nteGrid = CreateDataGridView();
            tabNTE.Controls.Add(nteGrid);
            LoadCollection(nteGrid, _hl7Message?.Notes);
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

        #region Generic Load Methods with Reflection

        // Generic method to load simple objects (non-collection)
        private void LoadObject(DataGridView grid, object obj, string prefix = "")
        {
            var table = CreateFieldValueTable();

            if (obj != null)
            {
                LoadObjectProperties(table, obj, prefix);
            }

            grid.DataSource = table;
        }

        // Recursive method to load properties
        private void LoadObjectProperties(DataTable table, object obj, string prefix)
        {
            if (obj == null) return;

            var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(obj);
                    var fieldName = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";

                    if (value == null)
                    {
                        table.Rows.Add(fieldName, "");
                    }
                    else if (IsSimpleType(prop.PropertyType))
                    {
                        // Simple types (string, int, bool, DateTime, etc.)
                        table.Rows.Add(fieldName, value?.ToString() ?? "");
                    }
                    else if (prop.PropertyType.IsClass && !IsCollection(prop.PropertyType))
                    {
                        // Complex types - recursive (but not collections)
                        LoadObjectProperties(table, value, fieldName);
                    }
                }
                catch (Exception ex)
                {
                    // Log error and skip properties that can't be accessed
                    _logManager.LogError($"Error accessing property '{prop.Name}' in LoadObjectProperties", ex);
                }
            }
        }

        // Generic method to load collections
        private void LoadCollection<T>(DataGridView grid, IEnumerable<T> collection)
        {
            if (collection == null)
            {
                grid.DataSource = null;
                return;
            }

            var table = new DataTable();
            var type = typeof(T);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Create columns dynamically
            foreach (var prop in properties)
            {
                try
                {
                    if (IsSimpleType(prop.PropertyType))
                    {
                        table.Columns.Add(prop.Name, typeof(string));
                    }
                    else if (prop.PropertyType.IsClass && !IsCollection(prop.PropertyType))
                    {
                        // For complex properties, add sub-properties as columns
                        var subProperties = prop.PropertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        foreach (var subProp in subProperties)
                        {
                            if (IsSimpleType(subProp.PropertyType))
                            {
                                table.Columns.Add($"{prop.Name}.{subProp.Name}", typeof(string));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error and skip properties that can't be accessed
                    _logManager.LogError($"Error creating column for property '{prop.Name}' in LoadCollection", ex);
                }
            }

            // Add rows
            foreach (var item in collection)
            {
                var row = table.NewRow();

                foreach (var prop in properties)
                {
                    try
                    {
                        var value = prop.GetValue(item);

                        if (value == null)
                        {
                            if (table.Columns.Contains(prop.Name))
                                row[prop.Name] = "";
                        }
                        else if (IsSimpleType(prop.PropertyType))
                        {
                            if (table.Columns.Contains(prop.Name))
                            {
                                row[prop.Name] = value?.ToString() ?? "";
                            }
                        }
                        else if (prop.PropertyType.IsClass && value != null && !IsCollection(prop.PropertyType))
                        {
                            // Handle complex properties
                            var subProperties = prop.PropertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                            foreach (var subProp in subProperties)
                            {
                                try
                                {
                                    var subValue = subProp.GetValue(value);
                                    var columnName = $"{prop.Name}.{subProp.Name}";

                                    if (table.Columns.Contains(columnName))
                                    {
                                        if (subValue == null)
                                        {
                                            row[columnName] = "";
                                        }
                                        else
                                        {
                                            row[columnName] = subValue?.ToString() ?? "";
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // Log error and skip sub-properties that can't be accessed
                                    _logManager.LogError($"Error accessing sub-property '{subProp.Name}' of property '{prop.Name}' in LoadCollection", ex);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error and skip properties that can't be accessed
                        _logManager.LogError($"Error accessing property '{prop.Name}' in LoadCollection", ex);
                    }
                }

                table.Rows.Add(row);
            }

            grid.DataSource = table;
        }

        // Helper method to check if type is simple
        private bool IsSimpleType(Type type)
        {
            return type.IsPrimitive
                || type.IsEnum
                || type == typeof(string)
                || type == typeof(decimal)
                || type == typeof(DateTime)
                || type == typeof(DateTime?)
                || type == typeof(decimal?)
                || type == typeof(int?)
                || type == typeof(long?)
                || type == typeof(double?)
                || type == typeof(float?)
                || type == typeof(bool?)
                || Nullable.GetUnderlyingType(type) != null;
        }

        // Helper method to check if type is collection
        private bool IsCollection(Type type)
        {
            return typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string);
        }

        #endregion
    }
}