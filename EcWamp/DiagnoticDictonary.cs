
using EXO;
using ExoConfig.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
            string exoFile = "";// FileOp.GetExoFile(@"slib:\EXOscada Built-In Views\" + builtInViewType + ".esbiv");
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
           
        }

        public DiagnosticDictionary<string, string> Arguments { get; private set; }
        public string URL { get; private set; }

        public string SubType { get; set; }
    }
    public static class Numbers
    {
        private static long areanumber = 0;
        private static long sessionnumber = 0;
        private static long windownumber = 0;

        private static long Next(ref long number)
        {
            return System.Threading.Interlocked.Increment(ref number);
        }

        public static string NextWebAreaToString()
        {
            return Next(ref areanumber).ToString();
        }
        public static string NextSessionToString()
        {
            return Next(ref sessionnumber).ToString();
        }
        public static string NextWindowToString()
        {
            return Next(ref windownumber).ToString();
        }
    }
    public enum WindowTypes
    {
        esav,
        web,
        swf,
        esbiv,
        eshv,
        system,
        esc
    };
    public class WFRuntimeArguments
    {
        public WFRuntimeArguments()
        {
            Arguments = new DiagnosticDictionary<string, string>();
            Type = WindowTypes.system;
            Path = "";
            ViewName = "Internal" + Numbers.NextWindowToString();
            ViewTitle = "";
        }

        public WFRuntimeArguments(object[] inargs)
        {
            Init(inargs);
        }

        private void Init(object[] inargs)
        {
            Arguments = new DiagnosticDictionary<string, string>();

            ViewTitle = "";
            URL = "";
            Path = inargs[0] as string;
            ParseFileArgument(inargs[0]); //parse file argument, always the first argument to open---- call. 

            List<string> tmp = (
                                   from arg in inargs
                                   where arg != null
                                   select arg.ToString()
                               ).ToList();

            tmp.RemoveRange(0, 3);

            if (Type == WindowTypes.esbiv || Type == WindowTypes.eshv)
            {
                var c = new WindowTypeConfig(ViewName, Path, ViewTitle, tmp, AreaName);
                URL = c.URL;
                Arguments = c.Arguments;
                SubType = c.SubType;
            }
            else
            {
                for (int i = 0; i < tmp.Count; i++)
                {
                    Arguments.Add(i.ToString(), tmp[i]);
                }
            }

            if (inargs[1] != null && !inargs[1].ToString().Equals(string.Empty))
                ViewTitle = inargs[1] as string;

            if (inargs[2] != null && !inargs[2].ToString().Equals(string.Empty))
                ViewName = inargs[2] as string;
            else
                ViewName = "Internal" + Numbers.NextWindowToString();
        }

        public string SubType { get; set; }

     

        protected string AreaName { get; set; }

        public WindowTypes Type { get; private set; }

        public string Path { get; private set; }
        public string ViewName { get; private set; }
        public string ViewTitle { get; private set; }
        public DiagnosticDictionary<string, string> Arguments { get; private set; }

        public string URL { get; set; }

        public DateTime LastWriteTime { get; set; }

        private void ParseFileArgument(object arg)
        {
            string a = arg.ToString().ToLower();
            if (a.StartsWith("http://") ||
                a.StartsWith("https://") ||
                a.StartsWith("file://") ||
                a.StartsWith("ftp://"))
            {
                Type = WindowTypes.web;
                URL = arg.ToString();
            }
            else if (a.EndsWith(".esc"))
            {
                Type = WindowTypes.esc;
            }
            else if (a.EndsWith(".esav"))
            {
                Type = WindowTypes.esav;
            }
            else
            {
                Type = WindowTypes.esbiv;
            }
            //strip down arg to a proper viewName
            ViewName = Strip(arg.ToString());
        }

        private string Strip(string p)
        {
            string[] stringparts = p.ToLower().Split('/');

            string[] stripstrings = { ".esav", ".esc", ".esbiv", ".eshv", "area:", ".", ":", "/", "\\", "%" };

            string fileName = "";

            if (stringparts.Length > 0)
                fileName = stringparts[stringparts.Length - 1]; //May be ""...

            if (fileName == "")
                fileName = p.ToLower();

            foreach (string s in stripstrings)
                fileName = fileName.Replace(s, "");

            return fileName;
        }
    }
    public class DiagnosticDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        protected object tag;
        protected string name = "unknown";

        /// <summary>
        /// Gets/sets an object that you can associate with the dictionary.
        /// </summary>
        /// De
        public object Tag
        {
            get { return tag; }
            set { tag = value; }
        }

        /// <summary>
        /// The dictionary name. The default is "unknown". 
        /// Used to enhance the KeyNotFoundException.
        /// </summary>

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// Parameterless constructor.
        /// </summary>
        public DiagnosticDictionary()
        {

        }

        public DiagnosticDictionary(IEqualityComparer<TKey> comp) : base(comp)
        {
        }

        /// <summary>
        /// Constructor that takes a name.
        /// </summary>
        public DiagnosticDictionary(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// Constructor that takes a name.
        /// </summary>
        public DiagnosticDictionary(string name, IEqualityComparer<TKey> comp) : base(comp)
        {
            this.name = name;
        }
        /// <summary>
        /// Indexer that produces a more useful KeyNotFoundException.
        /// </summary>
        public new TValue this[TKey key]
        {
            get
            {
                try
                {
                    return base[key];
                }
                catch (KeyNotFoundException)
                {

                    throw new KeyNotFoundException("The key '" + key.ToString() +
                       "' was not found in the dictionary '" + name + "'. in method: " + System.Reflection.MethodBase.GetCurrentMethod().Name);
                }
            }

            set { base[key] = value; }
        }
        public void Add(Dictionary<TKey, TValue> indict)
        {
            foreach (KeyValuePair<TKey, TValue> kvp in indict)
            {
                Add(kvp.Key, kvp.Value);
            }
        }
    }
}
