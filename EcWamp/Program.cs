using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExoConfig;
using ExoConfig.Query;
using ExoConfig.Support;
using SystemEx;
using WampSharp.V2;
using WampSharp.V2.Realm;

namespace EcWamp
{
    class Program
    {
        static void Main(string[] args)
        {
           // OpenWampHost();
            BlopService blob = new BlopService();
            blob.GetView();


            Console.ReadKey();
            
        }
        public static void OpenWampHost()
        {
            const string location = "ws://127.0.0.1:8080/";

            using (IWampHost host = new DefaultWampHost(location))
            {

                IWampHostedRealm realm1 = host.RealmContainer.GetRealmByName("realm");

                Task<IAsyncDisposable> registrationTask = realm1.Services.RegisterCallee(new BlopService());
                // await registrationTask;
                registrationTask.Wait();
                host.Open();

                Console.WriteLine("Wamp is open on" + location);
                Console.ReadKey();

            }
        }

    }
}
