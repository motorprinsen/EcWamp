using EXO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EcWamp
{
    public static class FileOp
    {
        public static string getPath(string Domain, string AreaName, string RelativePath)
        {
            lock (EXOGLibSupport.busy)
            {
                uint garbage = 0;
                var MyArea = new DomainCx.tAreaDomain(Domain + "\\" + AreaName);

                var tempdomain = new DomainCx();

                tempdomain.Domain = MyArea;

                return tempdomain.TransVirtPath(RelativePath, 0, ref garbage);
            }
        }
        /// <summary>
        /// Function to fetch a file that is either in proj: or prod: or exoscada: . I.e. outside area definition.
        /// </summary>
        /// <param name="path">The relative path</param>
        /// <returns>The full path to file</returns>
        public static string GetExoFile(string path)
        {
            return EXOGLibSupport.TranslateFilePath(path);
        }

        /// <summary>
        /// Translates a relative path according to the contextNodes location
        /// </summary>
        /// <param name="inPath">The relative Path</param>
        /// <param name="contextNode">The node </param>
        /// <returns>The full path to file</returns>
        //public static string TranslatePath(string inPath, IDataItem contextNode)
        //{

        //    //handle commaseparated 
        //    CommaSeparatedChoises ch = new CommaSeparatedChoises(inPath);
        //    WebArea parentArea = null;
        //    ITreeNode nodeptr = contextNode;

        //    while (nodeptr != null)
        //    {
        //        parentArea = nodeptr as WebArea;
        //        if (parentArea != null)
        //            break;

        //        nodeptr = nodeptr.Parent;
        //    }
        //    if (parentArea == null)
        //        return inPath;

        //    uint garbage = 0;

        //    lock (EXOGLibSupport.busy)
        //    {
        //        var tempdomain = new DomainCx();

        //        if (parentArea != null && !string.IsNullOrEmpty(parentArea.AreaPath))
        //        {
        //            tempdomain.Domain = new DomainCx.tAreaDomain(parentArea.AreaPath);
        //        }
        //        else if (parentArea != null && !string.IsNullOrEmpty(parentArea.ProjectPath))
        //        {
        //            tempdomain.Domain = new DomainCx.tProjectDomain(parentArea.ProjectPath);
        //        }
        //        else
        //        {
        //            tempdomain = new DefaultCx();
        //        }


        //        for (int i = 0; i < ch.Choises.Count; i++)
        //        {
        //            if (string.IsNullOrEmpty(ch.Choises[i])) continue;
        //            ch.Choises[i] = SplitAndTranslate(ch.Choises[i], tempdomain);
        //        }
        //        if (ch.HasDefault && !string.IsNullOrEmpty(ch.Default))
        //        {
        //            ch.Default = SplitAndTranslate(ch.Default, tempdomain);
        //        }


        //        return ch.TranslateToString();
        //    }
        //}
        //public static DomainCx GetDomain(IDataItem contextNode)
        //{

        //    //handle commaseparated 

        //    WebArea parentArea = null;
        //    ITreeNode nodeptr = contextNode;

        //    while (nodeptr != null)
        //    {
        //        parentArea = nodeptr as WebArea;
        //        if (parentArea != null)
        //            break;

        //        nodeptr = nodeptr.Parent;
        //    }

        //    uint garbage = 0;

        //    lock (EXOGLibSupport.busy)
        //    {
        //        var tempdomain = new DomainCx();
        //        if (parentArea == null)
        //            return new DefaultCx();

        //        if (!string.IsNullOrEmpty(parentArea.AreaPath))
        //        {
        //            tempdomain.Domain = new DomainCx.tAreaDomain(parentArea.AreaPath);
        //        }
        //        else if (!string.IsNullOrEmpty(parentArea.ProjectPath))
        //        {
        //            tempdomain.Domain = new DomainCx.tProjectDomain(parentArea.ProjectPath);
        //        }
        //        else
        //        {
        //            tempdomain = new DefaultCx();
        //        }


        //        return tempdomain;

        //    }
        //}
        public static DomainCx GetDomain()
        {

            //handle commaseparated 

            //WebArea parentArea = null;
            //ITreeNode nodeptr = contextNode;

            //while (nodeptr != null)
            //{
            //    parentArea = nodeptr as WebArea;
            //    if (parentArea != null)
            //        break;

            //    nodeptr = nodeptr.Parent;
            //}

            //uint garbage = 0;

            //lock (EXOGLibSupport.busy)
            //{
            //    var tempdomain = new DomainCx();
            //    if (parentArea == null)
            //        return new DefaultCx();

            //    if (!string.IsNullOrEmpty(parentArea.AreaPath))
            //    {
            //        tempdomain.Domain = new DomainCx.tAreaDomain(parentArea.AreaPath);
            //    }
            //    else if (!string.IsNullOrEmpty(parentArea.ProjectPath))
            //    {
            //        tempdomain.Domain = new DomainCx.tProjectDomain(parentArea.ProjectPath);
            //    }
            //    else
            //    {
            //        tempdomain = new DefaultCx();
            //    }


                return new DomainCx();

            //}
        }
        private static string SplitAndTranslate(string inPath, DomainCx tempdomain)
        {
            string choise = inPath;
            bool first = true;
            foreach (string path in inPath.Split('|'))
            {
                if (first)
                {
                    choise = DoTranslate(path, tempdomain);
                    first = false;
                }
                else
                {
                    choise += "|" + DoTranslate(path, tempdomain);
                }
            }
            return choise;
        }
        private static string DoTranslate(string inPath, DomainCx tempdomain)
        {
            List<string> forbiddens = new List<string>(new[] { "[" });

            string path;
            string suffix = "";
            if (inPath.Contains("["))
            {
                var vars = inPath.Split('[');
                path = vars[0];
                suffix = vars[1];
            }
            else
            {
                path = inPath.Replace('/', '\\');
            }

            if (forbiddens.Any(path.Contains))
            {
                //Logger.log("ConfigurationReader", "Can not Translate path: " + inPath, LogTypes.error);
                return "";
            }


            uint garbage = 0;



            var translated = tempdomain.TransVirtPath(path, 0, ref garbage) + suffix;
            if (translated.ToLower().Contains("accounts") && translated.ToLower().Contains("exoscada"))
            {
                //this is an accounts file.. 
                if (!File.Exists(translated))
                {
                    //translate this to default folder.. 

                    var file = Regex.Split(translated, "Exoscada\\\\Accounts\\\\", RegexOptions.IgnoreCase);
                    if (file.Length == 2)
                    {
                        var parts = file[1].Split(new[] { "\\" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                        parts.RemoveAt(0);
                        return tempdomain.TransVirtPath("Exoscada:Accounts\\Default\\" + Path.Combine(parts.ToArray()), 0, ref garbage);
                    }
                }
            }
            return translated;
        }
    }
}
