using Newtonsoft.Json;
using System.Collections.Generic;

namespace JsonData
{
    public class JsonDataNode
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("children")]
        public List<JsonDataNode> Children { get; set; }

        //use this for design
        //[JsonProperty("properties")]
        //public List<JsonDataProperty> Properties { get; set; }

        [JsonProperty("attributes")]
        public IDictionary<string, object> Attributes { get; set; }
    }
}