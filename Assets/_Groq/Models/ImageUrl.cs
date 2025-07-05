using Newtonsoft.Json;

namespace Groq.Models
{
    public class ImageUrl
    {
        [JsonProperty("url")]
        public string Url { get; set; }
    }
}