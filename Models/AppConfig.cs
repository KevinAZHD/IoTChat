namespace IoTChat.Models
{
    public static class AppConfig
    {
        // Para probar en un MOVIL REAL, cambia "10.0.2.2" por la IP de tu PC (ej: "192.168.1.50")
        public static string BaseIp = Microsoft.Maui.Devices.DeviceInfo.Platform == Microsoft.Maui.Devices.DevicePlatform.Android 
            ? "10.0.2.2"   // Android
            : "127.0.0.1"; // Desktop

        public static string LlmBaseUrl { get; set; } = $"http://{BaseIp}:1234";
        public static string NodeRedUrl { get; set; } = $"http://{BaseIp}:1880/encender";
        public static string ModelName { get; set; } = "auto";
        public static string MqttTopic      { get; set; } = "iabd2425/esp32/led";
        public static string TelemetryTopic { get; set; } = "iabd2425/esp32/telemetry";
    }
}
