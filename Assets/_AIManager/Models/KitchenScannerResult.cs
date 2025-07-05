using System.Collections.Generic;
using Newtonsoft.Json;

namespace AI.Models
{
    public class KitchenScannerResult
    {
        [JsonProperty("data")]
        public List<BoundingBox> Data { get; set; }
    }
}
