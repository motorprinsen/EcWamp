using EcWamp.Subscriptions;
using JsonData;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using WampSharp.V2;

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
            TestMasterList();

            var viewArgs = new[] { @"Area:\EXOFlex2.Esav", "EXOFlex2", "EXOFlex2_Tab" };

            //get this from      var tab = TabSupport.DecodeTabId(encodedTabId);
            ViewData vd = GetView("ReginSE", "Styrsystem1", viewArgs);
            Console.ReadKey();
        }

        private static ViewData GetView(String userName, String area, string[] args)
        {
            const string location = "ws://127.0.0.1:8080/";
            Console.WriteLine("Opening channel");
            DefaultWampChannelFactory channelFactory = new DefaultWampChannelFactory();
            IWampChannel channel = channelFactory.CreateJsonChannel(location, "views");
            channel.Open().Wait(5000);
            IViewService proxy = channel.RealmProxy.Services.GetCalleeProxy<IViewService>();

            //get filtered view for area
            var view = proxy.GetView(area, args);

            //delete all elements with no access

            Func<JsonDataNode, String, bool> filterFunc = (node, level) =>
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
            List<BindableElement> bindableElementsList = view.Nodes.First().GetBindableElementsList();

            Console.WriteLine(view);
            string subId = $"{area}{""}{userName}";
            var json = JsonConvert.SerializeObject(view);
            return new ViewData() { SubscriptionId = subId, Blob = json };
        }

        private static void TestMasterList()
        {
            var masterList = new MasterList(variable => Console.WriteLine($"Heard that {variable} was evicted from the MasterList"));
            masterList.Add("Controller1.OutdoorTemp");
            masterList.Add("controller1.outdoortemp");
            masterList.Add("Controller2.RoomTemp");

            var refs1 = masterList.References("controller1.outdoortemp");
            Console.WriteLine($"controller1.outdoortemp has {refs1} refs");

            masterList.Remove("CONTROLLER1.OUTDOORTEMP");
            masterList.Remove("Controller2.RoomTemp");
            masterList.Remove("Controller1.OutdoorTemp");
        }
    }
}