using System.Collections.Generic;
using Newtonsoft.Json;

namespace Groq.Models
{
    public class ChatCompletionResponse
    {
        [JsonProperty("choices")]
        public List<Choice> Choices { get; set; }

        public class Choice
        {
            [JsonProperty("message")]
            public Message Message { get; set; }
            
            [JsonProperty("finish_reason")]
            public string FinishReason { get; set; }
        }

        public class Message
        {
            [JsonProperty("role")]
            public string Role { get; set; }
            
            [JsonProperty("content")]
            public string Content { get; set; }
        }
    }
}