using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EcWamp
{
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
}
