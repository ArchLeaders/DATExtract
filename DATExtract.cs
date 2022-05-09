using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace DATLib
{
    public struct ExtractResult
    {
        public int extracted;
        public int total;
    }

    /// <summary>
    /// Entry class that wraps the DAT functions nicely.
    /// </summary>
    public static class DATExtract
    {
        internal static string fileLocation = "";
        internal static string extractLocation = "";
        internal static ExtractResult lastResult = new ExtractResult();

        public static void SetDAT(string datFile)
        {
            Compression.CheckOodle();

            fileLocation = datFile;
            if (AccessDAT.mmf != null)
            {
                AccessDAT.mmf.Dispose(); // Frees the file
            }

            AccessDAT.mmf = MemoryMappedFile.CreateFromFile(datFile, FileMode.Open, Path.GetFileNameWithoutExtension(datFile) + "DATFile");
            AccessDAT.files = null;

            if (extractLocation == "")
            {
                extractLocation = Path.GetDirectoryName(datFile);
            }
        }

        public static void ReleaseDAT()
        {
            if (AccessDAT.mmf != null)
            {
                AccessDAT.mmf.Dispose();
            }
        }

        public static void SetExtractLocation(string location)
        {
            extractLocation = location;
        }

        public static FileInfo[] GetFiles()
        {
            if (fileLocation == null) throw new Exception("Missing .dat file!");

            if (AccessDAT.files == null)
            {
                AccessDAT.GetInfo();
            }

            return AccessDAT.files;
        }

        private static void UpdateResult(int fileCount)
        {
            lastResult.extracted = Compression.totalExtracted;
            lastResult.total = fileCount;
        }

        private static void Handler(FileInfo[] files, BackgroundWorker worker)
        {
            if (files == null || files.Length == 0) { return; }

            Compression.totalExtracted = 0;
            int filesCompleted = 0;
            foreach (FileInfo file in files)
            {
                Console.WriteLine("Currently extracting: " + file.path);
                worker.ReportProgress((int)((float)filesCompleted / files.Length * 100), file.path);
                AccessDAT.ExtractFile(file);
                filesCompleted++;
                if (worker != null)
                {
                    if (worker.CancellationPending)
                    {
                        Console.WriteLine("User requested cancellation.");
                        UpdateResult(files.Length);
                        return;
                    }
                }

            }
            Extract.WriteFile();
            Extract.ResetLists();
            worker.ReportProgress(100, "Done.");
            UpdateResult(files.Length);
        }

        public static void ExtractFile(FileInfo file) // Only one file, so progress cannot be reported.
        {
            Compression.totalExtracted = 0;
            AccessDAT.ExtractFile(file);
            Extract.WriteFile();
            Extract.ResetLists();
            UpdateResult(1);
        }

        public static void ExtractFiles(FileInfo[] files, BackgroundWorker worker = null)
        {
            Handler(files, worker);
        }

        public static void ExtractAll(BackgroundWorker worker = null)
        {
            Handler(AccessDAT.files, worker);
        }

        public static ExtractResult GetResult()
        {
            return lastResult;
        }

        public static string[] GetFailedFiles()
        {
            return Extract.GetFailedFiles().ToArray();
        }
    }
}
