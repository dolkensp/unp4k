namespace System.IO;

public static class IOExtensions
{
    public static string ReadPString(this BinaryReader binaryReader, StringSizeEnum byteLength = StringSizeEnum.Int32)
    {
        var stringLength = byteLength switch
        {
            StringSizeEnum.Int8 => binaryReader.ReadByte(),
            StringSizeEnum.Int16 => binaryReader.ReadInt16(),
            StringSizeEnum.Int32 => binaryReader.ReadInt32(),
            _ => throw new NotSupportedException("Only Int8, Int16, and int string sizes are supported"),
        };
        // If there is actually a string to read
        if (stringLength > 0) return new string(binaryReader.ReadChars(stringLength));
        return null;
    }

    public static string ReadCString(this BinaryReader binaryReader)
    {
        int stringLength = 0;
        while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length && binaryReader.ReadChar() != 0) stringLength++;
        long nul = binaryReader.BaseStream.Position;
        binaryReader.BaseStream.Seek(0 - stringLength - 1, SeekOrigin.Current);
        char[] chars = binaryReader.ReadChars(stringLength + 1);
        binaryReader.BaseStream.Seek(nul, SeekOrigin.Begin);
        // Why is this necessary?
        if (stringLength > chars.Length) stringLength = chars.Length;
        // If there is actually a string to read
        if (stringLength > 0) return new string(chars, 0, stringLength).Replace("\u0000", "");
        return null;
    }

    public static string ReadFString(this BinaryReader binaryReader, int stringLength)
    {
        char[] chars = binaryReader.ReadChars(stringLength);
        for (int i = 0; i < stringLength; i++) if (chars[i] == 0) return new string(chars, 0, i);
        return new string(chars);
    }

    public static byte[] ReadAllBytes(this Stream stream)
    {
        using MemoryStream ms = new();
        long oldPosition = stream.Position;
        stream.Position = 0;
        stream.CopyTo(ms);
        stream.Position = oldPosition;
        return ms.ToArray();
    }

    public static Guid? ReadGuid(this BinaryReader reader, bool nullable = true)
    {
        bool isNull = nullable && reader.ReadInt32() == -1;
        short c = reader.ReadInt16();
        short b = reader.ReadInt16();
        int a = reader.ReadInt32();
        byte k = reader.ReadByte();
        byte j = reader.ReadByte();
        byte i = reader.ReadByte();
        byte h = reader.ReadByte();
        byte g = reader.ReadByte();
        byte f = reader.ReadByte();
        byte e = reader.ReadByte();
        byte d = reader.ReadByte();
        if (isNull) return null;
        return new Guid(a, b, c, d, e, f, g, h, i, j, k);
    }
}

public enum StringSizeEnum
{
    Int8 = 1,
    Int16 = 2,
    Int32 = 4,
}
