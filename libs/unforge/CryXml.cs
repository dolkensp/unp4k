namespace unforge;

internal class CryXmlNode
{
    internal int NodeID { get; set; }
    internal int NodeNameOffset { get; set; }
    internal int ContentOffset { get; set; }
    internal short AttributeCount { get; set; }
    internal short ChildCount { get; set; }
    internal int ParentNodeID { get; set; }
    internal int FirstAttributeIndex { get; set; }
    internal int FirstChildIndex { get; set; }
    internal int Reserved { get; set; }
}

internal class CryXmlReference
{
    internal int NameOffset { get; set; }
    internal int ValueOffset { get; set; }
}

public enum ByteOrderEnum
{
    AutoDetect,
    BigEndian,
    LittleEndian
}

internal static class CryXmlSerializer
{
    internal static long ReadInt64(this BinaryReader br, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
    {
        List<byte> bytes = new() { br.ReadByte(), br.ReadByte(), br.ReadByte(), br.ReadByte(), br.ReadByte(), br.ReadByte(), br.ReadByte(), br.ReadByte() };
        if (byteOrder == ByteOrderEnum.LittleEndian) bytes.Reverse();
        return BitConverter.ToInt64(bytes.ToArray(), 0);
    }

    internal static int ReadInt32(this BinaryReader br, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
    {
        List<byte> bytes = new() { br.ReadByte(), br.ReadByte(), br.ReadByte(), br.ReadByte() };
        if (byteOrder == ByteOrderEnum.LittleEndian) bytes.Reverse();
        return BitConverter.ToInt32(bytes.ToArray(), 0);
    }

    internal static short ReadInt16(this BinaryReader br, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
    {
        List<byte> bytes = new() { br.ReadByte(), br.ReadByte() };
        if (byteOrder == ByteOrderEnum.LittleEndian) bytes.Reverse();
        return BitConverter.ToInt16(bytes.ToArray(), 0);
    }

    internal static ulong ReadUInt64(this BinaryReader br, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
    {
        List<byte> bytes = new() { br.ReadByte(), br.ReadByte(), br.ReadByte(), br.ReadByte(), br.ReadByte(), br.ReadByte(), br.ReadByte(), br.ReadByte() };
        if (byteOrder == ByteOrderEnum.LittleEndian) bytes.Reverse();
        return BitConverter.ToUInt64(bytes.ToArray(), 0);
    }

    internal static uint ReadUInt32(this BinaryReader br, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
    {
        List<byte> bytes = new() { br.ReadByte(), br.ReadByte(), br.ReadByte(), br.ReadByte() };
        if (byteOrder == ByteOrderEnum.LittleEndian) bytes.Reverse();
        return BitConverter.ToUInt32(bytes.ToArray(), 0);
    }

    internal static ushort ReadUInt16(this BinaryReader br, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
    {
        List<byte> bytes = new() { br.ReadByte(), br.ReadByte() };
        if (byteOrder == ByteOrderEnum.LittleEndian) bytes.Reverse();
        return BitConverter.ToUInt16(bytes.ToArray(), 0);
    }
}

internal class CryXmlValue
{
    internal int Offset { get; set; }
    internal string Value { get; set; }
}