using Newtonsoft.Json;
using System.Collections.Generic;

namespace JsonData
{
    public class JsonDataProperty
    {
        [JsonProperty("parameters")]
        public List<JsonDataPropertyParameter> Parameters { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("isDefault")]
        public bool IsDefault { get; set; }
    }

    public class JsonDataPropertyParameter
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}