using ModLib;

namespace DATExtract;

public partial class DATFile
{
    private ModFile hdrBlock;
    private ModFile fullFile;

    public string patchFormat { get; private set; }

    public bool verboseOutput = false; // Should output every extract message to console (very slow)
    public string fileLocation { get; private set; }

    public long hdrOffset { get; private set; }

    public uint nameInfoOffset { get; private set; }

    public uint hdrSize { get; private set; }

    public int archiveId { get; private set; }

    public int version { get; private set; }

    public uint fileCount { get; private set; }

    public uint namesOffset { get; private set; }

    public uint nameSignOffset { get; private set; }

    public bool is64 { get; private set; }

    public CompFile[] files { get; private set; }

    // We re-use these arrays as the GC doesn't collect them quickly enough otherwise. 
    private byte[] decompressed = new byte[32768];
    private byte[] totalFile = new byte[90000000];
    private byte[] currentChunk = new byte[32768];

    private void Expand(int compressedSize, int decompressedSize)
    {
        if (decompressed.Length < decompressedSize) // Expand if necessary
        {
            decompressed = new byte[decompressedSize];
        }

        if (currentChunk.Length < compressedSize) {
            currentChunk = new byte[compressedSize];
        }
    }

    private void Extract(string extractLocation, CompFile file)
    {
        int offset = 0;
        int progress = 0;
        fullFile.Seek(file.offset, SeekOrigin.Begin);

        if (file.size == file.zsize && file.packed == 0) {
            if (totalFile.Length < file.size) // Expand if necessary
            {
                totalFile = new byte[file.size];
            }
            fullFile.ReadInto(totalFile, (int)file.size);
            FileHandler.WriteFile(extractLocation, file.path, totalFile, (int)file.size);
            return;
        }

        if (file.size > totalFile.Length) {
            totalFile = new byte[file.size];
        }

        while (offset < file.zsize) {
            string cc4 = fullFile.ReadString(4);
            int compressedSize = fullFile.ReadInt();
            int decompressedSize = fullFile.ReadInt();

            int successAmount = 0;

            if (compressedSize > 0) { // stupid rnc
                if (cc4 != "LZ2K") {
                    Expand(compressedSize, decompressedSize);

                    fullFile.ReadInto(currentChunk, compressedSize);
                }
            }
            else {
                Expand((int)file.zsize, ReverseEndian.Int(compressedSize));
            }

            if (cc4 == "OODL") {
                successAmount = ChunkHandler.ExtractOODL(currentChunk, compressedSize, decompressed, decompressedSize);
            }
            else if (cc4 == "ZIPX") {
                successAmount = ChunkHandler.ExtractZIPX(currentChunk, compressedSize, decompressed, decompressedSize);
            }
            else if (cc4 == "RFPK") {

                if (compressedSize == decompressedSize) {
                    successAmount = compressedSize;
                    Array.Copy(currentChunk, decompressed, compressedSize);
                }
                else { // Not sure if this applies for every type of com-method, but it definitely applies here.
                    successAmount = ChunkHandler.ExtractRFPK(currentChunk, compressedSize, decompressed, decompressedSize);
                }
            }
            else if (cc4 == "DFLT") {
                successAmount = ChunkHandler.ExtractDFLT(currentChunk, compressedSize, decompressed, decompressedSize);
            }
            else if (cc4 == "LZ2K") {
                int temp = decompressedSize;
                decompressedSize = compressedSize;
                compressedSize = temp;

                Expand(compressedSize, decompressedSize);
                fullFile.Seek(file.offset + offset + 12, SeekOrigin.Begin);
                fullFile.ReadInto(currentChunk, (int)compressedSize);

                successAmount = ChunkHandler.ExtractLZ2K(currentChunk, compressedSize, decompressed, decompressedSize);
            }
            else if (cc4.Substring(0, 3) == "RNC") {
                int temp = ReverseEndian.Int(decompressedSize);
                decompressedSize = ReverseEndian.Int(compressedSize);
                compressedSize = (int)file.zsize;

                Expand(compressedSize, decompressedSize);
                fullFile.Seek(file.offset, SeekOrigin.Begin);
                fullFile.ReadInto(currentChunk, (int)file.zsize);

                successAmount = ChunkHandler.ExtractRNC(currentChunk, compressedSize, decompressed, decompressedSize);
            }
            else {
                Console.WriteLine("Compression format not yet implemented ({0})", cc4);
            }

            if (decompressed.Length == 0 || successAmount != decompressedSize) {
                FileHandler.AddFailedFile(file.path);
                return;
            }

            Array.Copy(decompressed, 0, totalFile, progress, decompressedSize);
            progress += decompressedSize;

            offset += 12 + compressedSize;
        }

        FileHandler.WriteFile(extractLocation, file.path, totalFile, (int)file.size);
    }
}

public struct CompFile
{
    public long crc;
    public string path;
    public long offset;
    public uint zsize;
    public uint size;
    public long packed;
}