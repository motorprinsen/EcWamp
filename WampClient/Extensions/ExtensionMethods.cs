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
        public static List<BindableElement> GetBindableElementsList(this JsonDataNode dataNode)
        {
            return GetBindableElement(dataNode, new List<BindableElement>(), null);
            List<BindableElement> GetBindableElement(JsonDataNode node, List<BindableElement> bindableElementsList, String childIndexes = null)
            {
                foreach (var attribute in node.Attributes)
                {
                    if (attribute.Value.ToString().StartsWith("*"))
                    {
                        BindableElement dv = new BindableElement();
                        dv.AttributeName = attribute.Key.ToString();
                        dv.BindingAddress = attribute.Value.ToString();
                        dv.ElementIndex = childIndexes;
                        bindableElementsList.Add(dv);
                    }


                }
                int index = 0;
                foreach (var child in node.Children)
                {
                    GetBindableElement(child, bindableElementsList, childIndexes == null ? index.ToString() : childIndexes + "," + index.ToString());
                    index++;
                }
                return bindableElementsList;
            }


        }
    }
}
