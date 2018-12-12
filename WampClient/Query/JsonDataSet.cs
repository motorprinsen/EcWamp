using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.XPath;

namespace WampClient.Query
{
    public enum JsonDataSetResults {Success,FileNotFound,BadFormat,
        Error,Empty
    }
    [DataContract(IsReference = true)]
    [KnownType(typeof(JsonDataNode))]
    [KnownType(typeof(JsonDataProperty))]
    [KnownType(typeof(JsonDataPropertyParameter))]
    public class JsonDataSet
    {

        [DataMember]
        public JsonDataSetResults Result { get; internal set; }

        [DataMember]
        public string Format { get; internal set; }

        [DataMember]
        public List<JsonDataNode> Nodes;

    }
}
