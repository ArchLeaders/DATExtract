using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATLib
{
    internal static class ManageConsole
    {
        public static void ChangeTitle(string title)
        {
#if DEBUG
            string termination = "";
            if (Compression.oodle == null)
            {
                termination = " - OODLE DLL MISSING!"; // Should help when people provide screenshots when something's not working.
            }
            if (Program.rawFileName != "")
            {
                Console.Title = "DATExtract.exe - " + Program.version + " - " + title + $" [{Program.rawFileName}] " + termination;
            }
            else
            {
                Console.Title = "DATExtract.exe - " + Program.version + " - " + title + termination;

            }
#endif
        }
    }
}
