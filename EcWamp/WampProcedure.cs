using ExoConfig.Query;
using ExoConfig.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WampSharp.V2.Rpc;

namespace EcWamp
{
    public interface IBlopService
    {
        [WampProcedure("com.arguments.GetView")]
        object GetView(string area = "", string esavFile ="");
    }

    public class BlopService : IBlopService
    {
        public object GetView(string area = "", string esavFile = "")
        {
            EXO.DomainCx domain = new EXO.DomainCx();
            domain.Domain = new EXO.DomainCx.tAreaDomain(@"C:\EXO Projects\Regin\Styrsystem1");
            string defaultController = ExoProjectSupport.GetDefaultController(@"C:\EXO Projects\Regin\Styrsystem1");
            EcQuery ecQuery = new EcQuery("esav");
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("Controller", defaultController);
            dict.Add("UnnamedArgument_2", "Detta är arg 2!");
            EcDataSet dataSet = ecQuery.GetData(@"C:\EXO Projects\Regin\Styrsystem1\EXOFlex.Esav", dict, domain);

            //serialize dataSet
            return "";

        }
    }
}
