using System;
using System.IO;

namespace ConHIS_Service_XPHL7.Configuration
{
    public class AppConfig
    {
        private const string ConnFolder = "Connection";
        private const string ConnFile = "connectdatabase.txt";
        private const string ConfigFolder = "Config";
        private const string ConfigFile = "appsettings.txt";

        public string ConnectionString { get; private set; }
        public string ApiEndpoint { get; private set; }
        public int ProcessingIntervalSeconds { get; private set; } = 30;
        public bool AutoStart { get; private set; } = true;

        public bool LoadConfiguration()
        {
            try
            {
                LoadConnectionString();
                LoadAppSettings();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load configuration: {ex.Message}", ex);
            }
        }

        private void LoadConnectionString()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConnFolder, ConnFile);

            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Connection file not found: {path}");
            }

            ConnectionString = File.ReadAllText(path).Trim();

            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                throw new Exception("Connection string is empty");
            }
        }

        private void LoadAppSettings()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFolder, ConfigFile);

            // Create default config if not exists
            if (!File.Exists(path))
            {
                CreateDefaultConfig(path);
            }

            var lines = File.ReadAllLines(path);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                var parts = line.Split('=');
                if (parts.Length != 2) continue;

                var key = parts[0].Trim();
                var value = parts[1].Trim();

                switch (key.ToUpper())
                {
                    case "APIENDPOINT":
                        ApiEndpoint = value;
                        break;
                    case "PROCESSINGINTERVALSECONDS":
                        if (int.TryParse(value, out int interval))
                            ProcessingIntervalSeconds = interval;
                        break;
                    case "AUTOSTART":
                        if (bool.TryParse(value, out bool autoStart))
                            AutoStart = autoStart;
                        break;
                }
            }
        }

        private void CreateDefaultConfig(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var defaultConfig = @"# HL7 ipd Drug Dispenser Configuration
# API endpoint for sending processed data
ApiEndpoint=http://localhost:8080/api/drugdispense

# Processing interval in seconds
ProcessingIntervalSeconds=30

# Auto start processing on application load
AutoStart=true
";

            File.WriteAllText(path, defaultConfig);
        }
    }
}