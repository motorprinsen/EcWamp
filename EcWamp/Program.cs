using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExoConfig;
using ExoConfig.Query;
using ExoConfig.Support;
using EXO;

namespace EcWamp
{
    class Program
    {
        static void Main(string[] args)
        {
            DomainCx domain =new DomainCx();
            domain.Domain = new DomainCx.tAreaDomain(@"C:\EXO Projects\Regin\Styrsystem1");
            string defaultController=ExoProjectSupport.GetDefaultController(@"C:\EXO Projects\Regin\Styrsystem1");
            EcQuery ecQuery = new EcQuery("esav");
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("Controller", defaultController);
            dict.Add("UnnamedArgument_2", "Detta är arg 2!");
            EcDataSet dataSet = ecQuery.GetData(@"C:\EXO Projects\Regin\Styrsystem1\EXOFlex_1.Esav", dict, domain);
            
            Console.ReadKey();
            
        }

        public static void TT(string filepath, DomainCx domain, string defaultController)
        {
            //string filepath = "";
            var esavFormat = new EcQuery("Esav");
            //if (!EXO.EXOlib.CheckIdentifier(parser.ViewName))
            //{
            //    throw new BadIdentifierException(parser.ViewName, null);
            //}


            //filepath = FileOp.TranslatePath(parser.Path, parent);
            FileInfo f = new FileInfo(filepath);
            if (!f.Exists)
            {
                throw new FileNotFoundException("The call has an invalid File parameter. The file does not exist. (" + filepath + ")");
            }

            var argumentDataSet = esavFormat.GetData(filepath, new DiagnosticDictionary<string, string>(), domain);

            Dictionary<string, string> runtimeArgs = new Dictionary<string, string>();
            string controllerMacro = defaultController;
            runtimeArgs["controller"] = controllerMacro;
            //runtimeArgs["account"] = DataItemFactory.GetAccountMacro(parent);

            if (argumentDataSet.Result == DataSetResults.Success)
            {
                if (argumentDataSet.Nodes[0] != null && argumentDataSet.Nodes[0].Children[0] != null)
                {
                    int i = 0;
                    var arguments = parser.Arguments.ToList();
                    foreach (EcDataNode ecDataNode in argumentDataSet.Nodes[0].Children[0].Children)
                    {
                        //fetch argument names
                        if (i >= arguments.Count) break;

                        string name = ecDataNode.GetProperty("Name").Value;
                        string defaultValue = ecDataNode.GetProperty("Default").Value;

                        string runtimeValue = i < arguments.Count ? arguments[i].Value : defaultValue;

                        if (runtimeValue.ToLower() == "(default)")
                        {
                            runtimeValue = defaultValue;
                        }
                        if (runtimeArgs.ContainsKey(name)) continue;
                        runtimeArgs[name] = runtimeValue;
                        i++;
                    }
                }
            }
            else
            {
                throw new FileInBadFormatException(filepath, argumentDataSet.Result, null);
            }

            // var dataSet = EsavFormat.GetData(filepath, runtimeArgs, FileOp.GetDomain(parent));
            //EcQuery.CollectTimerEvents = true;
            var dataSet = esavFormat.GetData(filepath, runtimeArgs, FileOp.GetDomain(parent));
            //events.AddRange(EcQuery.TimerEntries.Select((entry => new ExoWebSession.WatchEvent { EventText = entry.EventText, ElapsedMs = entry.ElapsedMs })));
            //EcQuery.TimerEntries = new List<EcQuery.TimerEntry>();
            //EcQuery.CollectTimerEvents = false;

            //arguments is 

            //events.Add(new ExoWebSession.WatchEvent
            //{
            //    EventText = "Read file content with arguments"+filepath,
            //    ElapsedMs = sw.ElapsedMilliseconds
            //}); sw.Restart();

            //StaticTimer.Stop(701);

            if (dataSet.Result != DataSetResults.Success)
                return null;
        }
    }
}
