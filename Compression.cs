using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Reflection;
//using RC4Cryptography;

namespace DATLib
{
    internal static class Compression
    {
        //internal static OodleCompressor oodle;

        internal static bool oodleExists = false;
        private static bool checkedOodle = false;

        internal static void CheckOodle()
        {
            if (checkedOodle) return;

            checkedOodle = true;
            
            string dllLocation = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "oo2core_8_win64.dll");
            if (File.Exists(dllLocation))
            {
                oodleExists = true;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("Warning: ");
                Console.ResetColor();
                Console.WriteLine("Could not find oo2core_8_win64.dll, it is suggested that you provide it as most TSS archives heavily rely on them...");
            }
        }

        internal static byte[] ExtractOodle(byte[] buffer, uint decompressedSize, string filename)
        {
            byte[] decompressed = new byte[decompressedSize];
            if (!oodleExists)
            {
                // I would have liked to write in colour here to be a lot more bold to the user, but I think it would just be too slow...
                Console.WriteLine("Warning: Could not extract file '{0}' as oo2core_8_win64.dll is missing.", filename);
                return null;
            }

            Oodle.Decompress(buffer, buffer.Length, decompressed, (int)decompressedSize);

            //unsafe
            //{
            //    var result = oodle.DecompressBuffer(buffer, buffer.Length, decompressed, (int)decompressedSize, OodleLZ_FuzzSafe.No, OodleLZ_CheckCRC.No, OodleLZ_Verbosity.Max, 0L, 0L, 0L, 0L, 0L, 3L, OodleLZ_Decode_ThreadPhase.Unthreaded);
            //    if (result == 0)
            //    {
            //        Console.WriteLine("Critical: Oodle could not decompress file '{0}'!", filename);
            //        return null;
            //    }
            //}

            return decompressed;
        }

        internal static bool newa = false;

        // My implementation of Deflate works mostly, but the newer archives have highlighted that it's not perfect - so I fall back to QuickBMS
        private static byte[] CheatDeflate(uint decompressedSize, byte[] buffer)
        {
            byte[] toWrite = new byte[buffer.Length + 4];
            toWrite[0] = (byte)((decompressedSize >> 24) & 0xff);
            toWrite[1] = (byte)((decompressedSize >> 16) & 0xff);
            toWrite[2] = (byte)((decompressedSize >> 8) & 0xff);
            toWrite[3] = (byte)((decompressedSize >> 0) & 0xff);
            Array.Copy(buffer, 0, toWrite, 4, buffer.Length);
            string currentLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            File.WriteAllBytes("deflatetest.dat", toWrite);
            //Console.WriteLine(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Process cmd = new Process();
            Console.WriteLine("Calling: " + currentLocation + "\\quickbms\\QuickBMSWrapper.exe");
            cmd.StartInfo.FileName = currentLocation + "\\quickbms\\QuickBMSWrapper.exe";
            cmd.StartInfo.Arguments = "deflatetest.dat";
            cmd.StartInfo.WorkingDirectory = currentLocation;
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = false;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();
            cmd.WaitForExit();
            byte[] fileData = File.ReadAllBytes(currentLocation + "/output.data");
            File.Delete(currentLocation + "/output.data");
            return fileData;
        }

        internal static byte[] Deflate(byte[] buffer, uint decompressedSize)
        {
            byte[] decompressed = new byte[decompressedSize];


            int result = DeflateAlgorithm.Deflate(buffer, decompressed, decompressedSize);
            if (result == 0)
            {
                decompressed = CheatDeflate(decompressedSize, buffer);
            }
            DeflateAlgorithm.previousProgress += decompressed.Length;

            return decompressed;
        }

        internal static byte[] ExtractZIPX(byte[] buffer, byte[] key, uint decompressedSize, string filename)
        {
            return RC4.Apply(buffer, key); // Seems to work...
        }

        //internal static byte[] ExtractLZ2K(byte[] buffer, uint decompressedSize, string filename)
        //{
        //    LzwInputStream()
        //}

        internal static int totalExtracted = 0;

        internal static void WriteFile(string filename, byte[] buffer)
        {
            filename = filename.ToUpper();

            string location = Path.Combine(DATExtract.extractLocation, filename.Substring(1));

            if (location.Length > 247) // Max path size on Windows
            {
                Console.WriteLine("Failed to write file in desired directory due to path limitations!");
                location = Path.Combine(DATExtract.extractLocation, @"shortened\", Path.GetFileName(filename));
                Extract.AddTruncatedFile(filename);

                if (location.Length > 247)
                {
                    throw new Exception("Bad file location! Choose a location with a shorter path.");
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(location));

            File.WriteAllBytes(location, buffer);

            Console.WriteLine("Extracted {0}", filename);

            totalExtracted++;
        }

        internal static void ExtractRNC(byte[] buffer, byte[] decompressed)
        {
            //RncStatus result = Rnc.ReadRnc(buffer, decompressed);
            //Console.WriteLine(result);
        }
    }
}
