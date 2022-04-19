// Very very very incomplete attempt at decoding the DFLT algorithm.
// Feel free to poach / do whatever with.

using DATLib.CustomAlgorithms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATLib
{
    internal static class DeflateAlgorithm
    {
        private static long orByte = 0;
        private static int index = 0;
        private static int comparison = 0;
        private static byte[] buffer;

        private static byte[] arr1 = new byte[] { 0x10, 0x11, 0x12, 0x00, 0x08, 0x07, 0x09, 0x06, 0x0A, 0x05, 0x0B, 0x04, 0x0C, 0x03, 0x0D, 0x02, 0x0E, 0x01, 0x0F };
        private static uint[] table1 = new uint[] { 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x1, 0x1, 0x1, 0x1, 0x2, 0x2, 0x2, 0x2, 0x3, 0x3, 0x3, 0x3, 0x4, 0x4, 0x4, 0x4, 0x5, 0x5, 0x5, 0x5, 0x0, 0x0, 0x0, 0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x7, 0x9, 0xD, 0x11, 0x19, 0x21, 0x31, 0x41, 0x61, 0x81, 0xC1, 0x101, 0x181, 0x201, 0x301, 0x401, 0x601, 0x801, 0xC01, 0x1001, 0x1801, 0x2001, 0x3001, 0x4001, 0x6001, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x1, 0x1, 0x1, 0x1, 0x2, 0x2, 0x2, 0x2, 0x3, 0x3, 0x3, 0x3, 0x4, 0x4, 0x4, 0x4, 0x5, 0x5, 0x5, 0x5, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x1, 0x1, 0x2, 0x2, 0x3, 0x3, 0x4, 0x4, 0x5, 0x5, 0x6, 0x6, 0x7, 0x7, 0x8, 0x8, 0x9, 0x9, 0xA, 0xA, 0xB, 0xB, 0xC, 0xC, 0xD, 0xD, 0x0, 0x0, 0x10, 0x11, 0x12, 0x0, 0x8, 0x7, 0x9, 0x6, 0xA, 0x5, 0xB, 0x4, 0xC, 0x3, 0xD, 0x2, 0xE, 0x1, 0xF, 0x0, 0x0, 0xFF, 0xFFFF, 0xFFFFFF, 0xFFFFFFFF };
        private static uint[] table2 = new uint[] { 0x3, 0x4, 0x5, 0x6, 0x7, 0x8, 0x9, 0xA, 0xB, 0xD, 0xF, 0x11, 0x13, 0x17, 0x1B, 0x1F, 0x23, 0x2B, 0x33, 0x3B, 0x43, 0x53, 0x63, 0x73, 0x83, 0xA3, 0xC3, 0xE3, 0x102, 0x0, 0x0, 0x0 };
        private static uint[] table3 = new uint[] { 0x0, 0x0, 0x0, 0x0, 0x1, 0x1, 0x2, 0x2, 0x3, 0x3, 0x4, 0x4, 0x5, 0x5, 0x6, 0x6, 0x7, 0x7, 0x8, 0x8, 0x9, 0x9, 0xA, 0xA, 0xB, 0xB, 0xC, 0xC, 0xD, 0xD, 0x0 };
        private static uint[] table4 = new uint[] { 0x1, 0x2, 0x3, 0x4, 0x5, 0x7, 0x9, 0xD, 0x11, 0x19, 0x21, 0x31, 0x41, 0x61, 0x81, 0xC1, 0x101, 0x181, 0x201, 0x301, 0x401, 0x601, 0x801, 0xC01, 0x1001, 0x1801, 0x2001, 0x3001, 0x4001, 0x6001, 0x0, 0x0 };

        private static int[] result = new int[1024];

        internal static int previousProgress = 0;

        private static DeflateChunk final1;
        private static DeflateChunk final2;

        private static void Manipulate()
        {
            while (comparison < 0x19)
            {
                long currByte = 0;
                if (index < buffer.Length)
                {
                    currByte = buffer[index];
                    index++;
                }
                currByte <<= comparison;
                comparison += 8;

                orByte ^= currByte;
            }
        }

        /// <summary>
        /// Function finished!
        /// </summary>
        /// <param name="length"></param>
        /// <returns>success</returns>
        private static int AnalyseChunk(DeflateChunk chunk, int length)
        {
            if (0 < length)
            {
                for (int i = 0; i < length; i++)
                {
                    byte number = chunk.chunk[i];

                    chunk.occurs[number] = chunk.occurs[number] + 1;
                }
            }

            int cumulative = 0;
            int previous = 0;
            for (int i = 0xf; i > 0; i--)
            {
                int r11 = 0xf - i;
                int count = chunk.occurs[1 + r11];
                chunk.buffer1[r11] = (ushort)cumulative;
                chunk.buffer2[r11] = (ushort)previous;
                chunk.buffer3[1 + r11] = cumulative;

                previous += count;
                count += cumulative;
                cumulative = count * 2;
                chunk.buffer4[r11] = count << (i & 0x1f);
            }
            chunk.buffer4[15] = 0x00010000;
            if (0 < length)
            {
                for (int i = 0; i < length; i++)
                {
                    int number = chunk.chunk[i];
                    if (number != 0)
                    {
                        int fromBuf1 = chunk.buffer1[number - 1];
                        int fromBuf2 = chunk.buffer2[number - 1];
                        int fromBuf3 = chunk.buffer3[number];
                        fromBuf2 -= fromBuf1;
                        fromBuf2 += fromBuf3;
                        if (fromBuf2 == 144)
                        {
                            Console.WriteLine();
                        }
                        chunk.bufferA[fromBuf2] = (byte)number;
                        chunk.bufferB[fromBuf2] = (short)i;
                        if (number < 10)
                        {
                            int aNum = (int)((fromBuf3 >> 1) & 0x5555U) | (2 * (int)(fromBuf3 & 0x5555));
                            int bNum = aNum >> 2 & 0x3333 | (aNum & 0x3333) << 2;
                            int sub = (0x10 - (number & 0x1f));
                            int cNum = bNum >> 4 & 0xf0f | (bNum & 0xf0f) << 4;
                            int final = (int)((((byte)(cNum >> 8)) | ((byte)cNum << 8)) >> sub);
                            if (final < 0x200)
                            {
                                while (final < 0x200)
                                {
                                    long val = final;
                                    final = final + (1 << number);
                                    chunk.output[val] = (ushort)fromBuf2;
                                }
                            }
                        }
                        chunk.buffer3[number] = chunk.buffer3[number] + 1;
                    }
                }
            }
            return 1;
        }

        private static byte[] someByteArray = new byte[1000];

        private static void HelperFunction()
        {
            if (comparison < 5)
            {
                Manipulate();
            }
            comparison -= 5;
            long and = orByte & 0x1F;
            orByte >>= 5;
            result[0] = (int)and + 0x101;
            if (comparison < 5)
            {
                Manipulate();
            }

            comparison -= 5;
            and = orByte & 0x1F;
            orByte >>= 5;
            and++;
            result[1] = (int)and;

            if (comparison < 4)
            {
                Manipulate();
            }

            result[2] = 0x00;
            result[3] = 0x00;

            // Check the results of all the below:
            and = orByte & 0x0f;
            orByte >>= 4;
            comparison -= 4; // [ECX]
            and += 4;

            result[4] = 0x00;
            result[5] = 0x00;
            result[6] = 0x00; 

            if (and != 0)
            {
                for (int i = 0; i < and; i++)
                {
                    if (comparison < 3)
                    {
                        Manipulate();
                    }

                    byte pos = arr1[i];
                    comparison -= 3;

                    byte newByte = (byte)(orByte & 0x07);
                    int remainder = (pos % 4);
                    result[2 + (pos / 4)] ^= (newByte << (8 * (3 - remainder)));

                    orByte >>= 3;
                }
            }

            byte[] chunk = new byte[0x13];
            for (int i = 0; i < chunk.Length; i++)
            {
                chunk[i] = (byte)(0xff & (result[2 + (i / 4)] >> ((3 - i) * 8)));
            }

            DeflateChunk firstChunk = new DeflateChunk(chunk);
            int success = AnalyseChunk(firstChunk, 0x13);
            if (success != 0)
            {
                int i = 0;
                int progress = result[0] + result[1];
                while (i < progress)
                {
                    if (comparison < 0x10)
                    {
                        Manipulate();
                    }
                    and = orByte & 0x1ff;
                    ushort value = (ushort)firstChunk.output[and];
                    if (value < 0xffff) // TODO: Need to initialize buffer as 0xffff as this is an error check
                    {
                        int shift = firstChunk.bufferA[value];
                        orByte >>= shift;
                        comparison -= shift;
                    }
                    else
                    {
                        Console.WriteLine("Something new here!");
                    }

                    ushort test = (ushort)firstChunk.bufferB[value];
                    int increment = 0;
                    if (test < 0x10)
                    {
                        increment = 1;
                        int remainder = (i % 4);
                        someByteArray[i] = (byte)test;
                        //result[8 + (i / 4)] ^= ((byte)test << (8 * (3 - remainder)));
                    }
                    else
                    {
                        byte toCopy = someByteArray[i - 1];
                        if (test == 0x10)
                        {
                            if (comparison < 2)
                            {
                                Manipulate();
                            }
                            and = orByte & 3;
                            orByte >>= 2;
                            comparison -= 2;
                            increment = (int)and + 3;
                        }
                        else if (test == 0x11)
                        {
                            if (comparison < 3)
                            {
                                Manipulate();
                            }
                            comparison -= 3;
                            increment = (int)(orByte & 7) + 3;
                            orByte >>= 3;
                            toCopy = 0x00;
                        }
                        else
                        {
                            if (comparison < 7)
                            {
                                Manipulate();
                            }
                            comparison -= 7;
                            increment = (int)(orByte & 0x7f) + 0xb;
                            orByte >>= 7;
                            toCopy = 0x00;
                            // ^ this line needs to be verified
                        }

                        for (int copy = i; copy < i + increment; copy++)
                        {
                            //if (copy == 214)
                            //{
                            //    Console.WriteLine();
                            //}
                            someByteArray[copy] = toCopy;
                        }
                    }

                    i = i + increment;
                }

                final1 = new DeflateChunk(someByteArray);
                success = AnalyseChunk(final1, result[0]);
                if (success != 0)
                {
                    byte[] newChunk = new byte[result[1]];
                    Array.Copy(someByteArray, result[0], newChunk, 0, result[1]);
                    final2 = new DeflateChunk(newChunk);
                    AnalyseChunk(final2, result[1]);
                }
            }
            return;
        }

        private static void SetupTables()
        {
            byte[] bytes = File.ReadAllBytes(@"C:\Users\Connor\source\repos\DATExtract\CustomAlgorithms\DeflateTable4.hex");
            int[] testTable = new int[bytes.Length / 4];
            for (int i = 0; i < bytes.Length; i++)
            {
                byte thisByte = bytes[i];
                int intPos = i / 4;
                testTable[intPos] = testTable[intPos] | (thisByte << (8 * (i % 4)));
            }
            //setup = true;

            string writeToFile = "";
            for (int i = 0; i < testTable.Length; i++)
            {
                writeToFile += "0x" + testTable[i].ToString("X") + ", ";
            }
            Console.WriteLine(writeToFile);
            Console.WriteLine();
        }

        internal static int Deflate(byte[] chunk, byte[] decompressed, uint decompressedSize)
        {
            resultantFile = decompressed;
            Array.Clear(result, 0, result.Length);
            buffer = chunk;

            orByte = 0;
            index = 0;
            comparison = 0;

            if (comparison < 1)
            {
                Manipulate();
            }

            long and = orByte & 1; // Probably [RSI]
            orByte >>= 1;

            comparison--;

            if (comparison < 2)
            {
                Manipulate();
            }

            comparison -= 2;
            long testByte = (orByte & 3);
            orByte >>= 2;

            if (testByte == 2)
            {
                Console.WriteLine("Got here!");
            }
            else
            {
                int success = 1; // Supposed to check if the functions all return 1, but idc
                if (testByte == 1)
                {
                    Console.WriteLine("not sure what to do here");
                }
                else
                {
                    HelperFunction();
                }

                if (success == 0)
                {
                    return 0;
                }
            }
            if (comparison == 0)
            {
                return 0;
            }
            Finaliser();

            return 1;
        }

        internal static byte[] resultantFile = new byte[32768];

        private static byte[] sampleFile;

        private static void TestWithSample(byte[] resultantFile, long pos)
        {
#if DEBUG
            if (sampleFile == null)
            {
                sampleFile = File.ReadAllBytes(@"D:\Games\SteamLibrary\steamapps\common\LEGO Star Wars - The Skywalker Saga\extracted\ADDITIONALCONTENT\CP_1STPARTYPURCHASE\CHARS\SUPER_CHARACTER_TEXTURE\FACE\EYE\EYE_OBIWAN_20TH_NRM_DX11.TEXTURE");
            }

            byte ourFile = resultantFile[pos];
            byte sample = sampleFile[pos + previousProgress];
            if (ourFile != sample)
            {
                Console.WriteLine("Not happy bunny here...");
            }
#endif
        }

        private static void Finaliser()
        {
            long pos = 0;
            int and;
            ushort current = 0;
            while (true)
            {
                while (true)
                {
                    //if (pos > 0x52da)
                    //{
                    //    Console.WriteLine();
                    //}

                    if (comparison < 0x10)
                    {
                        Manipulate();
                    }
                    and = (int)orByte & 0x1ff;
                    current = final1.output[and];
                    if (current < 0xffff)
                    {
                        byte aByte = final1.bufferA[current];
                        orByte >>= aByte;
                        comparison -= aByte;
                        current = (ushort)final1.bufferB[current];
                    }
                    else
                    {
                        int aNum = (int)((orByte >> 1 & 0x5555U) | (orByte & 0x5555) * 2);
                        int bNum = (int)((aNum >> 2 & 0x3333) | (aNum & 0x3333) << 2);
                        int cNum = (int)((bNum >> 4 & 0xf0f) | (bNum & 0xf0f) << 4);
                        int increment = 10;
                        int another = 10;
                        int reversed = cNum >> 8 | ((cNum & 0xff) << 8);
                        if (final1.buffer4[increment - 1] <= reversed)
                        {
                            while (final1.buffer4[increment - 1] <= reversed)
                            {
                                increment++;
                                //buffer4 += 4;
                                another++;
                            }
                            //Console.WriteLine("Something needs to happen here!");
                        }
                        increment = 16 - (increment & 0x1f);
                        reversed >>= (byte)increment;
                        orByte >>= another;
                        comparison -= another;
                        current = (ushort)final1.bufferB[reversed - final1.buffer1[another - 1] + final1.buffer2[another - 1]];
                    }
                    if (0xff < current) break;
                    resultantFile[pos] = (byte)current;
                    if (pos == 293)
                    {
                        Console.WriteLine();
                    }
                    TestWithSample(resultantFile, pos);
                    pos++;
                }
                if (current == 0x100) { break; }

                uint fromTable1 = table1[current - 0x101];
                uint fromTable2 = table2[current - 0x101];
                if (fromTable1 != 0)
                {
                    if (comparison < fromTable1)
                    {
                        Manipulate();
                    }
                    comparison -= (int)fromTable1;
                    fromTable2 += (uint)((1 << ((byte)fromTable1 & 0x1f)) - 1U & orByte);
                    orByte >>= (byte)(fromTable1 & 0x1f);
                }
                if (comparison < 0x10)
                {
                    Manipulate();
                }
                and = (int)orByte & 0x1ff;
                current = final2.output[and];
                if (current < 0xffff)
                {
                    byte aByte = final2.bufferA[current];
                    orByte >>= aByte;
                    comparison -= aByte;
                    current = (ushort)final2.bufferB[current];
                }
                else
                {
                    int aNum = (int)((orByte >> 1 & 0x5555U) | (orByte & 0x5555) * 2);
                    int bNum = (int)((aNum >> 2 & 0x3333) | (aNum & 0x3333) << 2);
                    int cNum = (int)((bNum >> 4 & 0xf0f) | (bNum & 0xf0f) << 4);
                    int increment = 10;
                    int another = 10;
                    int reversed = (cNum & 0xff) << 8 | cNum >> 8;
                    if (final2.buffer4[increment - 1] <= reversed)
                    {
                        while (final2.buffer4[increment - 1] <= reversed)
                        {
                            increment++;
                            //buffer4 += 4;
                            another++;
                        }
                        //Console.WriteLine("Something needs to happen here!");
                    }
                    increment = 16 - (increment & 0x1f);
                    reversed >>= (byte)increment;
                    orByte >>= another;
                    comparison -= another;
                    current = (ushort)final2.bufferB[reversed - final2.buffer1[another - 1] + final2.buffer2[another - 1]];
                }
                uint fromTable3 = table3[current];
                uint fromTable4 = table4[current];
                if (fromTable3 != 0)
                {
                    if (comparison < fromTable3)
                    {
                        Manipulate();
                    }
                    comparison -= (int)fromTable3;
                    //long shifted = ((int)orByte >> (int)(fromTable3 & 0xff));
                    int editor = ((int)(1 << (int)(fromTable3 & 0x1f))) - 1;
                    fromTable4 += (uint)(editor & orByte);
                    orByte >>= ((byte)fromTable3 & 0x1f);
                }
                long readPos = pos - (int)fromTable4;
                for (int i = (int)fromTable2; i != 0; i--)
                {
                    byte toCopy = resultantFile[readPos];
                    readPos++;
                    resultantFile[pos] = toCopy;
                    TestWithSample(resultantFile, pos);
                    pos++;
                }
                //string final = Encoding.UTF8.GetString(resultantFile);
                //Console.WriteLine("----------------------");
                //string toOutput = "";
                //for (int i = 0; i < pos; i++)
                //{
                //    if (i == 0 || (i % 16 != 0))
                //    {
                //        toOutput += resultantFile[i].ToString("X") + " ";
                //    }
                //    else
                //    {
                //        Console.WriteLine(toOutput);
                //        toOutput = resultantFile[i].ToString("X") + " ";
                //    }
                //}
            }
        }
    }
}
