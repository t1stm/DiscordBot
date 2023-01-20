using System;
using System.Text.Json.Serialization;

namespace DiscordBot.Standalone
{
    public class ChatMessage
    {
        [JsonInclude, JsonPropertyName("user")]
        public string User { get; set; }
        [JsonInclude, JsonPropertyName("message")]
        public string Message { get; set; }
        [JsonInclude, JsonPropertyName("send_time")]
        public DateTime SendTime { get; set; }
    }
}
