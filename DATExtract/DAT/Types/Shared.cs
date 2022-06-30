using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ModLib;

namespace DATExtract
{
    public partial class DATFile
    {
        // GetFileID took ridiculously long if I didn't do a lookup
        private Dictionary<long, int> crcTranslation = new();

        private int GetFileID(int thisId, string fullname)
        {
            if (archiveId == -1)
            {
                return (int)fileCount - 1 - thisId;
            }

            long crc = is64 ? CRC_FNV_OFFSET_64 : CRC_FNV_OFFSET_32;
            fullname = fullname.Substring(1);
            foreach (char character in fullname.ToUpper())
            {
                crc ^= character;
                crc *= is64 ? CRC_FNV_PRIME_64 : CRC_FNV_PRIME_32;
            }

            if (!is64)
            {
                crc &= 0xffffffff;
            }

            if (crcTranslation.ContainsKey(crc))
            {
                return crcTranslation[crc];
            }

            for (int i = 0; i < files.Length; i++) // I think that despite having to reloop, this will still be quicker in the long run
            {
                if (files[i].path == fullname)
                {
                    return i;
                }
            }

            Console.WriteLine("Could not find CRC of file: {0}", fullname);

            return 0;
        }

        private bool DetectVariant()
        {

            long currentOffset = hdrBlock.Position;
            byte lastByte = 0x00;

            // BIG-ENDIAN: 32-BIT
            bool isBig32 = true;
            for (int i = 0; i < fileCount / 4; i++) // Take a small sample, and try to figure out DAT structure
            {
                byte currByte = hdrBlock.ReadByte();
                hdrBlock.Seek(3, SeekOrigin.Current);
                if (currByte < lastByte)
                {
                    isBig32 = false;
                    break;
                }
                lastByte = currByte;
            }

            if (isBig32)
            {
                is64 = false;
                return true;
            }

            // LITTLE-ENDIAN: 32-BIT
            bool isLittle32 = true;
            lastByte = 0x00;

            hdrBlock.Seek(currentOffset + 3, SeekOrigin.Begin);

            for (int i = 0; i < fileCount / 4; i++)
            {
                byte currByte = hdrBlock.ReadByte();
                hdrBlock.Seek(3, SeekOrigin.Current);
                if (currByte < lastByte)
                {
                    isLittle32 = false;
                    break;
                }
                lastByte = currByte;
            }

            if (isLittle32)
            {
                is64 = false;
                return false;
            }

            // PROBABLY 64-BIT
            return true;
        }

        private void GetCRCs()
        {
            long currentOffset = hdrBlock.Position;
            hdrBlock.Seek(nameSignOffset + (fileCount * 4), SeekOrigin.Begin);
            uint test = hdrBlock.ReadUint();
            if (test != 0 && archiveId <= -8)
            {
                is64 = true;
            }

            hdrBlock.Seek(currentOffset, SeekOrigin.Begin);


            bool bigEndian = DetectVariant();
            
            Console.WriteLine("Implicit archive format: {0}-Bit ({1}-Endian)", is64 ? "64" : "32", bigEndian ? "Big" : "Little");

            hdrBlock.Seek(currentOffset, SeekOrigin.Begin);

            bool checkForCollisions = false;

            for (int i = 0; i < fileCount; i++)
            {
                if (is64)
                {
                    files[i].crc = hdrBlock.ReadLong(true);
                }
                else
                {
                    files[i].crc = hdrBlock.ReadUint(bigEndian);
                }

                crcTranslation[files[i].crc] = i;

                if (files[i].crc == 0)
                {
                    checkForCollisions = true;
                }
            }

            if (checkForCollisions == false) return;

            uint collisionFiles = hdrBlock.ReadUint(bigEndian);

            uint collisionNamesSize = hdrBlock.ReadUint();
            if (collisionFiles > 0)
            {
                Console.WriteLine("CRC collisions detected for {0} files, fixing...", collisionFiles);
                for (int i = 0; i < collisionFiles; i++)
                {
                    string path = hdrBlock.ReadNullString();
                    while(true)
                    {
                        byte testByte = hdrBlock.ReadByte();
                        if (testByte >= 60)
                        {
                            hdrBlock.Seek(-1, SeekOrigin.Current);
                            break;
                        } 
                        else if (testByte != 0)
                        {
                            break;
                        }
                    }

                    files[i].path = path;
                }
            }
        }
    }
}
