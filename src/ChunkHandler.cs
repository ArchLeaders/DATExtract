using DATExtract.Decompressors;
using System.Runtime.InteropServices;

namespace DATExtract;

internal static class ChunkHandler
{
    //[DllImport(@"RefPack.dll")]
    //private static extern int rfpk_decompress(byte[] input, int inputSize, byte[] output, int outputSize);

    [DllImport(@"oo2core_8_win64.dll")]
    private static extern int OodleLZ_Decompress(byte[] buffer, long bufferSize, byte[] outputBuffer, long outputBufferSize,
        uint a, uint b, ulong c, uint d, uint e, uint f, uint g, uint h, uint i, uint threadModule);

    static bool oodleExists = false;
    static bool alreadyChecked = false;
    public static bool CheckOodleExists()
    {
        if (alreadyChecked) {
            return oodleExists;
        }

        alreadyChecked = true;
        oodleExists = File.Exists(@"oo2core_8_win64.dll");
        return oodleExists;
    }

    public static int ExtractDFLT(byte[] chunk, int compressedSize, byte[] decompressed, int decompressedSize)
    {
        if (compressedSize == decompressedSize) { // Deflate_v1.0
            Array.Copy(chunk, decompressed, compressedSize);
            return compressedSize;
        }
        else {
            return BMSInterfacer.SendToProcess("DFLT", chunk, compressedSize, decompressed, decompressedSize);
        }
    }

    public static int ExtractLZ2K(byte[] chunk, int compressedSize, byte[] decompressed, int decompressedSize)
    {
        return BMSInterfacer.SendToProcess("LZ2K", chunk, compressedSize, decompressed, decompressedSize);
    }

    public static int ExtractRFPK(byte[] chunk, int compressedSize, byte[] decompressed, int decompressedSize)
    {
        return BMSInterfacer.SendToProcess("RFPK", chunk, compressedSize, decompressed, decompressedSize);
    }

    public static int ExtractOODL(byte[] chunk, int compressedSize, byte[] decompressed, int decompressedSize)
    {
        if (CheckOodleExists()) {
            return OodleLZ_Decompress(chunk, compressedSize, decompressed, decompressedSize, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3);
        }

        return 0;
    }

    public static int ExtractZIPX(byte[] chunk, int compressedSize, byte[] decompressed, int decompressedSize)
    {
        RC4.Apply(chunk, BitConverter.GetBytes(compressedSize), compressedSize, decompressed);
        return decompressedSize;
    }

    public static int ExtractRNC(byte[] chunk, int compressedSize, byte[] decompressed, int decompressedSize)
    {
        RNC.Unpack(chunk, decompressed, compressedSize, decompressedSize);
        return decompressedSize;
    }
}
