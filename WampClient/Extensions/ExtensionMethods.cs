using JsonData;
using System;
using System.Collections.Generic;


namespace WampClient
{
    public static class JsonDataNodeExtensions
    {
        public static IEnumerable<JsonDataNode> FilterAccess<JsonDataNode>(this IEnumerable<JsonDataNode> enumerable, String level, Func<JsonDataNode, String, bool> compareMethod)
        {
            foreach (JsonDataNode item in enumerable)
            {
                if (compareMethod(item, level))
                {
                    yield return item;
                }
            }
        }
        public static List<DynamicValue> GetDynamicValueList(this JsonDataNode dataNode)
        {
            return GetDynamicValue(dataNode, new List<DynamicValue>(), null);
            List<DynamicValue> GetDynamicValue(JsonDataNode node, List<DynamicValue> dynamicValueList, String childIndexes = null)
            {
                foreach (var attribute in node.Attributes)
                {
                    if (attribute.Value.ToString().StartsWith("*"))
                    {
                        DynamicValue dv = new DynamicValue();
                        dv.Name = attribute.Key.ToString();
                        dv.Value = attribute.Value.ToString();
                        dv.ChildIndex = childIndexes;
                        dynamicValueList.Add(dv);
                    }


                }
                int index = 0;
                foreach (var child in node.Children)
                {
                    GetDynamicValue(child, dynamicValueList, childIndexes == null ? index.ToString() : childIndexes + "," + index.ToString());
                    index++;
                }
                return dynamicValueList;
            }


        }
    }
}
