using System.Text;

namespace CathermanCore;

public static class Crc32Extensions
{
    private static readonly uint[] Table;

    static Crc32Extensions()
    {
        uint poly = 0xEDB88320;
        Table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint r = i;
            for (int j = 0; j < 8; j++)
            {
                r = (r & 1) != 0 ? (r >> 1) ^ poly : r >> 1;
            }
            Table[i] = r;
        }
    }

    public static int ToCrc32Int(this ReadOnlySpan<byte> source)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte b in source)
        {
            crc = Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }
        return (int)~crc;
    }

    public static int ToCrc32Int(this byte[] source)
    {
        return ToCrc32Int((ReadOnlySpan<byte>)source);
    }

    public static int ToCrc32Int(this string source)
    {
        var byteCount = Encoding.UTF8.GetByteCount(source);
        Span<byte> bytes = byteCount <= 256 ? stackalloc byte[byteCount] : new byte[byteCount];
        Encoding.UTF8.GetBytes(source, bytes);
        return ToCrc32Int(bytes);
    }
}