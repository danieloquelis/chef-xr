using Newtonsoft.Json;

namespace Groq.Models
{
    public class Message
    {
        [JsonProperty("role")]
        public string Role { get; set; } // can be "user", "system" or "assistant"

        [JsonProperty("content")]
        public object Content { get; set; } // can be string or List<ContentBlock>
    }
}