using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Threading.Tasks;

namespace unforge;
public static class DataForge
{
    public static async Task SerialiseData(FileInfo inFile, FileInfo outFile, ByteOrderEnum byteOrder = ByteOrderEnum.AutoDetect)
    {
        using BinaryReader br = new(inFile.Open(FileMode.Open, FileAccess.Read, FileShare.None));
        char peek = br.ReadChar();
        if (peek is '<') return; // File is already XML
        else if (peek != 'C') return; // Unknown file format

        string header = br.ReadFString(7);
        if (header is "CryXml" || header is "CryXmlB") br.ReadCString();
        else return; // Unknown file format

        long headerLength = br.BaseStream.Position;
        int fileLength = br.ReadInt32(byteOrder = ByteOrderEnum.BigEndian);

        if (fileLength != br.BaseStream.Length)
        {
            br.BaseStream.Seek(headerLength, SeekOrigin.Begin);
            byteOrder = ByteOrderEnum.LittleEndian;
            br.ReadInt32(byteOrder); // Offset - Apparently reads fileLength
        }

        int nodeTableOffset = br.ReadInt32(byteOrder);
        int nodeTableCount = br.ReadInt32(byteOrder);
        int nodeTableSize = 28;

        int attributeTableOffset = br.ReadInt32(byteOrder);
        int attributeTableCount = br.ReadInt32(byteOrder);
        int referenceTableSize = 8;

        int childTableOffset = br.ReadInt32(byteOrder);
        int childTableCount = br.ReadInt32(byteOrder);
        int length3 = 4;

        int stringTableOffset = br.ReadInt32(byteOrder);
        br.ReadInt32(byteOrder); // Offset - Apparently reads stringTableCount

        List<CryXmlNode> nodeTable = new();
        br.BaseStream.Seek(nodeTableOffset, SeekOrigin.Begin);
        int nodeID = 0;
        while (br.BaseStream.Position < nodeTableOffset + nodeTableCount * nodeTableSize)
        {
            CryXmlNode value = new()
            {
                NodeID = nodeID++,
                NodeNameOffset = br.ReadInt32(byteOrder),
                ContentOffset = br.ReadInt32(byteOrder),
                AttributeCount = br.ReadInt16(byteOrder),
                ChildCount = br.ReadInt16(byteOrder),
                ParentNodeID = br.ReadInt32(byteOrder),
                FirstAttributeIndex = br.ReadInt32(byteOrder),
                FirstChildIndex = br.ReadInt32(byteOrder),
                Reserved = br.ReadInt32(byteOrder),
            };
            nodeTable.Add(value);
        }

        List<CryXmlReference> attributeTable = new();
        br.BaseStream.Seek(attributeTableOffset, SeekOrigin.Begin);
        while (br.BaseStream.Position < attributeTableOffset + attributeTableCount * referenceTableSize)
        {
            CryXmlReference value = new()
            {
                NameOffset = br.ReadInt32(byteOrder),
                ValueOffset = br.ReadInt32(byteOrder)
            };
            attributeTable.Add(value);
        }

        br.BaseStream.Seek(childTableOffset, SeekOrigin.Begin);
        while (br.BaseStream.Position < childTableOffset + childTableCount * length3) br.ReadInt32(byteOrder); // Offset - Apparently reads value

        List<CryXmlValue> dataTable = new();
        br.BaseStream.Seek(stringTableOffset, SeekOrigin.Begin);
        while (br.BaseStream.Position < br.BaseStream.Length)
        {
            long position = br.BaseStream.Position;
            CryXmlValue value = new()
            {
                Offset = (int)position - stringTableOffset,
                Value = br.ReadCString(),
            };
            dataTable.Add(value);
        }

        Dictionary<int, string> dataMap = dataTable.ToDictionary(k => k.Offset, v => v.Value);
        int attributeIndex = 0;
        XmlDocument xmlDoc = new();
        Dictionary<int, XmlElement> xmlMap = new();
        foreach (CryXmlNode node in nodeTable)
        {
            XmlElement element = xmlDoc.CreateElement(dataMap[node.NodeNameOffset]);
            for (int i = 0, j = node.AttributeCount; i < j; i++)
            {
                if (dataMap.ContainsKey(attributeTable[attributeIndex].ValueOffset)) element.SetAttribute(dataMap[attributeTable[attributeIndex].NameOffset], dataMap[attributeTable[attributeIndex].ValueOffset]);
                else element.SetAttribute(dataMap[attributeTable[attributeIndex].NameOffset], "BUGGED");
                attributeIndex++;
            }

            xmlMap[node.NodeID] = element;
            if (dataMap.ContainsKey(node.ContentOffset) && !string.IsNullOrWhiteSpace(dataMap[node.ContentOffset])) element.AppendChild(xmlDoc.CreateCDataSection(dataMap[node.ContentOffset]));
            else element.AppendChild(xmlDoc.CreateCDataSection("BUGGED"));

            if (xmlMap.ContainsKey(node.ParentNodeID)) xmlMap[node.ParentNodeID].AppendChild(element);
            else xmlDoc.AppendChild(element);
        }
        if (xmlDoc is not null) xmlDoc.Save(outFile.Open(FileMode.OpenOrCreate, FileAccess.Write, FileShare.None));
    }

    public static async Task ForgeData(DataForgeInstancePackage pckg)
    {
        XmlWriter writer = null;
        string currentSection = null;
        foreach (DataForgeDataMapping dm in pckg.DataMappingTable)
        {
            if (writer is null || currentSection != dm.Name)
            {
                currentSection = dm.Name;
                writer?.Close();
                writer?.Dispose();
                writer = XmlWriter.Create(new FileInfo(Path.Join(pckg.OutFile.FullName[..pckg.OutFile.FullName.LastIndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar })],
                        $"{pckg.OutFile.Name.Replace(pckg.OutFile.Extension, string.Empty)}_{dm.Name}.xml")).Open(FileMode.Create, FileAccess.Write, FileShare.None), new XmlWriterSettings
                        {
                            Indent = true,
                            Async = true
                        });
            }
            await pckg.StructDefinitionTable[dm.StructIndex].Read(writer);
        }
        writer?.Close();
        writer?.Dispose();
    }
}

public enum ByteOrderEnum
{
    AutoDetect,
    BigEndian,
    LittleEndian,
}

public class CryXmlValue
{
    public int Offset { get; set; }
    public string Value { get; set; }
}

public class CryXmlReference
{
    public int NameOffset { get; set; }
    public int ValueOffset { get; set; }
}

public class CryXmlNode
{
    public int NodeID { get; set; }
    public int NodeNameOffset { get; set; }
    public int ContentOffset { get; set; }
    public short AttributeCount { get; set; }
    public short ChildCount { get; set; }
    public int ParentNodeID { get; set; }
    public int FirstAttributeIndex { get; set; }
    public int FirstChildIndex { get; set; }
    public int Reserved { get; set; }
}

public static class CryXmlBinaryReaderExtensions
{
    public static long ReadInt64(this BinaryReader br, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
    {
        byte[] bytes = new[]
        {
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
            };
        if (byteOrder is ByteOrderEnum.LittleEndian) bytes = bytes.Reverse().ToArray();
        return BitConverter.ToInt64(bytes, 0);
    }

    public static int ReadInt32(this BinaryReader br, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
    {
        byte[] bytes = new[]
        {
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
            };
        if (byteOrder is ByteOrderEnum.LittleEndian) bytes = bytes.Reverse().ToArray();
        return BitConverter.ToInt32(bytes, 0);
    }

    public static short ReadInt16(this BinaryReader br, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
    {
        byte[] bytes = new[]
        {
                br.ReadByte(),
                br.ReadByte(),
            };
        if (byteOrder is ByteOrderEnum.LittleEndian) bytes = bytes.Reverse().ToArray();
        return BitConverter.ToInt16(bytes, 0);
    }

    public static ulong ReadUInt64(this BinaryReader br, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
    {
        byte[] bytes = new[]
        {
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
            };
        if (byteOrder is ByteOrderEnum.LittleEndian) bytes = bytes.Reverse().ToArray();
        return BitConverter.ToUInt64(bytes, 0);
    }

    public static uint ReadUInt32(this BinaryReader br, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
    {
        byte[] bytes = new[]
        {
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
            };
        if (byteOrder is ByteOrderEnum.LittleEndian) bytes = bytes.Reverse().ToArray();
        return BitConverter.ToUInt32(bytes, 0);
    }

    public static ushort ReadUInt16(this BinaryReader br, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
    {
        byte[] bytes = new[]
        {
                br.ReadByte(),
                br.ReadByte(),
            };
        if (byteOrder is ByteOrderEnum.LittleEndian) bytes = bytes.Reverse().ToArray();
        return BitConverter.ToUInt16(bytes, 0);
    }
}