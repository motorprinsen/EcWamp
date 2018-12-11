using ExoConfig.Query;
using ExoConfig.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WampSharp.V2.Rpc;

namespace EcWamp
{

    public interface IBlopService
    {
        [WampProcedure("com.arguments.GetView")]
        object GetView(string area = "", string esavFile = "", object[] args = null);
    }

    public class BlopService : IBlopService
    {
        public object GetView(string area = "", string esavFile = "", object[] args=null)
        {
            EXO.DomainCx domain = new EXO.DomainCx();
            domain.Domain = new EXO.DomainCx.tAreaDomain(@"C:\EXO Projects\Regin\Styrsystem1");
            string defaultController = ExoProjectSupport.GetDefaultController(@"C:\EXO Projects\Regin\Styrsystem1");
            EcQuery ecQuery = new EcQuery("esav");
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("Controller", defaultController);
            dict.Add("UnnamedArgument_2", "Detta är arg 2!");
            String filePath = @"C:\EXO Projects\Regin\Styrsystem1\EXOFlex.Esav";
            EcDataSet dataSet = ecQuery.GetData(filePath, dict, domain);

            getEsav(defaultController, new WFRuntimeArguments(args), domain, filePath);
            //serialize dataSet
            return "";

        }
        public static void getEsav(String defaultController, WFRuntimeArguments parser, EXO.DomainCx domain, String filepath)
        {
            try
            {


                var esavFormat = new EcQuery("Esav");


                FileInfo f = new FileInfo(filepath);
                if (!f.Exists)
                {
                    throw new FileNotFoundException("The call has an invalid File parameter. The file does not exist. (" + filepath + ")");
                }


                //var dataSet = EsavFormat.GetData(filepath);
                //StaticTimer.Start(701, "EcQuery.GetCachedFile");
                //EcQuery.CollectTimerEvents = true;
                var argumentDataSet = esavFormat.GetData(filepath, new DiagnosticDictionary<string, string>(), domain);
                //events.AddRange(EcQuery.TimerEntries.Select((entry => new ExoWebSession.WatchEvent{EventText = entry.EventText, ElapsedMs = entry.ElapsedMs})));
                //EcQuery.TimerEntries = new List<EcQuery.TimerEntry>();
                //EcQuery.CollectTimerEvents = false;

                //events.Add(new ExoWebSession.WatchEvent
                //{
                //    EventText = "Read file content without arguments" + filepath,
                //    ElapsedMs = sw.ElapsedMilliseconds
                //}); sw.Restart();
                //var argumentDataSet = EsavFormat.GetData(filepath);
                Dictionary<string, string> runtimeArgs = new Dictionary<string, string>();
                runtimeArgs["controller"] = defaultController;

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

                }

                // var dataSet = EsavFormat.GetData(filepath, runtimeArgs, FileOp.GetDomain(parent));
                //EcQuery.CollectTimerEvents = true;
                var dataSet = esavFormat.GetData(filepath, runtimeArgs, domain);


                if (dataSet.Result != DataSetResults.Success)
                {

                }

            }
            catch
            {

            }
        }
    }
}
