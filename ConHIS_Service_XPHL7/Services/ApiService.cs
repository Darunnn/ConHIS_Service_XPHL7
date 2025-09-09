using System;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using ConHIS_Service_XPHL7.Utils;

namespace ConHIS_Service_XPHL7.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiEndpoint;
        private readonly LogManager _logger = new LogManager();

        public ApiService(string apiEndpoint)
        {
            _httpClient = new HttpClient();
            _apiEndpoint = apiEndpoint;
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public bool SendToMiddleware(object data)
        {
            _logger.LogInfo("SendToMiddleware: Start");
            try
            {
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                _logger.LogInfo($"SendToMiddleware: JSON = {json}");
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = _httpClient.PostAsync(_apiEndpoint, content).Result;
                _logger.LogInfo($"SendToMiddleware: Response status = {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInfo("SendToMiddleware: Success");
                    return true;
                }
                else
                {
                    var errorContent = response.Content.ReadAsStringAsync().Result;
                    _logger.LogError($"API call failed: {response.StatusCode}, Content: {errorContent}");
                    throw new Exception($"API call failed: {response.StatusCode}, Content: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error calling API", ex);
                throw new Exception($"Error calling API: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}