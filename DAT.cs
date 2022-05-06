using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using Oodle.NET;
using System.IO;
using System.IO.Compression;

namespace DATLib
{
    internal static class DAT
    {
        internal static MemoryMappedFile mmf;

        private static bool NEW_FORMAT = true;

        public static FileInfo[] files;

        private static string[] filenameTable;

        internal static int TYPE_BOH;

        internal static uint fileCount;

        //private static long CRC_FNV_OFFSET = -3750763034362895579;

        //private static long CRC_FNV_PRIME = 1099511628211;

        private static long CRC_FNV_OFFSET_32 = 0x811c9dc5;

        private static long CRC_FNV_PRIME_32 = 0x199933;

        private static long CRC_FNV_OFFSET_64 = -3750763034362895579;

        private static long CRC_FNV_PRIME_64 = 1099511628211;


        internal static void CheckCompressed(MemoryMappedViewAccessor accessor)
        {
            byte[] compressed = new byte[16];
            accessor.ReadArray(0, compressed, 0, 16);
            if (Encoding.Default.GetString(compressed) == "CMP2CMP2CMP2CMP2")
            {
                throw new Exception("Compressed archives are not supported yet.");
            }
        }

        internal static void GetInfo()
        {
            using (var startOfFile = mmf.CreateViewAccessor(0, 16))
            {
                CheckCompressed(startOfFile);

                startOfFile.Read(0, out uint offset);
                if ((offset & 0x80000000) != 0)
                {
                    offset ^= 0xffffffff;
                    offset <<= 8;
                    offset += 0x100;
                }

                startOfFile.Read(4, out uint size);

                using (var info_block = mmf.CreateViewAccessor(offset, size))
                {
                    Console.WriteLine("offset: " + offset);

                    info_block.Read(0, out TYPE_BOH);

                    info_block.Read(4, out fileCount);

                    if (fileCount != 0x3443432e && fileCount != 0x2e434334 && TYPE_BOH != 0x3443432e && TYPE_BOH != 0x2e434334)
                    {
                        OldFormat(info_block, mmf);
                    }
                    else
                    {
                        NewFormat(info_block, mmf);
                    }
                }
            }
        }

        internal static List<string> pathsOldFormat;

        private static void OldFormat(MemoryMappedViewAccessor info_block, MemoryMappedFile mmf)
        {
            uint NAME_INFO = (fileCount * 16) + 8;
            info_block.Read(NAME_INFO, out uint NAMES);
            NAME_INFO += 4;

            uint NAME_FIELD_SIZE = 8;
            if (TYPE_BOH <= -5)
            {
                NAME_FIELD_SIZE = 12;
            }

            uint NAME_OFF = NAME_INFO + (NAMES * NAME_FIELD_SIZE);
            Console.WriteLine(NAME_OFF);
            info_block.Read(NAME_OFF, out uint NAMECRC_OFF);
            NAME_OFF += 4;
            NAMECRC_OFF += NAME_OFF;

            files = new FileInfo[fileCount];

            if (TYPE_BOH != -1)
            { // Lego Star Wars 1 does not contain crc
                GetCRCs(info_block, NAMECRC_OFF);
            }

            uint newOffset = 0;
            if (TYPE_BOH <= -2)
            {
                newOffset = 8;
            }

            pathsOldFormat = new List<string>();

            filenameTable = new string[fileCount];

            int arrPos = 0;
            //nameOffset = 0;
            for (int i = 0; i < fileCount; i++)
            { // Don't ask
                filenameTable[i] = SetName(info_block, ref NAME_INFO, NAME_OFF, ref arrPos);
                int fileId = GetName(i);

                files[fileId].path = filenameTable[i];
                int offset = (fileId * 16) + 8;
                info_block.Read(offset, out uint fileOffset);
                info_block.Read(offset + 4, out uint zsize);
                info_block.Read(offset + 8, out uint size);
                info_block.Read(offset + 12, out uint packed);
                packed &= 0x00ffffff;
                if (TYPE_BOH != -1)
                {
                    fileOffset <<= 8;
                }
                files[fileId].offset = fileOffset + info_block.ReadByte(offset + 15);
                files[fileId].zsize = zsize;
                files[fileId].size = size;
                files[fileId].packed = packed;
            }
        }

        private static void NewFormat(MemoryMappedViewAccessor info_block, MemoryMappedFile mmf)
        {
            byte[] check = new byte[4];
            // HDR_SIZE at offset 0
            info_block.ReadArray(4, check, 0, 4);
            if (check[0] != 0x2e || check[1] != 0x43 || check[2] != 0x43 || check[3] != 0x34) // .CC4
            {
                throw new Exception("Endianness is swapped");
            }

            info_block.Read(12, out TYPE_BOH);
            Endian.Swap(ref TYPE_BOH);

            info_block.Read(16, out uint NEW_FORMAT_VER);
            Endian.Swap(ref NEW_FORMAT_VER);

            info_block.Read(20, out fileCount);
            Endian.Swap(ref fileCount);

            info_block.Read(24, out uint NAMES);
            Endian.Swap(ref NAMES);

            info_block.Read(28, out uint NAMES_SIZE);
            Endian.Swap(ref NAMES_SIZE);

            if (NEW_FORMAT_VER < 2) { throw new Exception("Format version not implemented..."); }
            
            uint offset = 32 + 4 + NAMES_SIZE; // + 4 adjusts for unnecessary data!

            int id = 0;

            uint length = 12;

            //files.Add(new FileInfo());

            string[] folders = new string[NAMES];
            string[] paths = new string[NAMES];

            for (int i = 0; i < NAMES; i++)
            {
                info_block.Read(offset + (i * length), out uint NAME_OFF);
                Endian.Swap(ref NAME_OFF);
                info_block.Read(offset + 4 + (i * length), out ushort FOLDER_ID);
                Endian.Swap(ref FOLDER_ID);
                info_block.Read(offset + 8 + (i * length), out short SOME_ID);
                Endian.Swap(ref SOME_ID);
                info_block.Read(offset + 10 + (i * length), out ushort FILE_ID);
                Endian.Swap(ref FILE_ID);

                if (NAME_OFF != 0xffffffff)
                {
                    string NAME = "";
                    //info_block.Read(32 + NAME_OFF, out NAME);
                    bool previousZero = false;
                    uint nameOffset = 0;
                    while (true)
                    {
                        info_block.Read(32 + NAME_OFF + nameOffset, out byte currByte);
                        if (currByte == 0)
                        {
                            if (previousZero == true) { NAME = NAME.Substring(0, NAME.Length - 1); break; }
                            previousZero = true;
                        }
                        NAME += (char)currByte;
                        nameOffset++;
                    }

                    if (i == NAMES - 1)
                    {
                        FILE_ID = (ushort)id;
                    }

                    NAME = folders[FOLDER_ID] + "\\" + NAME;
                    if (FILE_ID  != 0)
                    {
                        paths[id] = NAME;
                        id++;
                    }
                    else
                    {
                        folders[i] = NAME;
                    }
                }

            }

            info_block.Read(offset + (NAMES * length), out TYPE_BOH);
            Endian.Swap(ref TYPE_BOH);
            //Console.WriteLine(TYPE_BOH);
            //throw new Exception();
            info_block.Read(offset + 4 + (NAMES * length), out fileCount);
            Endian.Swap(ref fileCount);

            offset = offset + 8 + (NAMES * length);

            files = new FileInfo[fileCount];
            filenameTable = paths;

            Console.WriteLine("baseline offset: " + offset);

            for (int i = 0; i < fileCount; i++)
            {
                long fileOffset;
                if (TYPE_BOH <= -11)
                {
                    info_block.Read(offset, out fileOffset);
                    Endian.Swap(ref fileOffset);
                }
                else
                {
                    throw new Exception("Unimplemented boh type");
                }

                info_block.Read(offset + 8, out uint ZSIZE);
                Endian.Swap(ref ZSIZE);
                
                info_block.Read(offset + 12, out uint SIZE);
                Endian.Swap(ref SIZE);

                if (ZSIZE == 0x1cfc && SIZE == 0x2f31)
                {
                    Console.WriteLine("FILE IN QUESTION:");
                    Console.WriteLine(offset);
                }

                long packed = 0;
                if (TYPE_BOH <= -13) // The original script suggested -12, but that causes errors with dcsv
                {
                    packed = fileOffset;
                    packed >>= 56;
                    fileOffset &= 0xffffffffffffff;
                    if (packed != 0)
                    {
                        packed = 2;
                    }
                }
                else if (TYPE_BOH <= -10)
                {
                    packed = SIZE;
                    SIZE &= 0x7fffffff;
                    packed >>= 31;
                    if (packed != 0)
                    {
                        packed = 2;
                    }
                }
                else
                { // Untested code block.
                    info_block.Read(offset + 16, out byte PACKED);
                    info_block.Read(offset + 17, out ushort ZERO);
                    info_block.Read(offset + 19, out byte OFFSET2);
                    offset <<= 8;
                    offset |= OFFSET2;
                }

                files[i].offset = fileOffset;
                files[i].zsize = ZSIZE;
                files[i].size = SIZE;
                files[i].packed = packed;

                offset += 16;
            }

            GetCRCs(info_block, offset);

            for (int i = 0; i < fileCount; i++)
            {
                int fileId = GetName(i);

                files[fileId].path = filenameTable[i];

                //if (!filenameTable[i].Contains("EYE_OBIWAN_20TH_NRM_DX11.TEXTURE".ToLower()))
                //{
                //    continue;
                //}

            }
        }

        //private static int nameOffset = 0;

        private static string SetName(MemoryMappedViewAccessor info_block, ref uint currentOffset, uint NAME_OFF, ref int arrPos)
        {
            short NEXT = 1;
            string FULLPATH = "";
            string NAME = "";
            //Console.WriteLine("currentOffset: " + currentOffset);
            while (NEXT > 0)
            {
                info_block.Read(currentOffset, out NEXT);
                //Console.WriteLine("next: " + NEXT);
                info_block.Read(currentOffset + 2, out short PREV);
                info_block.Read(currentOffset + 4, out int OFF);
                //Console.WriteLine(OFF);

                //Console.WriteLine("next: " + NEXT);
                //Console.WriteLine("prev: " + PREV);
                //Console.WriteLine("off: " + OFF);

                if (TYPE_BOH <= -5)
                {
                    currentOffset += 4; // unnecessary data or something idk
                }

                NAME = "";
                if (OFF >= 0)
                {
                    //Console.WriteLine("offset stuff");
                    //Console.WriteLine(OFF);
                    //Console.WriteLine(NAME_OFF);
                    //Console.WriteLine(nameOffset);
                    OFF += (int)NAME_OFF;
                    //Console.WriteLine(OFF);

                    //bool previousZero = false;
                    int nameOffset = 0;
                    while (true)
                    {
                        info_block.Read(OFF + nameOffset, out byte currByte);
                        if (currByte == 0)
                        {
                            //if (NAME.Length == 0)
                            //{
                            //    nameOffset++;
                            //    continue;
                            //}
                            nameOffset++;
                            break;
                            //if (previousZero == true) { NAME = NAME.Substring(0, NAME.Length - 1); nameOffset++; break; }
                            //previousZero = true;
                        }
                        NAME += (char)currByte;
                        nameOffset++;
                    }
                    //Console.WriteLine(NAME);
                    //currentOffset += nameOffset;
                }

                //Console.WriteLine("name: " + NAME);

                //FULLPATH = "";
                if (PREV != 0)
                {
                    //Console.WriteLine(PREV);
                    FULLPATH = pathsOldFormat[PREV];
                    //Console.WriteLine("Read path: " + FULLPATH);
                }
                pathsOldFormat.Add(FULLPATH);
                if (NEXT > 0)
                {
                    //Console.WriteLine("lll");
                    string temp = pathsOldFormat[PREV];
                    //Console.WriteLine("kkk");
                    //Console.WriteLine(FULLPATH);
                    //Console.WriteLine(temp);
                    //if (temp != "")
                    //{
                    //    throw new Exception("used");
                    //    FULLPATH = @"\" + temp + @"\";
                    //}
                    if (NAME != "")
                    {
                        FULLPATH += NAME + @"\";
                    }
                }
                arrPos += 1;
                currentOffset += 8;
            }

            string fullName = @"\" + FULLPATH + NAME;

            //Console.WriteLine(fullName);
            //throw new Exception();

            return fullName;
        }

        private static int GetName(int id)
        {
            if (TYPE_BOH == -1) // Only tested with Lego Star Wars 1 from the PS2
            {
                return (int)fileCount - 1 - id;
            }

            string test = filenameTable[id];
            string fullname = test.Substring(1);
            long crc = is64 ? CRC_FNV_OFFSET_64 : CRC_FNV_OFFSET_32;
            //Console.WriteLine("begin manipulate:");
            //Console.WriteLine(crc);
            foreach (char character in fullname.ToUpper())
            {
                crc ^= character;
                crc *= is64 ? CRC_FNV_PRIME_64 : CRC_FNV_PRIME_32;
                //Console.WriteLine(crc);
            }
            //Console.WriteLine(fullname.ToUpper());

            //Console.WriteLine(crc);

            if (!is64)
            {
                crc &= 0xffffffff;
            }

            //Console.WriteLine(crc);

            for (int i = 0; i < fileCount; i++)
            {
                if (files[i].crc == crc)
                {
                    return i;
                }
            }

            //Console.WriteLine(fullname.ToUpper());

            //throw new Exception();

            Console.WriteLine("Could not find CRC of file: {0}", fullname);

            return 0;
        }

        internal static bool is64 = false;

        private static void GetCRCs(MemoryMappedViewAccessor info_block, uint offset)
        {
            is64 = false;

            // should check if offset is less than the file size according to script
            //long temp4 = offset + (FILES * 4);
            //long temp8 = offset + (FILES * 8);

            Console.WriteLine(offset);
            info_block.Read(offset + (fileCount * 4), out uint test);
            uint endBlock64 = offset + (fileCount * 8);
            if (test != 0 && TYPE_BOH <= -8) // This TYPE_BOH check is just a guess, it might not be perfect and needs to be pushed backwards further 
            {
                is64 = true;
            }

            Console.WriteLine("64-bit archive: " + is64);

            for (int i = 0; i < fileCount; i++)
            {
                if (is64)
                {
                    info_block.Read(offset + (i * 8), out long crc);
                    Endian.Swap(ref crc);
                    files[i].crc = crc;
                }
                else
                {
                    info_block.Read(offset + (i * 4), out uint crc);
                    files[i].crc = crc;
                }
            }
        }

        internal static void ExtractFile(FileInfo file)
        {
            using (var chunk = mmf.CreateViewAccessor(file.offset, file.zsize))
            {
                string filename = file.path;

                float div = (float)(Compression.totalExtracted) / DAT.fileCount;
                uint percentage = (uint)(div * 100);

                ManageConsole.ChangeTitle($"Extracting... ({percentage}%)");

                uint chunkProgress = 0;
                uint offset = 0;

                byte[] completeFile = new byte[file.size];
                int previousCopy = 0;

                if (file.zsize < 5)
                {
                    chunk.ReadArray(0, completeFile, 0, (int)file.zsize);
                    Compression.WriteFile(filename, completeFile);
                    return;
                }

                while (chunkProgress < file.zsize)
                {
                    char char1 = (char)chunk.ReadByte(0 + offset);
                    char char2 = (char)chunk.ReadByte(1 + offset);
                    char char3 = (char)chunk.ReadByte(2 + offset);
                    char char4 = (char)chunk.ReadByte(3 + offset);

                    uint compressedSize = 0;
                    uint decompressedSize = 0;

                    byte[] decompressed = new byte[0];

                    bool compressed = true;

                    if (char1 == 'O' && char2 == 'O' && char3 == 'D' && char4 == 'L')
                    {
                        chunk.Read(4 + offset, out compressedSize);
                        chunk.Read(8 + offset, out decompressedSize);

                        byte[] buffer = new byte[compressedSize];
                        chunk.ReadArray(12 + offset, buffer, 0, (int)compressedSize);

                        decompressed = Compression.ExtractOodle(buffer, decompressedSize, filename);

                    }
                    else if (char1 == 'O' && char2 == 'O' && char3 == 'D' && char4 == '2')
                    {
                        // Uses oodle with a different parameter
                        Console.WriteLine("Warning: File {0} uses a compression method that has not yet been implemented!", filename);
                    }
                    else if (char1 == 'L' && char2 == 'Z' && char3 == '2' && char4 == 'K')
                    {
                        Console.WriteLine("Warning: File {0} uses a compression method that has not yet been implemented!", filename);
                    }
                    else if (char1 == 'Z' && char2 == 'L' && char3 == 'I' && char4 == 'B')
                    {
                        Console.WriteLine("Warning: File {0} uses a compression method that has not yet been implemented!", filename);
                    }
                    else if (char1 == 'R' && char2 == 'N' && char3 == 'C')
                    {
                        byte[] buffer = new byte[file.zsize];
                        chunk.ReadArray(0, buffer, 0, (int)file.zsize);

                        chunk.Read(4, out decompressedSize);
                        Endian.Swap(ref decompressedSize);
                        compressedSize = file.zsize;
                        decompressed = new byte[(int)file.size];
                        RNC.Unpack(buffer, decompressed);
                    }
                    else if (char1 == 'R' && char2 == 'F' && char3 == 'P' && char4 == 'K')
                    {
                        Console.WriteLine("Warning: File {0} uses a compression method that has not yet been implemented!", filename);
                    }
                    else if (char1 == 'L' && char2 == 'Z' && char3 == 'M' && char4 == 'A')
                    {
                        Console.WriteLine("Warning: File {0} uses a compression method that has not yet been implemented!", filename);
                    }
                    else if (char1 == 'D' && char2 == 'F' && char3 == 'L' && char4 == 'T')
                    {
                        chunk.Read(4 + offset, out compressedSize);
                        chunk.Read(8 + offset, out decompressedSize);

                        byte[] buffer = new byte[compressedSize];
                        chunk.ReadArray(12 + offset, buffer, 0, (int)compressedSize);

                        decompressed = Compression.Deflate(buffer, decompressedSize);
                    }
                    else if (char1 == 'Z' && char2 == 'I' && char3 == 'P' && char4 == 'X')
                    {
                        byte[] key = new byte[] { chunk.ReadByte(4 + offset), chunk.ReadByte(5 + offset), chunk.ReadByte(6 + offset), chunk.ReadByte(7 + offset) };
                        chunk.Read(4 + offset, out compressedSize);

                        byte[] buffer = new byte[compressedSize];
                        chunk.ReadArray(12 + offset, buffer, 0, (int)compressedSize);

                        decompressed = Compression.ExtractZIPX(buffer, key, compressedSize, filename);
                    }
                    else
                    {
                        byte[] buffer = new byte[file.size];
                        chunk.ReadArray(0 + offset, buffer, 0, (int)file.size);
                        Array.Copy(buffer, 0, completeFile, previousCopy, buffer.Length);
                        previousCopy += buffer.Length;

                        compressedSize = (uint)buffer.Length;

                        compressed = false;
                    }

                    if (decompressed != null && decompressed.Length > 0)
                    {
                        Array.Copy(decompressed, 0, completeFile, previousCopy, decompressed.Length);
                        previousCopy += decompressed.Length;
                    }
                    else if (compressed)
                    {
                        Console.WriteLine("Could not extract {0}", filename);
                        Extract.AddFailedFile(filename);
                        return;
                    }

                    if (compressedSize == 0)
                    {
                        throw new Exception("CompressedSize of chunk == 0?");
                    }

                    offset += 12;
                    offset += compressedSize;

                    chunkProgress += 12;
                    chunkProgress += compressedSize;
                }

                Compression.WriteFile(filename, completeFile);
            }
        }
    }
}
