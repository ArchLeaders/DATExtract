using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATLib.CustomAlgorithms
{
    internal class DeflateChunk
    {
        internal ushort[] buffer1 = new ushort[16];
        internal ushort[] buffer2 = new ushort[16];
        internal int[] buffer3 = new int[16];
        internal int[] buffer4 = new int[16];


        internal byte[] bufferA = new byte[1024];
        internal short[] bufferB = new short[1024];

        internal ushort[] output = new ushort[0x400];

        internal int[] occurs = new int[16];

        internal byte[] chunk;

        internal DeflateChunk(byte[] _chunk)
        {
            chunk = _chunk;

            for (int i = 0; i < output.Length; i++)
            {
                output[i] = 0xffff;
            }
        }
    }
}
