using Newtonsoft.Json;

namespace Groq.Models
{
    public class ContentBlock
    {
        [JsonProperty("type")]
        public string Type { get; set; } // "text" or "image_url"

        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }

        [JsonProperty("image_url", NullValueHandling = NullValueHandling.Ignore)]
        public ImageUrl ImageUrl { get; set; }
    }
}