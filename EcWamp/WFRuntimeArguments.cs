using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcWamp
{
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

        //public WFRuntimeArguments(WebArea area, object[] args)
        //{
        //    AreaName = area.AreaName;
        //    Init(args);
        //}

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
}
