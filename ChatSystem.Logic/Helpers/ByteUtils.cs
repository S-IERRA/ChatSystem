using System.IO.Compression;
using System.Text;

namespace ChatSystem.Logic.Helpers;

public class ByteUtils
{
    //[LENGTH][DATA]
    private static byte[] Int2Byte(int number)
    {
        byte[] bytes = new byte[2];
        bytes[0] = (byte)(number & 0xFF);
        bytes[1] = (byte)((number >> 8) & 0xFF);
        return bytes;
    }

    private static byte[] UInt2Byte(uint number)
    {
        byte[] bytes = new byte[2];
        bytes[0] = (byte)(number & 0xFF);
        bytes[1] = (byte)((number >> 8) & 0xFF);
        return bytes;
    }

    public static int Byte2Int(byte[] bytes, int offset = 0)
    {
        return (bytes[offset + 0] | (bytes[offset + 1] << 8));
    }

    public static uint Byte2UInt(byte[] bytes, int offset = 0)
    {
        return (uint)(bytes[offset + 0] | (bytes[offset + 1] << 8));
    }

    public static byte[] Compress(string data)
    {
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);
        byte[] length = Int2Byte(data.Length);

        byte[] prepended = length.Concat(dataBytes).ToArray();

        /*using MemoryStream ms = new();
        using (GZipStream zip = new(ms, CompressionMode.Compress, true))
        {
            zip.Write(prepended, 0, prepended.Length);
        }*/

        return prepended; //ms.ToArray();
    }

    public static async Task<byte[]> Decompress(byte[] inBytes)
    {
        /*using MemoryStream inStream = new(inBytes);
        await using GZipStream zip = new(inStream, CompressionMode.Decompress);
        using MemoryStream outStream = new();

        Memory<byte> buffer = new(new byte[4096]);
        
        int totalRead = 0, bytesRead;
        while ((bytesRead = zip.Read(buffer, totalRead, buffer.Length - totalRead)) > 0)
        {
            totalRead += bytesRead;
        }
        
        return outStream.ToArray();*/

        return inBytes;
    }
}