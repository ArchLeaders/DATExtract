namespace DATExtract;

internal static class FileHandler
{
    internal static string lastUsedLocation = "";

    internal static List<string> failedFiles = new List<string>();

    internal static void AddFailedFile(string filename)
    {
        Console.WriteLine("Failed to extract file: {0}", filename);
        failedFiles.Add(filename);
    }

    static Dictionary<string, bool> createdDirectories = new();

    internal static int written = 0;
    internal static void WriteFile(string extractLocation, string filename, byte[] fileData, int fileSize)
    {
        if (filename[0] == '\\') filename = filename.Substring(1);
        string parentDirectory = Path.GetDirectoryName(filename);
        filename = filename.ToUpper();
        string location = Path.Combine(extractLocation, filename);
        if (!createdDirectories.ContainsKey(parentDirectory)) {
            createdDirectories[parentDirectory] = true;
            Directory.CreateDirectory(Path.GetDirectoryName(location));
        }
        FileStream file = File.Create(location);
        file.Write(fileData, 0, fileSize); // Write async seems to cause some problems.
        file.DisposeAsync();
        written++;
    }

    internal static int correct = 0;
    internal static void CompareFile(string filename, byte[] fileData, int amount)
    {
        string storage = @"A:\SteamLibrary\steamapps\common\LEGO Star Wars - The Skywalker Saga\";
        string diskLocation = Path.Combine(storage, filename.Substring(1));

        byte[] diskBuffer = File.ReadAllBytes(diskLocation);
        for (int i = 0; i < amount; i++) {
            if (diskBuffer[i] != fileData[i]) {
                string path = @"A:\failedfiles\" + filename.Substring(1);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, fileData);
                Console.WriteLine("failed file: " + filename);
                return;
            }
            i++;
            i++;
        }

        correct++;
        //Console.WriteLine("all good for " + filename);
    }

    public static void Reset(string extractLocation)
    {
        written = 0;
        failedFiles.Clear();
        if (lastUsedLocation != extractLocation) {
            lastUsedLocation = extractLocation;
            createdDirectories.Clear();
        }
    }
}
