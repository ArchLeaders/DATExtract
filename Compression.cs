using Oodle.NET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Runtime.InteropServices;
//using RC4Cryptography;

namespace DATExtract
{
    internal static class Compression
    {
        internal static OodleCompressor oodle;

        internal static void CheckOodle()
        {
            try
            {
                string dllLocation = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "oo2core_8_win64.dll");
                oodle = new OodleCompressor(dllLocation);
            }
            catch (DllNotFoundException ex)
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
            if (oodle == null)
            {
                // I would have liked to write in colour here to be a lot more bold to the user, but I think it would just be too slow...
                Console.WriteLine("Warning: Could not extract file '{0}' as oo2core_8_win64.dll is missing.", filename);
                return null;
            }

            unsafe
            {
                var result = oodle.DecompressBuffer(buffer, buffer.Length, decompressed, (int)decompressedSize, OodleLZ_FuzzSafe.No, OodleLZ_CheckCRC.No, OodleLZ_Verbosity.Max, 0L, 0L, 0L, 0L, 0L, 3L, OodleLZ_Decode_ThreadPhase.Unthreaded);
                if (result == 0)
                {
                    Console.WriteLine("Critical: Oodle could not decompress file '{0}'!", filename);
                    return null;
                }
            }

            return decompressed;
        }

        internal static bool newa = false;

        internal static byte[] Deflate(byte[] buffer, uint decompressedSize)
        {
            byte[] decompressed = new byte[decompressedSize];

            int result = DeflateAlgorithm.Deflate(buffer, decompressed, decompressedSize);
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
            string location = Path.Join(Program.extractLocation, filename);

            Directory.CreateDirectory(Path.GetDirectoryName(location));

            File.WriteAllBytes(location, buffer);

            Console.WriteLine("Extracted {0}", filename);

            totalExtracted++;
        }

    }
}
