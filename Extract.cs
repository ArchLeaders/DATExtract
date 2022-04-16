using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATExtract
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
    }
}
