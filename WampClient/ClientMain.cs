using EcWamp.Subscriptions;
using EXOscadaAPI.Protocols;
using EXOScadaAPI.DataStore;
using EXOScadaAPI.DataStore.Wamp;
using JsonData;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using WampSharp.V2;

namespace WampClient
{
    public class ViewData
    {
        public string SubscriptionId { get; set; }
        public string Blob { get; set; }
    }

    internal class ClientMain
    {
        const string location = "ws://127.0.0.1:8080/";
        static IViewService viewsProxy;
        static IWampChannel viewsChannel;

        static IOperationsService operationsProxy;
        static IWampChannel operationsChannel;
        static IDisposable subscription;

        private static void Main(string[] args)
        {
            Console.WriteLine("Opening channel");
            DefaultWampChannelFactory channelFactory = new DefaultWampChannelFactory();

            viewsChannel = channelFactory.CreateJsonChannel(location, "views");
            viewsChannel.Open().Wait(5000);
            viewsProxy = viewsChannel.RealmProxy.Services.GetCalleeProxy<IViewService>();

            operationsChannel = channelFactory.CreateJsonChannel(location, "data");
            operationsChannel.Open().Wait(5000);
            operationsProxy = operationsChannel.RealmProxy.Services.GetCalleeProxy<IOperationsService>();

            //subscription = operationsChannel.RealmProxy.Services.GetSubject<DataStoreMessage>("subscriptions")
            //       .Subscribe(x =>
            //       {
            //           Console.WriteLine($"WampClient got update from server: {x.Variable} -> {x.Value}");
            //       });

            TestMasterList();
            TestRead();

            var viewArgs = new[] { @"Area:\EXOFlex2.Esav", "EXOFlex2", "EXOFlex2_Tab" };

            //get this from      var tab = TabSupport.DecodeTabId(encodedTabId);
            ViewData vd = GetView("ReginSE", "Styrsystem1", viewArgs);
            Console.ReadKey();

            subscription.Dispose();
            operationsChannel.Close();
            viewsChannel.Close();
        }

        private static void TestRead()
        {
            var dataStore = new DataStore("127.0.0.1", "", 8080, "");
            dataStore.Open();
            var msg1 = dataStore.Read("Controller1.OutdoorTemp");
            var msg2 = dataStore.Read("Controller1.OutdoorTemp");
            var msg3 = dataStore.Read("Controller1.RoomTemp");

            Console.WriteLine($"Got {msg1.Result.Value} for {msg1.Result.Variable}");
            Console.WriteLine($"Got {msg2.Result.Value} for {msg2.Result.Variable}");
            Console.WriteLine($"Got {msg3.Result.Value} for {msg3.Result.Variable}");
        }

        private static ViewData GetView(String userName, String area, string[] args)
        {
            //get filtered view for area
            var view = viewsProxy.GetView(area, args);

            if (view == null) return null;

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
            var variables1 = new List<BindableElement>
            {
                new BindableElement(){ BindingAddress = "Controller1.OutdoorTemp", AttributeName = "Value", ElementIndex = "1" },
                new BindableElement(){ BindingAddress = "Controller2.RoomTemp", AttributeName = "Value", ElementIndex = "2" },
                new BindableElement(){ BindingAddress = "Controller1.OutdoorTemp", AttributeName = "Value", ElementIndex = "3" }
            };

            var variables2 = new List<BindableElement>
            {
                new BindableElement(){ BindingAddress = "Controller1.OutdoorTemp", AttributeName = "Value", ElementIndex = "1" },
                new BindableElement(){ BindingAddress = "Controller2.OutdoorTemp", AttributeName = "Value", ElementIndex = "2" },
                new BindableElement(){ BindingAddress = "Controller3.SAFmode", AttributeName = "Value", ElementIndex = "3" }
            };

            var subscriptionService = new SubscriptionService();
            var stream1 = subscriptionService.GetStream("sid", variables1);
            var stream2 = subscriptionService.GetStream("sid2", variables2);

            var subject = stream1.Updates.Subscribe(tuple =>
            {
                Console.WriteLine($"{tuple.Element.BindingAddress} is now {tuple.Value}");
                // TODO: Here we OnNext to GraphQL
            });

            var subject2 = stream1.Updates.Subscribe(tuple =>
            {
                Console.WriteLine($"{tuple.Element.BindingAddress} is now {tuple.Value}");
                // TODO: Here we OnNext to GraphQL
            });

            var subject3 = stream2.Updates.Subscribe(tuple => 
            {
                Console.WriteLine($"{tuple.Element.BindingAddress} is now {tuple.Value}");
                // TODO: Here we OnNext to GraphQL
            });

            Console.ReadKey();

            Console.WriteLine("Removing stream");
            subscriptionService.Remove("sid");
            subject.Dispose();
            subject2.Dispose();
            Console.ReadKey();
            subscriptionService.Remove("sid2");
            subject3.Dispose();
            Console.ReadKey();
            Console.WriteLine("Everything is disposed");
            Console.ReadKey();

            //var masterList = new MasterList(evictedVariable => Console.WriteLine($"Heard that {evictedVariable} was evicted from the MasterList"));
            //var obs1 = masterList.GetOrAdd("Controller1.OutdoorTemp");
            //var obs2 = masterList.GetOrAdd("controller1.outdoortemp");
            //var obs3 = masterList.GetOrAdd("Controller2.RoomTemp");

            //obs1.Subscribe(_ => Console.WriteLine("obs1 changed"));
            //obs2.Subscribe(_ => Console.WriteLine("obs2 changed"));
            //obs3.Subscribe(_ => Console.WriteLine("obs3 changed"));

            //var refs1 = masterList.References("controller1.outdoortemp");
            //Console.WriteLine($"controller1.outdoortemp has {refs1} refs");

            //masterList.Remove("CONTROLLER1.OUTDOORTEMP");
            //masterList.Remove("Controller2.RoomTemp");
            //masterList.Remove("Controller1.OutdoorTemp");
        }
    }
}