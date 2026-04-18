using System.Text.Json;

namespace IoTChat.Services
{
    // Convierte variantes de nombres de tools a su nombre canonico
    public static class ToolNormalizer
    {
        // Tabla de sinónimos → nombre oficial de tool
        private static readonly Dictionary<string, string> _map = new()
        {
            // Encender individuales
            ["enciende_rojo"] = "encender_luz_roja",    ["encender_rojo"] = "encender_luz_roja",
            ["enciende_roja"] = "encender_luz_roja",    ["encender_roja"] = "encender_luz_roja",
            ["enciende_luz_roja"] = "encender_luz_roja", ["turn_on_red"] = "encender_luz_roja",
            ["red_on"] = "encender_luz_roja",

            ["enciende_amarillo"] = "encender_luz_amarilla", ["encender_amarillo"] = "encender_luz_amarilla",
            ["enciende_amarilla"] = "encender_luz_amarilla", ["encender_amarilla"] = "encender_luz_amarilla",
            ["enciende_luz_amarilla"] = "encender_luz_amarilla", ["turn_on_yellow"] = "encender_luz_amarilla",

            ["enciende_verde"] = "encender_luz_verde",   ["encender_verde"] = "encender_luz_verde",
            ["enciende_luz_verde"] = "encender_luz_verde", ["turn_on_green"] = "encender_luz_verde",

            // Apagar individuales
            ["apaga_rojo"] = "apagar_luz_roja",    ["apagar_rojo"] = "apagar_luz_roja",
            ["apaga_roja"] = "apagar_luz_roja",    ["apaga_luz_roja"] = "apagar_luz_roja",
            ["apaga_amarillo"] = "apagar_luz_amarilla", ["apagar_amarillo"] = "apagar_luz_amarilla",
            ["apaga_amarilla"] = "apagar_luz_amarilla", ["apaga_luz_amarilla"] = "apagar_luz_amarilla",
            ["apaga_verde"] = "apagar_luz_verde",   ["apagar_verde"] = "apagar_luz_verde",
            ["apaga_luz_verde"] = "apagar_luz_verde",

            // Apagar todas
            ["apaga_todas"] = "apagar_todas",  ["apagar_todo"] = "apagar_todas",
            ["apaga_todo"] = "apagar_todas",   ["apagar_luces"] = "apagar_todas",
            ["apaga_luces"] = "apagar_todas",  ["turn_off_all"] = "apagar_todas",

            // Parpadear individuales
            ["parpadea_rojo"] = "parpadear_luz_roja",       ["parpadear_rojo"] = "parpadear_luz_roja",
            ["parpadea_roja"] = "parpadear_luz_roja",       ["blink_red"] = "parpadear_luz_roja",
            ["parpadea_amarillo"] = "parpadear_luz_amarilla", ["parpadear_amarillo"] = "parpadear_luz_amarilla",
            ["parpadea_amarilla"] = "parpadear_luz_amarilla", ["blink_yellow"] = "parpadear_luz_amarilla",
            ["parpadea_verde"] = "parpadear_luz_verde",     ["parpadear_verde"] = "parpadear_luz_verde",
            ["blink_green"] = "parpadear_luz_verde",

            // Parpadear todas
            ["parpadea_todas"] = "parpadear_todas", ["parpadear_todo"] = "parpadear_todas",
            ["parpadea_todo"] = "parpadear_todas",  ["parpadear_luces"] = "parpadear_todas",
            ["blink_all"] = "parpadear_todas",

            // Encender dobles
            ["enciende_rojo_amarillo"] = "encender_rojo_amarillo",
            ["enciende_rojo_verde"] = "encender_rojo_verde",
            ["enciende_amarillo_verde"] = "encender_amarillo_verde",
            ["enciende_todas"] = "encender_todas",  ["encender_todo"] = "encender_todas",
            ["enciende_todo"] = "encender_todas",   ["encender_luces"] = "encender_todas",
            ["enciende_luces"] = "encender_todas",  ["turn_on_all"] = "encender_todas",

            // Apagar dobles
            ["apaga_rojo_amarillo"] = "apagar_rojo_amarillo",   ["apagar_rojo_amarillo"] = "apagar_rojo_amarillo",
            ["apaga_rojo_verde"] = "apagar_rojo_verde",         ["apagar_rojo_verde"] = "apagar_rojo_verde",
            ["apaga_amarillo_verde"] = "apagar_amarillo_verde", ["apagar_amarillo_verde"] = "apagar_amarillo_verde",

            // Parpadear dobles
            ["parpadea_rojo_amarillo"] = "parpadear_rojo_amarillo",   ["parpadear_rojo_amarillo"] = "parpadear_rojo_amarillo",
            ["parpadea_rojo_verde"] = "parpadear_rojo_verde",         ["parpadear_rojo_verde"] = "parpadear_rojo_verde",
            ["parpadea_amarillo_verde"] = "parpadear_amarillo_verde", ["parpadear_amarillo_verde"] = "parpadear_amarillo_verde"
        };

        // Normaliza el nombre de tool recibido del LLM
        public static string Normalize(string raw)
        {
            var name = raw.Trim();

            // Si viene como JSON objeto, extrae el campo "name"
            if (name.Contains('{'))
            {
                try
                {
                    var parsed = JsonDocument.Parse(name);
                    if (parsed.RootElement.TryGetProperty("name", out var n))
                        name = n.GetString() ?? name;
                }
                catch { }
            }

            name = name.ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
            return _map.TryGetValue(name, out var normalized) ? normalized : name;
        }
    }
}
