using ModLib;

namespace DATExtract;

public partial class DATFile
{
    private static long CRC_FNV_OFFSET_32 = 0x811c9dc5;

    private static long CRC_FNV_PRIME_32 = 0x199933;

    private static long CRC_FNV_OFFSET_64 = -3750763034362895579;

    private static long CRC_FNV_PRIME_64 = 1099511628211;

    public static DATFile Open(string location)
    {
        DATFile dat = new DATFile();
        dat.fileLocation = location;

        using (ModFile file = ModFile.Open(location)) {
            uint hdrOffset = file.ReadUint();
            if ((hdrOffset & 0x80000000) != 0) {
                hdrOffset ^= 0xffffffff;
                hdrOffset <<= 8;
                hdrOffset += 0x100;
            }
            dat.hdrOffset = hdrOffset;

            if (file.fileStream.Length > uint.MaxValue) {
                Console.WriteLine("Archive is very large, sliding header offset.");
                long newHdrOffset = 0x0100000000;
                newHdrOffset ^= hdrOffset;
                dat.hdrOffset = newHdrOffset;
            }

            uint hdrSize = file.ReadUint();
            dat.hdrSize = hdrSize;

            file.Seek(16, SeekOrigin.Begin);
            dat.patchFormat = file.ReadString(5);

            using (ModFile hdrBlock = file.LoadSegment(dat.hdrOffset, (int)hdrSize)) {
                dat.hdrBlock = hdrBlock;

                int firstValue = hdrBlock.ReadInt();
                uint secondValue = hdrBlock.ReadUint();

                if (secondValue != 0x3443432e && secondValue != 0x2e434334 && firstValue != 0x3443432e && firstValue != 0x2e434334) {
                    dat.archiveId = firstValue;
                    dat.fileCount = secondValue;

                    dat.LoadAsOld();
                }
                else {
                    dat.LoadAsNew();
                }
            }

        }

        return dat;
    }
}
