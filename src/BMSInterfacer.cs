using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.Text;

namespace DATExtract;

class BMSInterfacer
{
    static bool quickbmsStarted = false;

    private static Mutex ourMutex;

    private static Mutex bmsMutex;

    private static void CheckMutex()
    {
        if (ourMutex != null) return;

        ourMutex = new Mutex(true, "DATManQuickBMSLock");
    }

    private static void StartQuickBMS()
    {
        if (quickbmsStarted) return;

        quickbmsStarted = true;
        Process cmd = new Process();
        Console.WriteLine("Employing QuickBMS for assistance...");
        string currentLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        cmd.StartInfo.FileName = currentLocation + "\\quickbms\\QuickBMSWrapper.exe";
        cmd.StartInfo.RedirectStandardInput = true;
        cmd.StartInfo.RedirectStandardOutput = true;
        cmd.StartInfo.RedirectStandardError = true;
        cmd.StartInfo.CreateNoWindow = false;
        cmd.StartInfo.UseShellExecute = false;
        cmd.EnableRaisingEvents = true;
        cmd.Exited += QuickBMS_Exited;
        cmd.ErrorDataReceived += QuickBMS_Message;
        cmd.OutputDataReceived += QuickBMS_Message;
        cmd.Start();
        cmd.BeginOutputReadLine();
        cmd.BeginErrorReadLine();
        int checkedCount = 0;
        while (true) {
            if (checkedCount <= 40) {
                if (Mutex.TryOpenExisting("QuickBMSDatManLock", out bmsMutex)) {
                    break;
                }
                Thread.Sleep(250);
                checkedCount++;
            }
            else {
                Console.WriteLine("QuickBMS failed to respond.");
                throw new Exception();
            }
        }
    }

    private static void QuickBMS_Message(object sender, DataReceivedEventArgs e)
    {
        Console.WriteLine(e.Data);
    }

    // When we finish, we abandon the mutex and this makes c# throw an exception and makes the process "out of sync" with us.
    private static void QuickBMS_Exited(object sender, EventArgs e)
    {
        Console.WriteLine("QuickBMS process abandoned. Ignore.");
        quickbmsStarted = false;
        if (Mutex.TryOpenExisting("QuickBMSDatManLock", out bmsMutex)) {
            bmsMutex.Close();
        }
        ourMutex = null;
    }

    // My implementation of Deflate works mostly, but the newer archives have highlighted that it's not perfect - so I fall back to QuickBMS
    internal static int SendToProcess(string sign, byte[] buffer, int compressedSize, byte[] result, int decompressedSize)
    {
        byte[] magicBytes = Encoding.ASCII.GetBytes(sign);

        CheckMutex();
        byte[] toWrite = new byte[buffer.Length + 12];
        toWrite[0] = magicBytes[0]; toWrite[1] = magicBytes[1]; toWrite[2] = magicBytes[2]; toWrite[3] = magicBytes[3];
        toWrite[4] = (byte)((buffer.Length >> 24) & 0xff);
        toWrite[5] = (byte)((buffer.Length >> 16) & 0xff);
        toWrite[6] = (byte)((buffer.Length >> 8) & 0xff);
        toWrite[7] = (byte)((buffer.Length >> 0) & 0xff);
        toWrite[8] = (byte)((decompressedSize >> 24) & 0xff);
        toWrite[9] = (byte)((decompressedSize >> 16) & 0xff);
        toWrite[10] = (byte)((decompressedSize >> 8) & 0xff);
        toWrite[11] = (byte)((decompressedSize >> 0) & 0xff);
        Array.Copy(buffer, 0, toWrite, 12, compressedSize);

        // This is how the IPC works:
        // We obtain a lock, write to the mmf and then release said lock.
        // In the mean time, quickbms created a lock, and after releasing our lock, quickbms will eventually obtain our lock, and release their own
        // We obtain the quickbms lock, and then wait for our own lock to be released.
        //ourMutex.WaitOne();

        // Because memory mapped files are trash
        // Once they're created, they cannot be resized, so I'll just make it the max theoretical size.
        // 29/06/22 - Connor from the future here, looks like Dimensions DATs just throw the "max theoretical size" out the window and don't partition the files...
        using (MemoryMappedFile file = MemoryMappedFile.CreateOrOpen("DATManQuickBMS", 100000 + 8, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.Inheritable)) {
            using (MemoryMappedViewStream stream = file.CreateViewStream()) {
                stream.Write(toWrite, 0, toWrite.Length);
                stream.Flush();
            }

            try {
                ourMutex.ReleaseMutex();
            }
            catch (Exception ex) {
                Console.WriteLine(ex);
            }

            StartQuickBMS();
            bmsMutex.WaitOne();
            ourMutex.WaitOne();

            using (MemoryMappedFile output = MemoryMappedFile.OpenExisting("QuickBMSDATMan")) {
                bmsMutex.ReleaseMutex();
                using (MemoryMappedViewStream outputStream = output.CreateViewStream()) {
                    outputStream.Read(result, 0, (int)decompressedSize);
                }
            }
        }

        return decompressedSize;
    }
}
