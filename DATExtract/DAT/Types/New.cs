using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace DATExtract
{
    public partial class DATFile
    {
        private void LoadAsNew()
        {
            hdrBlock.Seek(4, SeekOrigin.Begin);
            string cc4 = hdrBlock.ReadString(4);
            if (cc4 != ".CC4") { throw new Exception("Endianness is swapped!"); }
            
            hdrBlock.Seek(4, SeekOrigin.Current); // 0TAD
            
            archiveId = hdrBlock.ReadInt(true);
            version = hdrBlock.ReadInt(true);
            fileCount = hdrBlock.ReadUint(true);

            uint namesCount = hdrBlock.ReadUint(true);
            uint namesSize = hdrBlock.ReadUint(true);

            if (version < 2) { throw new Exception("Format version not implemented!"); }

            uint offset = 32 + 4 + namesSize;

            int id = 0;

            string[] folders = new string[namesCount];
            string[] paths = new string[namesCount];

            hdrBlock.Seek(offset, SeekOrigin.Begin);

            for (int i = 0; i < namesCount; i++)
            {
                uint nameOffset = hdrBlock.ReadUint(true);
                ushort folderId = hdrBlock.ReadUshort(true);
                short something = hdrBlock.ReadShort(true);
                short someId = hdrBlock.ReadShort(true);
                ushort fileId = hdrBlock.ReadUshort(true);

                long previousPosition = hdrBlock.Position;

                if (nameOffset != 0xffffffff)
                {
                    hdrBlock.Seek(32 + nameOffset, SeekOrigin.Begin);
                    string NAME = hdrBlock.ReadNullString();
                    if (i == namesCount - 1)
                    {
                        fileId = (ushort)id;
                    }

                    NAME = folders[folderId] + "\\" + NAME;

                    if (fileId != 0)
                    {
                        paths[id] = NAME;
                        id++;
                    }
                    else
                    {
                        folders[i] = NAME;
                    }
                }

                hdrBlock.Seek(previousPosition, SeekOrigin.Begin);
            }

            hdrBlock.Seek(4, SeekOrigin.Current); // archiveId
            fileCount = hdrBlock.ReadUint(true);

            files = new CompFile[fileCount];

            for (int i = 0; i < fileCount; i++)
            {
                long fileOffset = hdrBlock.ReadLong(true);

                uint zsize = hdrBlock.ReadUint(true);
                uint size = hdrBlock.ReadUint(true);

                long packed = 0;
                if (archiveId <= -13)
                {
                    packed = fileOffset;
                    packed >>= 56;
                    fileOffset &= 0xffffffffffffff;
                    if (packed != 0)
                    {
                        packed = 2;
                    }
                }
                else if (archiveId <= -10)
                {
                    packed = size;
                    size &= 0x7fffffff;
                    packed >>= 31;
                    if (packed != 0)
                    {
                        packed = 2;
                    }
                }
                else
                { // Untested code block.
                    packed = hdrBlock.ReadByte();
                    ushort ZERO = hdrBlock.ReadUshort();
                    byte OFFSET2 = hdrBlock.ReadByte();
                    offset <<= 8;
                    offset |= OFFSET2;
                }

                files[i].offset = fileOffset;
                files[i].zsize = zsize;
                files[i].size = size;
                files[i].packed = packed;
            }

            GetCRCs();

            for (int i = 0; i < fileCount; i++)
            {
                int fileId = GetFileID(i, paths[i]);

                if (paths[i].StartsWith("\\")) paths[i].Substring(1);
                files[fileId].path = paths[i];
            }
        }
    }
}
