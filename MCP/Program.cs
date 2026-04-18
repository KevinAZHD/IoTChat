using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

// Configura y arranca el servidor MCP via stdio
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new()
    {
        Name = "Semaforo MCP Server",
        Version = "1.0.0"
    };
})
.WithStdioServerTransport()
.WithTools<IoTChat.MCP.SemaforoTools>();

// Cliente HTTP para comunicarse con Node-RED (NODE_RED_URL sobreescribe el default local)
var nodeRedUrl = Environment.GetEnvironmentVariable("NODE_RED_URL") ?? "http://127.0.0.1:1880";
builder.Services.AddHttpClient("NodeRED", client =>
{
    client.BaseAddress = new Uri(nodeRedUrl);
});

await builder.Build().RunAsync();
