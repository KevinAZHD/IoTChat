namespace IoTChat.Models
{
    public class ChatMessage
    {
        public string Text { get; set; } = "";
        public bool IsUser { get; set; }
        public bool IsSystem { get; set; }
        public string Icon => IsSystem ? "SYS" : IsUser ? "USR" : "ELIoT";
        public string BubbleColor => IsSystem ? "#1A2332" : IsUser ? "#1A3D1F" : "#21262D";
        public string TextColor => IsSystem ? "#58A6FF" : "#E6EDF3";
    }
}
