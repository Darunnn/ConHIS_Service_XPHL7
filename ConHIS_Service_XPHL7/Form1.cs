using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using ConHIS_Service_XPHL7.Configuration;
using ConHIS_Service_XPHL7.Services;
using ConHIS_Service_XPHL7.Utils;

namespace ConHIS_Service_XPHL7
{
    public partial class Form1 : Form
    {
        private AppConfig _appConfig;
        private DatabaseService _databaseService;
        private LogManager _logger;
        private DrugDispenseProcessor _processor;

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
            _logger = new LogManager();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            _logger.LogInfo("Start Interface");
            try
            {
                _logger.LogInfo("Loading configuration");
                _appConfig = new AppConfig();
                _appConfig.LoadConfiguration();
                _logger.LogInfo("Configuration loaded");

                _logger.LogInfo("Connecting to database");
                _databaseService = new DatabaseService(_appConfig.ConnectionString);
                _logger.LogInfo("DatabaseService initialized");

                var pending = await Task.Run(() => _databaseService.GetPendingDispenseData());
                _logger.LogInfo($"Get Data from DB, Count: {pending.Count}");

                // Show count on form title (example)
                this.Text = $"Pending dispense items: {pending.Count}";

                // --- เรียก DrugDispenseProcessor เพื่อเข้าสู่ขั้นตอน Read Format HL7 ---
                var apiService = new ApiService(_appConfig.ApiEndpoint);
                var hl7Service = new HL7Service();
                _processor = new DrugDispenseProcessor(_databaseService, hl7Service, apiService);
                _processor.ProcessPendingOrders(msg => _logger.LogInfo(msg));
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to initialize", ex);
                MessageBox.Show($"Failed to initialize: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}