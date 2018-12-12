﻿using Newtonsoft.Json;
using System.Collections.Generic;

namespace WampClient.Query
{
    public class JsonDataNode
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("children")]
        public List<JsonDataNode> Children { get; set; }

        [JsonProperty("properties")]
        public List<JsonDataProperty> Properties { get; set; }
    }
}