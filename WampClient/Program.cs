using EcWamp.Subscriptions;
using EXOscadaAPI.Protocols;
using EXOScadaAPI.DataStore.Wamp;
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
        const string location = "ws://127.0.0.1:8080/";
        static IViewService viewsProxy;
        static IWampChannel viewsChannel;

        static IOperationsService operationsProxy;
        static IWampChannel operationsChannel;
        static IDisposable subscription;

        private static void Main(string[] args)
        {
            TestMasterList();

            Console.WriteLine("Opening channel");
            DefaultWampChannelFactory channelFactory = new DefaultWampChannelFactory();

            viewsChannel = channelFactory.CreateJsonChannel(location, "views");
            viewsChannel.Open().Wait(5000);
            viewsProxy = viewsChannel.RealmProxy.Services.GetCalleeProxy<IViewService>();

            operationsChannel = channelFactory.CreateJsonChannel(location, "data");
            operationsChannel.Open().Wait(5000);
            operationsProxy = operationsChannel.RealmProxy.Services.GetCalleeProxy<IOperationsService>();

            subscription = operationsChannel.RealmProxy.Services.GetSubject<DataStoreMessage>("subscriptions")
                   .Subscribe(x =>
                   {
                       Console.WriteLine($"WampClient got update from server: {x.Variable} -> {x.Value}");
                   });

            TestRead();

            var viewArgs = new[] { @"Area:\EXOFlex2.Esav", "EXOFlex2", "EXOFlex2_Tab" };

            //get this from      var tab = TabSupport.DecodeTabId(encodedTabId);
            //ViewData vd = GetView("ReginSE", "Styrsystem1", viewArgs);
            Console.ReadKey();

            subscription.Dispose();
            operationsChannel.Close();
            viewsChannel.Close();
        }

        private static void TestRead()
        {
            operationsProxy.Read("Controller1.OutdoorTemp");
        }

        private static ViewData GetView(String userName, String area, string[] args)
        {
            //get filtered view for area
            var view = viewsProxy.GetView(area, args);

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