using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATExtract
{
    /// <summary>
    /// Entry class that wraps the DAT functions nicely.
    /// </summary>
    public static class DATExtract
    {
        internal static string fileLocation = "";

        public static void SetDAT(string datFile)
        {
            fileLocation = datFile;
            DAT.mmf = MemoryMappedFile.CreateFromFile(datFile, FileMode.Open, Path.GetFileNameWithoutExtension(datFile) + "DATFile");
        }

        public static FileInfo[] GetFiles()
        {
            if (fileLocation == null) throw new Exception("Missing .dat file!");

            if (DAT.files == null)
            {
                DAT.GetInfo();
            }

            return DAT.files;
        }

        public static void ExtractFile(FileInfo file)
        {
            DAT.ExtractFile(file);
        }

        public static void ExtractFiles(FileInfo[] files)
        {
            foreach (FileInfo file in files)
            {
                DAT.ExtractFile(file);
            }
        }

        public static void ExtractAll()
        {
            foreach (FileInfo file in DAT.files)
            {
                DAT.ExtractFile(file);
            }
        }
    }
}
