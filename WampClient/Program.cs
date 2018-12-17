using JsonData;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using WampSharp.V2;
using WampSharp.V2.Rpc;
using System.Linq;
namespace WampClient
{
   

    public class ViewData
    {
        public string SubscriptionId { get; set; }
        public string Blob { get; set; }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {

            var viewArgs = new[] { @"Area:\EXOFlex2.Esav", "EXOFlex2", "EXOFlex2_Tab" };


            //get this from      var tab = TabSupport.DecodeTabId(encodedTabId);
            ViewData vd =GetView("ReginSE","Styrsystem1", viewArgs);
            Console.ReadKey();
        }
        private static ViewData GetView(String userName,String area,string[] args)
        {
            const string location = "ws://127.0.0.1:8080/";
            Console.WriteLine("Opening channel");
            DefaultWampChannelFactory channelFactory = new DefaultWampChannelFactory();
            IWampChannel channel = channelFactory.CreateJsonChannel(location, "views");
            channel.Open().Wait(5000);
            IViewService proxy = channel.RealmProxy.Services.GetCalleeProxy<IViewService>();

         

            //get filtered view for area
            var view = proxy.GetView(area,args);

            //delete all elements with no access


            Func<JsonDataNode,String, bool> filterFunc = (node, level) =>
            {
                foreach (KeyValuePair<string, object> o in node.Attributes)
                {
                    if (o.Key == "VisibleAccess")
                        return AccessLevels.HasAccess(o.Value.ToString(), level);
                }
                return false;
            };

            //filter all childs
            //TODO :  add user accesslevel
            //var currentUserAccess = AccessSupport.GetCurrentUserAreaAccess(area, username);
            String currentUserAccess = "Guest";
            view.Nodes.First().Children = view.Nodes.First().Children.FilterAccess(currentUserAccess, filterFunc).ToList();

            //Get all dynamicvalues from filtered view
            //add this to a subscription list
            List<DynamicValue> dynamicValueList = view.Nodes.First().GetDynamicValueList();

            Console.WriteLine(view);
            string subId = $"{area}{""}{userName}";
            var json = JsonConvert.SerializeObject(view);
            return new ViewData() { SubscriptionId = subId, Blob = json };

        }
    }
}