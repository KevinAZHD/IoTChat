using System.Text;
using System.Text.Json;
using IoTChat.Models;

namespace IoTChat.Services
{
    // Envia comandos a Node-RED via HTTP POST
    public class NodeRedService
    {
        private readonly HttpClient _http;

        public NodeRedService(HttpClient http) => _http = http;

        // Publica un comando MQTT a traves de Node-RED
        public async Task<bool> SendCommandAsync(string command)
        {
            try
            {
                var payload = JsonSerializer.Serialize(new { command });
                var resp = await _http.PostAsync(AppConfig.NodeRedUrl,
                    new StringContent(payload, Encoding.UTF8, "application/json"));
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }
    }
}
