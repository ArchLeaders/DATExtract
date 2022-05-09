using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.MemoryMappedFiles;

namespace DATLib.DATFormats
{
    internal static class Legacy
    {
        public static void Extract(MemoryMappedViewAccessor info_block, MemoryMappedFile mmf)
        {
            uint fileCount = AccessDAT.fileCount;
            int TYPE_BOH = AccessDAT.TYPE_BOH;

            for (int i = 0; i < fileCount; i++)
            {
                //info_block.Read(i, )
            }
        }
    }
}
