using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace WampClient.Query
{
    [DataContract]
    public class JsonDataProperty
    {
        [DataMember]
        public  List<JsonDataPropertyParameter> Parameters;
        [DataMember]
        public string Type { get; internal set; }
        [DataMember]
        public string Name { get; internal set; }
        [DataMember]
        public string Value { get; internal set; }
        [DataMember]
        public bool IsDefault { get; internal set; }
    }
    [DataContract]
    public class JsonDataPropertyParameter      
    {
        [DataMember]
        public string Name { get; internal set; }
        [DataMember]
        public string Value { get; internal set; }

    }
}