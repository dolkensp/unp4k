using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace unforge;

public static class DataForge
{
    public static void DeserialiseCryXml(FileInfo inFile, FileInfo outFile, ByteOrderEnum byteOrder = ByteOrderEnum.AutoDetect, bool detailedLogs = false)
    {
        using BinaryReader br = new(inFile.Open(FileMode.Open, FileAccess.Read, FileShare.None));
        int peek = br.PeekChar();

        if (peek == '<') return; // File is already XML
        else if (peek != 'C') return; // Unknown file format

        string header = br.ReadFString(7);
        if (header == "CryXml" || header == "CryXmlB") br.ReadCString();
        else if (header == "CRY3SDK") br.ReadBytes(2);
        else return; // Unknown file format

        long headerLength = br.BaseStream.Position;
        byteOrder = ByteOrderEnum.BigEndian;
        int fileLength = br.ReadInt32(byteOrder);
        if (fileLength != br.BaseStream.Length)
        {
            br.BaseStream.Seek(headerLength, SeekOrigin.Begin);
            byteOrder = ByteOrderEnum.LittleEndian;
            fileLength = br.ReadInt32(byteOrder);
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
        int stringTableCount = br.ReadInt32(byteOrder);

#if DEBUG
        if (detailedLogs) Logger.LogInfo("Header" + '\n' +
                $"0x{0x00:X6}: {header}" + '\n' +
                $"0x{headerLength + 0x00:X6}: {fileLength:X8} (Dec: {fileLength:D8})" + '\n' +
                $"0x{headerLength + 0x04:X6}: {nodeTableOffset:X8} (Dec: {nodeTableOffset:D8}) node offset" + '\n' +
                $"0x{headerLength + 0x08:X6}: {nodeTableCount:X8} (Dec: {nodeTableCount:D8}) nodes" + '\n' +
                $"0x{headerLength + 0x12:X6}: {attributeTableOffset:X8} (Dec: {attributeTableOffset:D8}) reference offset" + '\n' +
                $"0x{headerLength + 0x16:X6}: {attributeTableCount:X8} (Dec: {attributeTableCount:D8}) references" + '\n' +
                $"0x{headerLength + 0x20:X6}: {childTableOffset:X8} (Dec: {childTableOffset:D8}) child offset" + '\n' +
                $"0x{headerLength + 0x24:X6}: {childTableCount:X8} (Dec: {childTableCount:D8}) child" + '\n' +
                $"0x{headerLength + 0x28:X6}: {stringTableOffset:X8} (Dec: {stringTableOffset:D8}) content offset" + '\n' +
                $"0x{headerLength + 0x32:X6}: {stringTableCount:X8} (Dec: {stringTableCount:D8}) content" + '\n' +
                "Node Table");
#endif

        List<CryXmlNode> nodeTable = new();
        br.BaseStream.Seek(nodeTableOffset, SeekOrigin.Begin);
        int nodeID = 0;
        while (br.BaseStream.Position < nodeTableOffset + nodeTableCount * nodeTableSize)
        {
            long position = br.BaseStream.Position;
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
#if DEBUG
            if (detailedLogs) Logger.LogInfo($"0x{position:X6}: {value.NodeNameOffset:X8} {value.ContentOffset:X8} attr:{value.AttributeCount:X4} {value.ChildCount:X4} {value.ParentNodeID:X8} " +
                $"{value.FirstAttributeIndex:X8} {value.FirstChildIndex:X8} {value.Reserved:X8}");
#endif
        }

#if DEBUG
        if (detailedLogs) Logger.LogInfo('\n' + "Reference Table");
#endif

        List<CryXmlReference> attributeTable = new();
        br.BaseStream.Seek(attributeTableOffset, SeekOrigin.Begin);
        while (br.BaseStream.Position < attributeTableOffset + attributeTableCount * referenceTableSize)
        {
            long position = br.BaseStream.Position;
            CryXmlReference value = new()
            {
                NameOffset = br.ReadInt32(byteOrder),
                ValueOffset = br.ReadInt32(byteOrder)
            };

            attributeTable.Add(value);
#if DEBUG
            if (detailedLogs) Logger.LogInfo($"0x{position:X6}: {value.NameOffset:X8} {value.ValueOffset:X8}");
#endif
        }

#if DEBUG
        if (detailedLogs) Logger.LogInfo('\n' + "Order Table");
#endif

        List<int> parentTable = new();
        br.BaseStream.Seek(childTableOffset, SeekOrigin.Begin);
        while (br.BaseStream.Position < childTableOffset + childTableCount * length3)
        {
            long position = br.BaseStream.Position;
            int value = br.ReadInt32(byteOrder);
            parentTable.Add(value);
#if DEBUG
            if (detailedLogs) Logger.LogInfo($"0x{position:X6}: {value:X8}");
#endif
        }

#if DEBUG
        if (detailedLogs) Logger.LogInfo('\n' + "Dynamic Dictionary");
#endif

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
#if DEBUG
            if (detailedLogs) Logger.LogInfo($"0x{position:X6}: {value.Offset:X8} {value.Value}");
#endif
        }

        Dictionary<int, string> dataMap = dataTable.ToDictionary(k => k.Offset, v => v.Value);
        int attributeIndex = 0;
        XmlDocument xmlDoc = new();

        Dictionary<int, XmlElement> xmlMap = new();
        foreach (CryXmlNode node in nodeTable)
        {
            if (dataMap.ContainsKey(node.NodeNameOffset) && !string.IsNullOrEmpty(dataMap[node.NodeNameOffset]))
            {
                XmlElement element = xmlDoc.CreateElement(dataMap[node.NodeNameOffset]);
                for (int i = 0, j = node.AttributeCount; i < j; i++)
                {
                    if (dataMap.ContainsKey(attributeTable[attributeIndex].NameOffset) && !string.IsNullOrEmpty(dataMap[attributeTable[attributeIndex].NameOffset]))
                    {
                        if (dataMap.ContainsKey(attributeTable[attributeIndex].ValueOffset) && !string.IsNullOrEmpty(dataMap[attributeTable[attributeIndex].ValueOffset]))
                            element.SetAttribute(dataMap[attributeTable[attributeIndex].NameOffset], dataMap[attributeTable[attributeIndex].ValueOffset]);
                        else element.SetAttribute(dataMap[attributeTable[attributeIndex].NameOffset], "BUGGED");
                    }
                    attributeIndex++;
                }

                xmlMap[node.NodeID] = element;
                if (dataMap.ContainsKey(node.ContentOffset) && !string.IsNullOrWhiteSpace(dataMap[node.ContentOffset])) element.AppendChild(xmlDoc.CreateCDataSection(dataMap[node.ContentOffset]));
                else element.AppendChild(xmlDoc.CreateCDataSection("BUGGED"));
                if (xmlMap.ContainsKey(node.ParentNodeID)) xmlMap[node.ParentNodeID].AppendChild(element);
                else xmlDoc.AppendChild(element);
            }
        }
        if (xmlDoc != null)
        {
            if (!outFile.Directory.Exists) outFile.Directory.Create();
            xmlDoc.Save(Path.ChangeExtension(outFile.FullName, "xml"));
        }
    }

    // Just a simplicity abstraction
    public static void Forge(FileInfo fileIn, FileInfo fileOut) => new DataForgeIndex(fileIn).Serialise(fileOut);
}
