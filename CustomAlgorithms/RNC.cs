using System;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
namespace DATLib
{
    internal static class RNC
    {
        [DllImport("RNC.dll")]
        private static extern int RNCUnpack(byte[] input, byte[] output, int inSize, int outSize);

        public static void Unpack(byte[] input, byte[] output)
        {
            int inSize = input.Length;
            int outSize = output.Length;
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
