using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATLib
{
    public struct FileInfo
    {
        public long crc;
        public string path;
        public long offset;
        public uint zsize;
        public uint size;
        public long packed;
    }
}
