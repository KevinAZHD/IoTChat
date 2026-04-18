using System.Text;
using System.Text.Json;
using IoTChat.Models;

namespace IoTChat.Services
{
    public class LlmService
    {
        private readonly HttpClient _http;

        public LlmService(HttpClient http) => _http = http;

        public async Task<string> GetModelNameAsync()
        {
            if (AppConfig.ModelName != "auto") return AppConfig.ModelName;
            try
            {
                var resp = await _http.GetAsync($"{AppConfig.LlmBaseUrl}/v1/models");
                var body = await resp.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(body);
                return doc.RootElement.GetProperty("data")[0].GetProperty("id").GetString() ?? "local-model";
            }
            catch { return "local-model"; }
        }

        public async Task<(List<string> toolCalls, string? textResponse)> SendAsync(string userMessage)
        {
            var model = await GetModelNameAsync();
            var systemPrompt =
                "Eres ELIoT, asistente IoT experto en semaforos. Tono: PROFESIONAL, AMIGABLE y CONCISO. NO seas poeta ni infantil.\n" +
                "REGLAS:\n" +
                "1. TOOLS MULTIPLES: Si piden varios colores (ej: rojo y verde), busca la tool combinada exacta (ej: 'parpadear_rojo_verde' o 'encender_rojo_verde'). Lee TODAS las tools.\n" +
                "2. OBLIGATORIO usar la tool correspondiente ANTES de responder. No hables si no has usado la tool.\n" +
                "3. RESPUESTA: Cuando la tool termine, responde de forma profesional confirmando EXACTAMENTE la accion realizada (ej: 'Las luces roja y verde estan parpadeando. ¿Te ayudo con otra cosa?').\n" +
                "4. PROHIBIDO: lenguaje infantil ('wow', 'bailan', 'espectacular'), emojis, y escribir el nombre tecnico de la tool en el texto.";

            var req1 = new { model, messages = new object[] { new { role = "system", content = systemPrompt }, new { role = "user", content = userMessage } }, tools = BuildToolDefinitions(), tool_choice = "auto" };
            var res1 = await _http.PostAsync($"{AppConfig.LlmBaseUrl}/v1/chat/completions", new StringContent(JsonSerializer.Serialize(req1), Encoding.UTF8, "application/json"));
            var msg1 = JsonDocument.Parse(await res1.Content.ReadAsStringAsync()).RootElement.GetProperty("choices")[0].GetProperty("message");

            var toolNames   = new List<string>();
            var toolCallIds = new List<(string id, string name)>();

            if (msg1.TryGetProperty("tool_calls", out var toolCallsEl) && toolCallsEl.GetArrayLength() > 0)
            {
                for (int i = 0; i < toolCallsEl.GetArrayLength(); i++)
                {
                    var tc  = toolCallsEl[i];
                    var raw = tc.GetProperty("function").GetProperty("name").GetString() ?? "";
                    var id  = tc.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? $"call_{i}" : $"call_{i}";
                    toolNames.Add(ToolNormalizer.Normalize(raw));
                    toolCallIds.Add((id, raw));
                }
            }
            else
            {
                var directText = msg1.TryGetProperty("content", out var ct) ? ct.GetString() ?? "" : "";
                var embedded = ExtractToolNameFromContent(directText);
                if (embedded != null)
                {
                    toolNames.Add(ToolNormalizer.Normalize(embedded));
                    toolCallIds.Add(("call_inline", embedded));
                }
                else
                {
                    return (new List<string>(), string.IsNullOrWhiteSpace(directText) ? null : directText);
                }
            }

            var history = new List<object>
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = userMessage }
            };

            if (toolCallIds.Any(t => t.id == "call_inline"))
            {
                history.Add(new
                {
                    role = "assistant",
                    content = "",
                    tool_calls = new[] { new { type = "function", id = "call_inline", function = new { name = toolCallIds[0].name, arguments = "{}" } } }
                });
            }
            else
            {
                history.Add(JsonSerializer.Deserialize<object>(msg1.GetRawText())!);
            }

            foreach (var (callId, rawName) in toolCallIds)
                history.Add(new { role = "tool", tool_call_id = callId, content = $"La accion '{rawName}' se ejecuto correctamente." });

            var req2 = new { model, messages = history.ToArray() };
            var res2 = await _http.PostAsync($"{AppConfig.LlmBaseUrl}/v1/chat/completions", new StringContent(JsonSerializer.Serialize(req2), Encoding.UTF8, "application/json"));
            var msg2 = JsonDocument.Parse(await res2.Content.ReadAsStringAsync()).RootElement.GetProperty("choices")[0].GetProperty("message");
            var naturalText = msg2.TryGetProperty("content", out var c2) ? c2.GetString() ?? "" : "";
            if (IsRawToolCall(naturalText)) naturalText = "";

            return (toolNames, string.IsNullOrWhiteSpace(naturalText) ? null : naturalText);
        }

        private static bool IsRawToolCall(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            var t = text.Trim();
            return t.Contains("<tool_call>") || t.Contains("</tool_call>") ||
                   (t.Contains("{") && t.Contains("\"name\"") && t.Contains("\"arguments\""));
        }

        private static string? ExtractToolNameFromContent(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var t = text.Trim();
            var startXml = t.IndexOf("<tool_call>", StringComparison.Ordinal);
            var endXml   = t.IndexOf("</tool_call>", StringComparison.Ordinal);
            if (startXml >= 0 && endXml > startXml)
                t = t.Substring(startXml + 11, endXml - startXml - 11).Trim();

            var startJson = t.IndexOf('{');
            var endJson = t.LastIndexOf('}');
            if (startJson >= 0 && endJson > startJson)
            {
                var jsonPart = t.Substring(startJson, endJson - startJson + 1);
                if (jsonPart.Contains("\"name\""))
                {
                    try
                    {
                        var doc = JsonDocument.Parse(jsonPart);
                        if (doc.RootElement.TryGetProperty("name", out var n))
                            return n.GetString();
                    }
                    catch { }
                }
            }
            return null;
        }

        private static object[] BuildToolDefinitions() => new object[]
        {
            Tool("encender_luz_roja",          "Enciende SOLO la luz ROJA (apaga las demas). Disparar con: rojo, red, stop."),
            Tool("encender_luz_amarilla",       "Enciende SOLO la luz AMARILLA (apaga las demas). Disparar con: amarillo, yellow."),
            Tool("encender_luz_verde",          "Enciende SOLO la luz VERDE (apaga las demas). Disparar con: verde, green."),
            Tool("apagar_luz_roja",             "Apaga SOLO la luz roja."),
            Tool("apagar_luz_amarilla",         "Apaga SOLO la luz amarilla."),
            Tool("apagar_luz_verde",            "Apaga SOLO la luz verde."),
            Tool("apagar_todas",                "Apaga TODAS las luces. Disparar con: apagar, off, apaga todo."),
            Tool("parpadear_luz_roja",          "Hace parpadear la luz roja infinitamente."),
            Tool("parpadear_luz_amarilla",      "Hace parpadear la luz amarilla infinitamente."),
            Tool("parpadear_luz_verde",         "Hace parpadear la luz verde infinitamente."),
            Tool("parpadear_todas",             "Hace parpadear TODAS las luces infinitamente."),
            Tool("encender_rojo_amarillo",      "Enciende roja y amarilla juntas."),
            Tool("encender_rojo_verde",         "Enciende roja y verde juntas."),
            Tool("encender_amarillo_verde",     "Enciende amarilla y verde juntas."),
            Tool("encender_todas",              "Enciende TODAS las luces a la vez."),
            Tool("apagar_rojo_amarillo",        "Apaga rojas y amarillas juntas."),
            Tool("apagar_rojo_verde",           "Apaga rojas y verdes juntas."),
            Tool("apagar_amarillo_verde",       "Apaga amarillas y verdes juntas."),
            Tool("parpadear_rojo_amarillo",     "Parpadea rojas y amarillas infinitamente."),
            Tool("parpadear_rojo_verde",        "Parpadea rojas y verdes infinitamente."),
            Tool("parpadear_amarillo_verde",    "Parpadea amarillas y verdes infinitamente."),
        };

        private static object Tool(string name, string desc) => new
        {
            type = "function",
            function = new { name, description = desc, parameters = new { type = "object", properties = new { } } }
        };
    }
}
