using EXO;
using ExoConfig.Query;
using ExoConfig.Support;
using JsonData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using SystemEx;
using WampSharp.V2;
using WampSharp.V2.Realm;

namespace EcWamp
{
    public class ViewService : IViewService
    {
        [HandleProcessCorruptedStateExceptions]
        public JsonDataSet GetView(string area, object[] args)
        {
            Console.WriteLine($"Getting view on thread {Thread.CurrentThread.ManagedThreadId}");
            Console.WriteLine($"Are we 64bit right now? {Environment.Is64BitProcess}");
            // We already have the the proj path in the EXOscada Function
            var projPath = @"C:\EXO Projects\Regin";
            var fullAreaPath = Path.Combine(projPath, area);

            string defaultController = ExoProjectSupport.GetDefaultController(fullAreaPath);

            return ParseEsavAndGenerateJsonDataSet(projPath, area, defaultController, args);
        }

        private JsonDataSet ParseEsavAndGenerateJsonDataSet(string projPath, string area, string defaultController, object[] args)
        {
            WFRuntimeArguments parser = new WFRuntimeArguments(args);

            //string filepath = "";
            var esavFormat = new EcQuery("Esav");
            if (!EXO.EXOlib.CheckIdentifier(parser.ViewName))
            {
                //throw new BadIdentifierException(parser.ViewName, null);
                Console.WriteLine("BadIdentifierException");
                return null;
            }

            //var filepath = domain.TransVirtPath(viewFile);
            DomainCx domain = FileOp.CreateDomain(projPath, area);

            //Get full path to viewfile
            var filepath = domain.GetPath(parser.ViewFile);
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
                Console.WriteLine("FileInBadFormatException");
            }

            var dataSet = esavFormat.GetData(filepath, runtimeArgs/*, domain*/);

            if (dataSet.Result != DataSetResults.Success)
            {
                Console.WriteLine("dataSet.Result != DataSetResults.Success");
            }
            //convert ec to json data
            var jsonDataSet = ConvertEcDataSetToJsonDataSet(dataSet);
            //delete arguments
            jsonDataSet.Nodes.First().Children.RemoveAt(0);

            //flatten child elements
            //TODO : flatten all?
            jsonDataSet.Nodes.First().Children = FlattenElements(jsonDataSet.Nodes.First().Children).ToList();

            //Blob
            return jsonDataSet;

            //param till wampservern callee procedure
            //JsonDataSet dataSet = GetView(String virtPath = "Area:\....esav", string area = "styrsystem1");

            ///wampa över jsonDataSet
            ///Deserialize
            ///accescontrol filter
            ///Change all bindings  (*....)
            ///blob
        }

        public static IEnumerable<JsonDataNode> FlattenElements(IEnumerable<JsonDataNode> items)
        {
            if (items == null || items.Count() == 0) return new List<JsonDataNode>();

            var result = new List<JsonDataNode>();
            var q = new Queue<JsonDataNode>(collection: items);

            while (q.Count > 0)
            {
                var item = q.Dequeue();
                if (!(item.Type == "ElementsFolder" || item.Type == "ElementsGroup"))
                {
                    result.Add(item);
                }

                if (item?.Children?.Count() > 0)
                    foreach (var child in item.Children)
                        q.Enqueue(child);
            }

            return result;
        }

        private JsonDataSet ConvertEcDataSetToJsonDataSet(EcDataSet source)
        {
            var result = new JsonDataSet
            {
                Nodes = new List<JsonDataNode>(),
                Format = source.Format,
                Result = (JsonDataSetResults)Enum.Parse(typeof(JsonDataSetResults), source.Result.ToString())
            };

            foreach (var node in source.Nodes)
            {
                result.Nodes.Add(ConvertEcDataNodeToJsonDataNode(node));
            }

            return result;
        }

        private JsonDataNode ConvertEcDataNodeToJsonDataNode(EcDataNode source)
        {
            var result = new JsonDataNode
            {
                Children = new List<JsonDataNode>(),
                Attributes = new Dictionary<string, object>(),
                Type = source.Type
            };

            foreach (var property in source.Properties)
            {
                var jsonProperty = new JsonDataProperty
                {
                    Parameters = new List<JsonDataPropertyParameter>(),
                    Name = property.Name,
                    IsDefault = property.IsDefault,
                    Type = property.Type,
                    Value = property.Value
                };

                result.Attributes.Add(jsonProperty.Name, jsonProperty.Value);
            }

            foreach (var child in source.Children)
            {
                result.Children.Add(ConvertEcDataNodeToJsonDataNode(child));
            }

            return result;
        }
    }

    public class WampServer
    {
        private DefaultWampHost wampHost;
        private IWampHostedRealm realm;
        private readonly ViewService viewService = new ViewService();

        public void Start()
        {
            var location = "ws://127.0.0.1:8080/";
            wampHost = new DefaultWampHost(location);
            realm = wampHost.RealmContainer.GetRealmByName("views");
            Task<IAsyncDisposable> registrationTask = realm.Services.RegisterCallee(viewService);
            registrationTask.Wait();
            wampHost.Open();
            Console.WriteLine($"Wamp server started at {location}");
        }
    }

    public class Program
    {
        private static void Main(string[] args)
        {
            var viewService = new ViewService();
            var viewArgs = new[] { @"Area:\EXOFlex.Esav", "EXOFlex %MsgProj(200257)%", "EXOFlex_Tab" };

            //var viewArgs = new[] { @"Area:\EXOFlex_1.Esav", "EXOFlex %MsgProj(200257)%", "EXOFlex_1_Tab", "(Default)", "ss" };
            var view = viewService.GetView("Styrsystem1", viewArgs);

            new WampServer().Start();
            new AutoResetEvent(false).WaitOne();
            Console.ReadKey();
        }
    }
}