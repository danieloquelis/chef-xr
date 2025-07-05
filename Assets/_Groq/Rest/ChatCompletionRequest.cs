using System.Collections.Generic;
using Groq.Models;
using Newtonsoft.Json;

namespace Groq.Rest
{
    public class ChatCompletionRequest
    {
        [JsonProperty("messages")]
        public List<Message> Messages { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("temperature")]
        public float Temperature { get; set; } = 1;

        [JsonProperty("max_completion_tokens")]
        public int MaxCompletionTokens { get; set; } = 8192;

        [JsonProperty("top_p")]
        public float TopP { get; set; } = 1;

        [JsonProperty("stream")]
        public bool Stream { get; set; } = true;
        
        [JsonProperty("response_format",  NullValueHandling = NullValueHandling.Ignore)]
        public ResponseFormat ResponseFormat { get; set; }

        [JsonProperty("stop", NullValueHandling = NullValueHandling.Ignore)]
        public object Stop { get; set; } = null; // null, or string[], etc.   
    }
}