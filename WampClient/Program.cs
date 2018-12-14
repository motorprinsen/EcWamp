using JsonData;
using System;
using WampSharp.V2;
using WampSharp.V2.Rpc;

namespace WampClient
{
    public interface IViewService
    {
        [WampProcedure("se.rssoftware.eos.getView")]
        JsonDataSet GetView(string area, string viewFile, object[] args);
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            const string location = "ws://127.0.0.1:8080/";
            Console.WriteLine("Opening channel");
            DefaultWampChannelFactory channelFactory = new DefaultWampChannelFactory();

            IWampChannel channel = channelFactory.CreateJsonChannel(location, "views");
            channel.Open().Wait(5000);
            IViewService proxy = channel.RealmProxy.Services.GetCalleeProxy<IViewService>();

            var viewArgs = new[] { @"Area:\EXOFlex_1.Esav", "EXOFlex %MsgProj(200257)%", "EXOFlex_1_Tab", "(Default)", "ss" };
            var view = proxy.GetView("Styrsystem1", @"Area:\EXOFlex_1.Esav", viewArgs);

            Console.WriteLine(view);
            Console.ReadKey();
        }
    }
}