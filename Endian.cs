using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATLib
{
    static class Endian
    {
        public static void Swap(ref uint x)
        {
            // swap adjacent 16-bit blocks
            x = (x >> 16) | (x << 16);
            // swap adjacent 8-bit blocks
            x = ((x & 0xFF00FF00) >> 8) | ((x & 0x00FF00FF) << 8);
        }

        public static void Swap(ref int x)
        {
            uint y = (uint)x;
            Swap(ref y);
            x = (int)y;
        }

        public static void Swap(ref ushort x)
        {
            x = (ushort)((ushort)((x & 0xff) << 8) | ((x >> 8) & 0xff));
        }

        public static void Swap(ref short x)
        {
            ushort y = (ushort)x;
            Swap(ref y);
            x = (short)y;
        }

        public static void Swap(ref ulong x)
        {
 
            // swap adjacent 32-bit blocks
            x = (x >> 32) | (x << 32);
            // swap adjacent 16-bit blocks
            x = ((x & 0xFFFF0000FFFF0000) >> 16) | ((x & 0x0000FFFF0000FFFF) << 16);
            // swap adjacent 8-bit blocks
            x = ((x & 0xFF00FF00FF00FF00) >> 8) | ((x & 0x00FF00FF00FF00FF) << 8);
        }

        public static void Swap(ref long x)
        {
            ulong y = (ulong)x;
            Swap(ref y);
            x = (long)y;
        }
    }
}
