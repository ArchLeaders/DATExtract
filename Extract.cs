using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DATLib
{
    internal static class Extract
    {
        private static List<string> failedFiles = new List<string>();

        public static void AddFailedFile(string filename)
        {
            failedFiles.Add(filename);
        }

        public static List<string> GetFailedFiles()
        {
            return failedFiles;
        }

        private static List<string> truncatedFiles = new List<string>();

        public static void AddTruncatedFile(string file)
        {
            truncatedFiles.Add(file);
        }

        public static void WriteFile()
        {
            if (truncatedFiles.Count > 0)
            {
                string fileData = "This documents contains a list of files that could not be placed in their default locations due to path length constraints.\n\n\n\n";
                foreach (string file in truncatedFiles)
                {
                    fileData += file + " -> " + "shortened/" + Path.GetFileName(file) + "\n";
                }
                File.WriteAllText(Path.Combine(DATExtract.extractLocation, "shortened/", "README.TXT"), fileData);
            }
        }

        public static void ResetLists()
        {
            failedFiles = new List<string>();
            truncatedFiles = new List<string>();
        }
    }
}
