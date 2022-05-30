using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace DATExtract.Decompressors
{
    public static class RNC
    {
        [DllImport("RNC.dll")]
        private static extern int RNCUnpack(byte[] input, byte[] output, int inSize, int outSize);

        public static void Unpack(byte[] input, byte[] output, int inSize, int outSize)
        {
            try
            {
                RNCUnpack(input, output, inSize, outSize);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
