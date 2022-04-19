using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATLib
{
    internal static class Debug
    {
        public static void WriteHex(string start, object hex)
        {
            Console.WriteLine("{0} {1:X}", start, hex);
        }

        public static void WriteHex(object hex)
        {
            Console.WriteLine("{0:x}", hex);
        }
    }
}
