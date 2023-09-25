using System.IO.Compression;
using System.Text;

namespace ChatSystem.Logic.Helpers;

public class ByteUtils
{
    //[ID][LENGTH][DATA]
    private static byte[] Int2Byte(int number)
    {
        byte[] bytes = new byte[3];
        bytes[0] = (byte)(number & 0xFF);
        bytes[1] = (byte)((number >> 8) & 0xFF);
        return bytes;
    }

    private static byte[] UInt2Byte(uint number)
    {
        byte[] bytes = new byte[4];
        bytes[0] = (byte)(number & 0xFF);
        bytes[1] = (byte)((number >> 8) & 0xFF);
        return bytes;
    }

    public static int Byte2Int(byte[] bytes, int offset = 0)
    {
        return (bytes[offset + 0] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16));
    }

    public static uint Byte2UInt(byte[] bytes, int offset = 0)
    {
        return (uint)(bytes[offset + 0] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16));
    }

    public static byte[] Compress(string data)
    {
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);
        byte[] length = Int2Byte(data.Length);

        byte[] prepended = length.Concat(dataBytes).ToArray();

        using MemoryStream ms = new();
        using (GZipStream zip = new(ms, CompressionMode.Compress, true))
        {
            zip.Write(prepended, 0, prepended.Length);
        }

        return ms.ToArray();
    }

    public static async Task<byte[]> Decompress(byte[] inBytes)
    {
        using MemoryStream inStream = new(inBytes);
        await using GZipStream zip = new(inStream, CompressionMode.Decompress);
        using MemoryStream outStream = new();

        Memory<byte> buffer = new(new byte[4096]);
        int read;
        
        while ((read = await zip.ReadAsync(buffer)) > 0)
            await outStream.WriteAsync(buffer[..read]);
        
        return outStream.ToArray();
    }
}