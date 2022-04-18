using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oodle.NET;
using System.IO;
using System.IO.Compression;

namespace DATExtract
{
    internal static class DAT
    {
        private static bool NEW_FORMAT = true;

        private static FileInfo[] files;

        private static string[] filenameTable;

        internal static int TYPE_BOH;

        internal static uint FILES;

        private static long CRC_FNV_OFFSET = -3750763034362895579;
        
        private static long CRC_FNV_PRIME = 1099511628211;

        public static void CheckCompressed(MemoryMappedViewAccessor accessor)
        {
            byte[] compressed = new byte[16];
            accessor.ReadArray(0, compressed, 0, 16);
            if (Encoding.Default.GetString(compressed) == "CMP2CMP2CMP2CMP2")
            {
                throw new Exception("Compressed archives are not supported yet.");
            }
        }

        public static void GetInfo(MemoryMappedViewAccessor startOfFile, MemoryMappedFile mmf)
        {
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
                info_block.Read(0, out TYPE_BOH);

                info_block.Read(4, out FILES);

                if (FILES != 0x3443432e && FILES != 0x2e434334 && TYPE_BOH != 0x3443432e && TYPE_BOH != 0x2e434334)
                {

                    OldFormat(info_block, mmf);
                }
                else
                {
                    NewFormat(info_block, mmf);
                }
            }
        }

        private static void OldFormat(MemoryMappedViewAccessor info_block, MemoryMappedFile mmf)
        {
            uint NAME_INFO = (FILES * 16) + 8;
            info_block.Read(NAME_INFO, out uint NAMES);
            NAME_INFO += 4;

            uint NAME_FIELD_SIZE = 8;
            if (TYPE_BOH <= -5)
            {
                NAME_FIELD_SIZE = 12;
            }

            uint NAME_OFF = NAME_INFO + (NAMES * NAME_FIELD_SIZE);
            info_block.Read(NAME_OFF, out uint NAMECRC_OFF);

            GetCRCs(info_block, NAMECRC_OFF);
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

            info_block.Read(12, out int TYPE_BOH);
            Endian.Swap(ref TYPE_BOH);

            info_block.Read(16, out uint NEW_FORMAT_VER);
            Endian.Swap(ref NEW_FORMAT_VER);

            info_block.Read(20, out FILES);
            Endian.Swap(ref FILES);

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
                //info_block.Read(offset + 6 + (i * length), out ushort BLANK);
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
                    if (FILE_ID != 0)
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
            info_block.Read(offset + 4 + (NAMES * length), out FILES);
            Endian.Swap(ref FILES);

            offset = offset + 8 + (NAMES * length);

            files = new FileInfo[FILES];
            filenameTable = paths;

            for (int i = 0; i < FILES; i++)
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

                long packed = 0;
                if (TYPE_BOH <= -12)
                {
                    packed = fileOffset;
                    packed >>= 56;
                    fileOffset &= 0xffffffffffffff;
                    if (packed != 0)
                    {
                        packed = 2;
                    }
                }

                files[i].offset = fileOffset;
                files[i].zsize = ZSIZE;
                files[i].size = SIZE;
                files[i].packed = packed;

                offset += 16;
            }

            GetCRCs(info_block, offset);

            for (int i = 0; i < FILES; i++)
            {
                int j = GetName(i);

                //if (!filenameTable[i].Contains("EYE_OBIWAN_20TH_NRM_DX11.TEXTURE".ToLower()))
                //{
                //    continue;
                //}

                ExtractFile(mmf.CreateViewAccessor(files[j].offset, files[j].zsize), j, files[j], filenameTable[i]);
            }

            Console.WriteLine("Successfully extracted {0} out of {1} files!", Compression.totalExtracted, FILES);
        }

        private static int GetName(int id)
        {
            string test = filenameTable[id];
            string fullname = test.Substring(1);
            long crc = CRC_FNV_OFFSET;
            foreach (char character in fullname.ToUpper())
            {
                crc ^= character;
                crc *= CRC_FNV_PRIME;
            }

            for (int i = 0; i < FILES; i++)
            {
                if (files[i].crc == crc)
                {
                    return i;
                }
            }

            Console.WriteLine("Could not find CRC of file: {0}", fullname);

            return 0;
        }

        private static void GetCRCs(MemoryMappedViewAccessor info_block, uint offset)
        {
            // should check if offset is less than the file size according to script
            //long temp4 = offset + (FILES * 4);
            //long temp8 = offset + (FILES * 8);

            for (int i = 0; i < FILES; i++)
            {
                info_block.Read(offset + (i * 8), out long crc);
                Endian.Swap(ref crc);
                files[i].crc = crc;
            }
        }

        public static void ExtractFile(MemoryMappedViewAccessor chunk, int id, FileInfo file, string filename)
        {
            filename = filename.ToUpper();

            float div = (float)(Compression.totalExtracted) / DAT.FILES;
            uint percentage = (uint)(div * 100);

            ManageConsole.ChangeTitle($"Extracting... ({percentage}%)");

            Console.WriteLine(file.offset);

            uint chunkProgress = 0;
            uint offset = 0;

            byte[] completeFile = new byte[file.size];
            int previousCopy = 0;

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
                else if (char1 == 'R' && char2 == 'F' && char3 == 'P' && char4 == 'K')
                {
                    Console.WriteLine("Warning: File {0} uses a compression method that has not yet been implemented!", filename);
                }
                else if (char1 == 'D' && char2 == 'F' && char3 == 'L' && char4 == 'T')
                {
                    chunk.Read(4 + offset, out compressedSize);
                    chunk.Read(8 + offset, out decompressedSize);

                    //Console.WriteLine("File {0} has bytes: {1} {2}", filename.Substring(filename.Length - 16, 16), chunk.ReadByte(12 + offset).ToString("X"), chunk.ReadByte(13 + offset).ToString("X"));

                    byte[] buffer = new byte[compressedSize];
                    chunk.ReadArray(12 + offset, buffer, 0, (int)compressedSize);

                    //decompressed = Compression.Deflate(buffer, decompressedSize);

                    //try
                    //{
                        decompressed = Compression.Deflate(buffer, decompressedSize);
                    //}
                    //catch (Exception ex)
                    //{
                    //    Console.WriteLine("Failed file {0}", filename);
                    //}
                    //Console.WriteLine("Warning: File {0} uses a compression method that has not yet been implemented!", filename);

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
