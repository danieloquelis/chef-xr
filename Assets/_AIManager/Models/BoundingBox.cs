using System.Collections.Generic;
using Newtonsoft.Json;

namespace AI.Models
{
    public class BoundingBox
    {
        [JsonProperty("label")]
        public string Label { get; set; }
        
        [JsonProperty("className")]
        public string ClassName { get; set; }
        
        [JsonProperty("bbox")]
        public List<float> BBox { get; set; }
        
        [JsonProperty("confidence", NullValueHandling = NullValueHandling.Ignore)]
        public float Confidence { get; set; }
    }
}