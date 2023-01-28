using ModLib;
using System.ComponentModel;

namespace DATExtract;

public partial class DATFile
{
    public int handled;
    public int extractAmount;
    public int successfullyExtracted;

    public List<string> failedFiles => FileHandler.failedFiles;

    private void Reset(int number, string extractLocation)
    {
        successfullyExtracted = 0;
        handled = 0;
        extractAmount = number;

        FileHandler.Reset(extractLocation);
    }

    public void ExtractFile(CompFile file, string extractLocation)
    {
        Reset(1, extractLocation);

        using (ModFile datFile = ModFile.Open(fileLocation, false)) {
            fullFile = datFile;

            Extract(extractLocation, file);

            handled++;
        }
        successfullyExtracted = FileHandler.written;
    }

    int lastOutput = 0;
    public void ExtractAll(string extractLocation, BackgroundWorker worker = null)
    {
        Handle(files, extractLocation, worker);
    }

    public void ExtractCollection(CompFile[] files, string extractLocation, BackgroundWorker worker = null)
    {
        Handle(files, extractLocation, worker);
    }

    private void Handle(CompFile[] files, string extractLocation, BackgroundWorker worker)
    {
        Reset(files.Length, extractLocation);

        bool shouldCache = files.Length > 5000;

        using (ModFile datFile = ModFile.Open(fileLocation, shouldCache)) {
            fullFile = datFile;

            foreach (CompFile file in files) {
                int current = (handled * 100) / files.Length;
                if (worker != null) {
                    worker.ReportProgress(current, file.path);
                }

                Extract(extractLocation, file);
                handled++;
                if (!verboseOutput) {
                    if (current >= lastOutput + 5) {
                        Console.WriteLine("Progress: " + current + "%");
                        lastOutput = current;
                    }
                }
                if (worker != null && worker.CancellationPending) {
                    Console.WriteLine("User requested cancellation.");
                    successfullyExtracted = FileHandler.written;
                    return;
                }
            }
        }

        successfullyExtracted = FileHandler.written;
    }
}
