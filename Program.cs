using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Diagnostics;

namespace DATExtract
{
    internal class Program
    {
        internal static string version = "1.0.0";
        internal static string extractLocation = "";
        internal static string fileLocation = "";
        internal static string rawFileName = "";

        static void Main(string[] args)
        {
            Compression.CheckOodle();

            ManageConsole.ChangeTitle("Setting up...");

            if (args.Length > 0)
            {
                fileLocation = @"" + args[0];
                
                if (args.Length > 1)
                {
                    extractLocation = args[1];
                }
            }


            if (!File.Exists(fileLocation))
            {
                Console.Write(".DAT archive location: ");
                fileLocation = @"" + Console.ReadLine().Trim('"');
            }

#if DEBUG
            extractLocation = Path.GetDirectoryName(fileLocation);
#endif
            if (!Directory.Exists(extractLocation))
            {
                Console.Write("Where to extract the files to:");
                extractLocation = @"" + Console.ReadLine().Trim('"');
                
                if (extractLocation.Length == 0)
                {
                    Console.WriteLine("No input, extracting to same directory as .DAT file...");

                    extractLocation = Path.GetDirectoryName(fileLocation);
                }
            }

            rawFileName = Path.GetFileName(fileLocation);

            Stopwatch sw = new Stopwatch();

            sw.Start();

            using (var mmf = MemoryMappedFile.CreateFromFile(fileLocation, FileMode.Open, Path.GetFileNameWithoutExtension(fileLocation) + "DATFile"))
            {
                using (var accessor = mmf.CreateViewAccessor(0, 16))
                {
                    DAT.CheckCompressed(accessor);

                    ManageConsole.ChangeTitle("Analysing...");

                    DAT.GetInfo(accessor, mmf);
                }

            }

            sw.Stop();

            ManageConsole.ChangeTitle("Done!");

            Console.WriteLine("Time elapsed extracting: {0} seconds!", sw.ElapsedMilliseconds / 1000);

            Console.ReadLine();

            foreach (string file in Extract.GetFailedFiles())
            {
                Console.WriteLine("Failed to extract: {0}", file);
            }

            Console.ReadLine();
        }
    }
}
