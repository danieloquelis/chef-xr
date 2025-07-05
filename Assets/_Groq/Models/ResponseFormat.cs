using Newtonsoft.Json;

namespace Groq.Models
{
    public class ResponseFormat
    {
        [JsonProperty("type")]
        public string Type { get; set; }
    }
}