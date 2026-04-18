using System.ComponentModel;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace IoTChat.MCP;

[McpServerToolType]
public sealed class SemaforoTools
{
    private readonly IHttpClientFactory _httpFactory;

    public SemaforoTools(IHttpClientFactory httpFactory) => _httpFactory = httpFactory;

    // Envia un comando a Node-RED via HTTP POST y devuelve resultado
    private async Task<string> Send(string command)
    {
        try
        {
            var client  = _httpFactory.CreateClient("NodeRED");
            var payload = JsonSerializer.Serialize(new { command });
            var resp    = await client.PostAsync("/encender",
                new StringContent(payload, Encoding.UTF8, "application/json"));
            return resp.IsSuccessStatusCode ? $"'{command}' -> MQTT" : $"Error: {resp.StatusCode}";
        }
        catch (Exception ex) { return ex.Message; }
    }

    // Encender individuales
    [McpServerTool, Description("Enciende SOLO la luz ROJA (apaga las demas)")]
    public async Task<string> encender_luz_roja()     => await Send("encender_rojo");

    [McpServerTool, Description("Enciende SOLO la luz AMARILLA (apaga las demas)")]
    public async Task<string> encender_luz_amarilla() => await Send("encender_amarillo");

    [McpServerTool, Description("Enciende SOLO la luz VERDE (apaga las demas)")]
    public async Task<string> encender_luz_verde()    => await Send("encender_verde");

    // Apagar individuales
    [McpServerTool, Description("Apaga SOLO la luz roja")]
    public async Task<string> apagar_luz_roja()       => await Send("apagar_rojo");

    [McpServerTool, Description("Apaga SOLO la luz amarilla")]
    public async Task<string> apagar_luz_amarilla()   => await Send("apagar_amarillo");

    [McpServerTool, Description("Apaga SOLO la luz verde")]
    public async Task<string> apagar_luz_verde()      => await Send("apagar_verde");

    [McpServerTool, Description("Apaga TODAS las luces")]
    public async Task<string> apagar_todas()          => await Send("apagar_todas");

    // Parpadear individuales
    [McpServerTool, Description("Hace parpadear la luz ROJA infinitamente")]
    public async Task<string> parpadear_luz_roja()    => await Send("parpadear_rojo");

    [McpServerTool, Description("Hace parpadear la luz AMARILLA infinitamente")]
    public async Task<string> parpadear_luz_amarilla() => await Send("parpadear_amarillo");

    [McpServerTool, Description("Hace parpadear la luz VERDE infinitamente")]
    public async Task<string> parpadear_luz_verde()   => await Send("parpadear_verde");

    [McpServerTool, Description("Hace parpadear TODAS las luces infinitamente")]
    public async Task<string> parpadear_todas()       => await Send("parpadear_todas");

    // Encender dobles
    [McpServerTool, Description("Enciende luces ROJA y AMARILLA juntas")]
    public async Task<string> encender_rojo_amarillo()    => await Send("encender_rojo_amarillo");

    [McpServerTool, Description("Enciende luces ROJA y VERDE juntas")]
    public async Task<string> encender_rojo_verde()       => await Send("encender_rojo_verde");

    [McpServerTool, Description("Enciende luces AMARILLA y VERDE juntas")]
    public async Task<string> encender_amarillo_verde()   => await Send("encender_amarillo_verde");

    [McpServerTool, Description("Enciende TODAS las luces a la vez")]
    public async Task<string> encender_todas()            => await Send("encender_todas");

    // Apagar dobles
    [McpServerTool, Description("Apaga luces ROJA y AMARILLA juntas")]
    public async Task<string> apagar_rojo_amarillo()      => await Send("apagar_rojo_amarillo");

    [McpServerTool, Description("Apaga luces ROJA y VERDE juntas")]
    public async Task<string> apagar_rojo_verde()         => await Send("apagar_rojo_verde");

    [McpServerTool, Description("Apaga luces AMARILLA y VERDE juntas")]
    public async Task<string> apagar_amarillo_verde()     => await Send("apagar_amarillo_verde");

    // Parpadear dobles
    [McpServerTool, Description("Hace parpadear luces ROJA y AMARILLA")]
    public async Task<string> parpadear_rojo_amarillo()   => await Send("parpadear_rojo_amarillo");

    [McpServerTool, Description("Hace parpadear luces ROJA y VERDE")]
    public async Task<string> parpadear_rojo_verde()      => await Send("parpadear_rojo_verde");

    [McpServerTool, Description("Hace parpadear luces AMARILLA y VERDE")]
    public async Task<string> parpadear_amarillo_verde()  => await Send("parpadear_amarillo_verde");
}
