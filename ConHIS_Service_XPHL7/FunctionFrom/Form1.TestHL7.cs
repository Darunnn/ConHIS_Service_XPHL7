using ConHIS_Service_XPHL7.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using static ConHIS_Service_XPHL7.Services.SimpleHL7FileProcessor;

namespace ConHIS_Service_XPHL7
{
    public partial class Form1
    {
   
            private async void TestHL7Button_Click(object sender, EventArgs e)
        {
            try
            {
                using (var openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Title = "Select HL7 File to Test";
                    openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";

                    var searchFolders = new[]
                    {
                        Path.Combine(Application.StartupPath, "TestData"),
                        Application.StartupPath,
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                    };

                    string initialDirectory = Application.StartupPath;
                    foreach (var folder in searchFolders)
                    {
                        if (Directory.Exists(folder))
                        {
                            initialDirectory = folder;
                            break;
                        }
                    }
                    openFileDialog.InitialDirectory = initialDirectory;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        var filePath = openFileDialog.FileName;
                        var fileName = Path.GetFileName(filePath);

                        if (string.IsNullOrEmpty(AppConfig.ApiEndpoint))
                        {
                            _logger.LogError("API Endpoint is not configured!");
                            UpdateStatus("Error: API Endpoint not configured");
                            return;
                        }

                        var sendToApi = true;

                        UpdateStatus($"Testing HL7 file: {fileName}...");
                        testHL7Button.Enabled = false;
                        manualCheckButton.Enabled = false;
                        startStopButton.Enabled = false;
                        clearButton.Enabled = false;
                        exportButton.Enabled = false;

                        HL7TestResult result = null;
                        await Task.Run(() =>
                        {
                            result = _hl7FileProcessor.ProcessAndSendHL7File(filePath, sendToApi);
                        });

                        if (result != null)
                        {
                            // ดึงข้อมูลจาก HL7Message
                            string orderNo = result.ParsedMessage?.CommonOrder?.PlacerOrderNumber ?? "N/A";
                            string hn = result.ParsedMessage?.PatientIdentification?.PatientIDExternal ??
                                       result.ParsedMessage?.PatientIdentification?.PatientIDInternal ?? "N/A";

                            // สร้างชื่อผู้ป่วย
                            string patientName = "N/A";
                            if (result.ParsedMessage?.PatientIdentification?.OfficialName != null)
                            {
                                var name = result.ParsedMessage.PatientIdentification.OfficialName;
                                patientName = $"{name.Prefix ?? ""} {name.FirstName ?? ""} {name.LastName ?? ""}".Trim();
                                if (string.IsNullOrWhiteSpace(patientName)) patientName = "N/A";
                            }

                            // ดึงข้อมูลยาจาก RXD แรก (ถ้ามี)
                            string drugCode = "N/A";
                            string drugName = "N/A";
                            string quantity = "N/A";

                            if (result.ParsedMessage?.PharmacyDispense != null && result.ParsedMessage.PharmacyDispense.Count > 0)
                            {
                                var rxd = result.ParsedMessage.PharmacyDispense[0];
                                drugCode = rxd.Dispensegivecode?.Dispense ?? "N/A";
                                drugName = rxd.Dispensegivecode?.DrugName ??
                                           rxd.Dispensegivecode?.DrugNamePrint ??
                                           rxd.Dispensegivecode?.DrugNameThai ?? "N/A";
                                quantity = rxd.QTY > 0 ? rxd.QTY.ToString() : "N/A";
                            }

                            // เพิ่มข้อมูลลงตาราง
                            AddRowToGrid(
                                DateTime.Now.ToString("HH:mm:ss"),
                                orderNo,
                                hn,
                                patientName,
                                drugCode,
                                drugName,
                                quantity,
                                result.Success ? "Success" : "Failed",
                                result.ApiResponse ?? result.ErrorMessage ?? "N/A",
                                result.ParsedMessage  // ส่ง HL7Message ไปด้วย
                            );

                            if (result.Success)
                            {
                                UpdateStatus($"HL7 test completed - {fileName}");
                            }
                            else
                            {
                                UpdateStatus($"HL7 test failed - {fileName}");
                            }
                        }
                        else
                        {
                            UpdateStatus("HL7 test failed - Check log for details");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("HL7 file test error", ex);
                UpdateStatus($"HL7 test error: {ex.Message}");
            }
            finally
            {
                testHL7Button.Enabled = true;
                manualCheckButton.Enabled = true;
                startStopButton.Enabled = true;
                clearButton.Enabled = true;
                exportButton.Enabled = true;
            }
        }
    }
}