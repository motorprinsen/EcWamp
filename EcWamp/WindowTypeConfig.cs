using ExoConfig.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;

namespace EcWamp
{
    public class WindowTypeConfig
    {
        public WindowTypeConfig(string inViewName, string file, string title, List<string> args, string areaName)
        {
            string builtInViewType = inViewName;
            if (!file.Contains("%"))
            {
                builtInViewType = "documentview";
                args.Clear();
                args.Add(file);
                args.Add(title);
                SubType = "%DocumentView%";
            }
            else
                SubType = file;

            Arguments = new DiagnosticDictionary<string, string>();
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("WebFramework.Components.windowtypearguments.xml");

            XPathDocument conf = new XPathDocument(stream);

            XPathNavigator xp = conf.CreateNavigator();

            XPathNodeIterator niUrl = xp.Select("/windowtypes/" + builtInViewType + "/url");

            if (niUrl.Count > 0)
            {
                niUrl.MoveNext();
                URL = niUrl.Current.Value.Trim();
            }
            else
            {
                URL = "";
            }

            XPathNodeIterator niArgs = xp.Select("/windowtypes/" + builtInViewType + "/arguments/argument");
            string exoFile = FileOp.GetExoFile(@"slib:\EXOscada Built-In Views\" + builtInViewType + ".esbiv");
            EcDataSet ecDataSet = null;
            if (File.Exists(exoFile))
            {
                EcQuery ecQuery = new EcQuery("Esav");
                ecDataSet = ecQuery.GetData(exoFile);
            }


            int i = 0;
            foreach (XPathNavigator node in niArgs)
            {
                string argumentName = node.GetAttribute("name", "");

                if (args.Count > i && args[i] != null && args[i] != "(Default)")
                {
                    if (args[i].ToLower().StartsWith("area:"))
                    {
                        var tmp = args[i];
                        tmp = tmp.ToLower().Replace("area:", "Proj:" + areaName);
                        Arguments[argumentName] = tmp;
                    }
                    else
                        Arguments[argumentName] = args[i];


                }
                else
                {
                    if (ecDataSet != null && i < ecDataSet.Nodes[0].Children[0].Children.Count)
                        Arguments[argumentName] =
                            ecDataSet.Nodes[0].Children[0].Children[i].GetProperty("Default").Value;
                    else
                    {
                        Arguments[argumentName] = node.GetAttribute("default", "");
                    }
                }
                i++;
            }

            if (areaName != null)
                Arguments["Area"] = areaName;
            //Guid g = WFInput._StaticArguments.Set(Arguments);
            //URL += "?key=" + g;
        }

        public DiagnosticDictionary<string, string> Arguments { get; private set; }
        public string URL { get; private set; }

        public string SubType { get; set; }
    }
}
