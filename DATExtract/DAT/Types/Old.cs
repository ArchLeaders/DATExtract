using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATExtract
{
    public partial class DATFile
    {
        internal List<string> pathsOld;

        private void LoadAsOld()
        {
            nameInfoOffset = (fileCount * 16) + 8;
            hdrBlock.Seek(nameInfoOffset, SeekOrigin.Begin);
            nameInfoOffset += 4;

            uint namesCount = hdrBlock.ReadUint();
            int nameFieldSize = archiveId <= -5 ? 12 : 8;

            namesOffset = nameInfoOffset + (uint)(namesCount * nameFieldSize);
            hdrBlock.Seek(namesOffset, SeekOrigin.Begin);
            nameSignOffset = hdrBlock.ReadUint(false);
            namesOffset += 4;
            nameSignOffset += namesOffset;

            files = new CompFile[fileCount];

            if (archiveId != -1) // -2 = LIJ1 on PS3
            {
                hdrBlock.Seek(nameSignOffset, SeekOrigin.Begin);
                GetCRCs();
            }

            pathsOld = new List<string>();

            hdrBlock.Seek(8, SeekOrigin.Begin);
            for (int i = 0; i < fileCount; i++)
            {
                uint offset = hdrBlock.ReadUint();
                files[i].zsize = hdrBlock.ReadUint();
                files[i].size = hdrBlock.ReadUint();
                uint packed = hdrBlock.ReadUint();
                files[i].packed = packed & 0x00ffffff;
                if (archiveId != -1)
                {
                    offset <<= 8;
                }
                files[i].offset = offset + (packed >> 24);
            }

            //if (archiveId == -2)
            //{
            //    hdrBlock.Seek(16, SeekOrigin.Current);
            //    GetCRCs();
            //}

            string[] paths = new string[fileCount];
            for (int i = 0; i < fileCount; i++)
            {
                string name = @"\" + SetName();
                paths[i] = name;
                int fileId = GetFileID(i, name);

                files[fileId].path = name;
            }

            //uint spaceSaved = 0;
            //for (int i = 0; i < fileCount; i++)
            //{
            //    if (files[i].packed != 0)
            //    {
            //        spaceSaved += files[i].size - files[i].zsize;
            //        if (files[i].path.Contains("\\stuff\\"))
            //            Console.WriteLine(files[i].path);
            //    }
            //}

            //Console.WriteLine("Space saved: " + spaceSaved);
        }

        internal uint nameBlockPointer = 0;

        private string SetName()
        {
            short NEXT = 1;
            string FULLPATH = "";
            string NAME = "";
            hdrBlock.Seek(nameBlockPointer + nameInfoOffset, SeekOrigin.Begin);
            while (NEXT > 0)
            {
                NEXT = hdrBlock.ReadShort();
                short PREV = hdrBlock.ReadShort();
                int OFF = hdrBlock.ReadInt();

                if (archiveId <= -5)
                {
                    nameBlockPointer += 4;
                    hdrBlock.Seek(4, SeekOrigin.Current); // Some stupid value
                }

                long currPos = hdrBlock.Position;

                NAME = "";
                if (OFF >= 0)
                {
                    OFF += (int)(namesOffset);
                    hdrBlock.Seek(OFF, SeekOrigin.Begin);
                    NAME = hdrBlock.ReadNullString();
                }

                if (PREV != 0)
                {
                    FULLPATH = pathsOld[PREV];
                }
                pathsOld.Add(FULLPATH);
                if (NEXT > 0)
                {
                    if (NAME != "")
                    {
                        FULLPATH += NAME + @"\";
                    }
                }

                hdrBlock.Seek(currPos, SeekOrigin.Begin);
                nameBlockPointer += 8;
            }

            return FULLPATH + NAME;
        }
    }
}
