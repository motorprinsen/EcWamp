using EXO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcWamp
{
    public class EXOGLibSupport
    {
        public static object busy = new object();

        public static string TranslateFilePath(string relativePath)
        {
            lock (busy)
            {
                EXO.EXOlib.SyncThread();
                uint gbg = 0;
                return DefaultCx.TransVirtPath(relativePath, 0, ref gbg);
            }
        }
    }
}
