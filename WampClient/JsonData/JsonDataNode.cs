using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;


namespace WampClient.Query
{
    [DataContract(IsReference = true)]
    [KnownType(typeof(JsonDataProperty))]
    public class JsonDataNode
    {
      
        
        [DataMember]
        public string Type { get; private set; }

        [DataMember]
        private List<JsonDataNode> children;
        [DataMember]
        private List<JsonDataProperty> properties;


    }


}