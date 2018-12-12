using ExoConfig.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WampClient.Query;
using WampSharp.V2;
using WampSharp.V2.Rpc;

namespace WampClient
{
    public interface IBlopService
    {
        [WampProcedure("com.arguments.GetView")]
        JsonDataSet GetView(string area = "", string esavFile = "");
    }
    class Program
    {
        static void Main(string[] args)
        {
            Thread.Sleep(2000);
            const string location = "ws://127.0.0.1:8080/";
            Console.WriteLine("Open channel");
            DefaultWampChannelFactory channelFactory = new DefaultWampChannelFactory();

            IWampChannel channel = channelFactory.CreateJsonChannel(location, "realm");

            channel.Open().Wait(5000);


            IBlopService proxy =
                  channel.RealmProxy.Services.GetCalleeProxy<IBlopService>();



            object v =proxy.GetView("2", "3");

           // Console.WriteLine("Add2: {0}", result);

          





            Console.ReadLine();
        }
    }
}
