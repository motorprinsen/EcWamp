using Newtonsoft.Json;
using System.Collections.Generic;

namespace JsonData
{
    public enum JsonDataSetResults
    {
        Success, FileNotFound, BadFormat,
        Error, Empty
    }

    public class JsonDataSet
    {
        [JsonProperty("result")]
        public JsonDataSetResults Result { get; set; }

        [JsonProperty("format")]
        public string Format { get; set; }

        [JsonProperty("nodes")]
        public List<JsonDataNode> Nodes { get; set; }
    }
}