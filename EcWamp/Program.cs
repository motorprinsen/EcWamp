using EXO;
using ExoConfig.Query;
using ExoConfig.Support;
using JsonData;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EcWamp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            DomainCx domain = new DomainCx();
            domain.Domain = new DomainCx.tAreaDomain(@"C:\EXO Projects\Regin\Styrsystem1");
            string defaultController = ExoProjectSupport.GetDefaultController(@"C:\EXO Projects\Regin\Styrsystem1");
            var viewArgs = new[] { @"Area:\EXOFlex_1.Esav", "EXOFlex %MsgProj(200257)%", "EXOFlex_1_Tab", "(Default)", "ss" };

            TT(@"C:\EXO Projects\Regin\Styrsystem1\EXOFlex_1.Esav", domain, defaultController, viewArgs);

            //EcQuery ecQuery = new EcQuery("esav");
            //Dictionary<string, string> dict = new Dictionary<string, string>();
            //dict.Add("Controller", defaultController);
            //dict.Add("UnnamedArgument_2", "Detta är arg 2!");
            //EcDataSet dataSet = ecQuery.GetData(@"C:\EXO Projects\Regin\Styrsystem1\EXOFlex_1.Esav", dict, domain);

            Console.ReadKey();
        }

        public static void TT(string filepath, DomainCx domain, string defaultController, object[] args)
        {
            WFRuntimeArguments parser = new WFRuntimeArguments(args);
            //string filepath = "";
            var esavFormat = new EcQuery("Esav");
            if (!EXO.EXOlib.CheckIdentifier(parser.ViewName))
            {
                //throw new BadIdentifierException(parser.ViewName, null);
                Console.WriteLine("BadIdentifierException");
                return;
            }

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
                Console.WriteLine("FileInBadFormatException");
            }

            var dataSet = esavFormat.GetData(filepath, runtimeArgs, domain);

            if (dataSet.Result != DataSetResults.Success)
            {
                Console.WriteLine("dataSet.Result != DataSetResults.Success");
            }
            //convert ec to json data
            var jsonDataSet = ConvertEcDataSetToJsonDataSet(dataSet);
            //delete arguments
            jsonDataSet.Nodes.First().Children.RemoveAt(0);

            //Blob
            var json = JsonConvert.SerializeObject(jsonDataSet);

            //param till wampservern callee procedure
            //JsonDataSet dataSet = GetView(String virtPath = "Area:\....esav", string area = "styrsystem1");

            ///wampa över jsonDataSet
            ///Deserialize 
            ///accescontrol filter
            ///Change all bindings  (*....)
            ///blob



        }

        public static JsonDataSet ConvertEcDataSetToJsonDataSet(EcDataSet source)
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

        public static JsonDataNode ConvertEcDataNodeToJsonDataNode(EcDataNode source)
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

          
                result.Attributes.Add(jsonProperty.Name , jsonProperty.Value);
            }

            foreach (var child in source.Children)
            {
                result.Children.Add(ConvertEcDataNodeToJsonDataNode(child));
            }

            return result;
        }
    }
}