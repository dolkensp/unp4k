using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace unforge
{
    public static class CryXmlSerializer
    {
        public static long ReadInt64(this BinaryReader br, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
        {
            byte[] bytes = new[] {
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
            };
            if (byteOrder == ByteOrderEnum.LittleEndian) bytes = bytes.Reverse().ToArray();
            return BitConverter.ToInt64(bytes, 0);
        }

        public static int ReadInt32(this BinaryReader br, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
        {
            byte[] bytes = new[] {
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
            };
            if (byteOrder == ByteOrderEnum.LittleEndian) bytes = bytes.Reverse().ToArray();
            return BitConverter.ToInt32(bytes, 0);
        }

        public static short ReadInt16(this BinaryReader br, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
        {
            byte[] bytes = new[] {
                br.ReadByte(),
                br.ReadByte(),
            };
            if (byteOrder == ByteOrderEnum.LittleEndian) bytes = bytes.Reverse().ToArray();
            return BitConverter.ToInt16(bytes, 0);
        }

        public static ulong ReadUInt64(this BinaryReader br, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
        {
            byte[] bytes = new[] {
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
            };
            if (byteOrder == ByteOrderEnum.LittleEndian) bytes = bytes.Reverse().ToArray();
            return BitConverter.ToUInt64(bytes, 0);
        }

        public static uint ReadUInt32(this BinaryReader br, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
        {
            byte[] bytes = new[] {
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
                br.ReadByte(),
            };
            if (byteOrder == ByteOrderEnum.LittleEndian) bytes = bytes.Reverse().ToArray();
            return BitConverter.ToUInt32(bytes, 0);
        }

        public static ushort ReadUInt16(this BinaryReader br, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
        {
            byte[] bytes = new[] {
                br.ReadByte(),
                br.ReadByte(),
            };
            if (byteOrder == ByteOrderEnum.LittleEndian) bytes = bytes.Reverse().ToArray();
            return BitConverter.ToUInt16(bytes, 0);
        }

        public static XmlDocument ReadFile(string inFile, ByteOrderEnum byteOrder = ByteOrderEnum.AutoDetect, bool writeLog = false) => ReadStream(File.OpenRead(inFile), byteOrder, writeLog);

        public static XmlDocument ReadStream(Stream inStream, ByteOrderEnum byteOrder = ByteOrderEnum.AutoDetect, bool writeLog = false)
        {
            using (BinaryReader br = new(inStream))
            {
                int peek = br.PeekChar();
                if (peek == '<') return null; // File is already XML
                else if (peek != 'C') return null; // Unknown file format

                string header = br.ReadFString(7);
                if (header == "CryXml" || header == "CryXmlB") br.ReadCString();
                else if (header == "CRY3SDK")
                {
                    //byte[] bytes = br.ReadBytes(2);
                }
                else throw new FormatException("Unknown File Format");

                long headerLength = br.BaseStream.Position;
                var fileLength = br.ReadInt32(byteOrder = ByteOrderEnum.BigEndian);

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

                /*
                 * TODO: Write this to Debug Log File
                if (writeLog)
                {
                    // Regex byteFormatter = new Regex("([0-9A-F]{8})");
                    Console.WriteLine("Header");
                    Console.WriteLine("0x{0:X6}: {1}", 0x00, header);
                    Console.WriteLine("0x{0:X6}: {1:X8} (Dec: {1:D8})", headerLength + 0x00, fileLength);
                    Console.WriteLine("0x{0:X6}: {1:X8} (Dec: {1:D8}) node offset", headerLength + 0x04, nodeTableOffset);
                    Console.WriteLine("0x{0:X6}: {1:X8} (Dec: {1:D8}) nodes", headerLength + 0x08, nodeTableCount);
                    Console.WriteLine("0x{0:X6}: {1:X8} (Dec: {1:D8}) reference offset", headerLength + 0x12, attributeTableOffset);
                    Console.WriteLine("0x{0:X6}: {1:X8} (Dec: {1:D8}) references", headerLength + 0x16, attributeTableCount);
                    Console.WriteLine("0x{0:X6}: {1:X8} (Dec: {1:D8}) child offset", headerLength + 0x20, childTableOffset);
                    Console.WriteLine("0x{0:X6}: {1:X8} (Dec: {1:D8}) child", headerLength + 0x24, childTableCount);
                    Console.WriteLine("0x{0:X6}: {1:X8} (Dec: {1:D8}) content offset", headerLength + 0x28, stringTableOffset);
                    Console.WriteLine("0x{0:X6}: {1:X8} (Dec: {1:D8}) content", headerLength + 0x32, stringTableCount);
                    Console.WriteLine("");
                    Console.WriteLine("Node Table");
                }
                */

                List<CryXmlNode> nodeTable = new() { };
                br.BaseStream.Seek(nodeTableOffset, SeekOrigin.Begin);
                int nodeID = 0;
                while (br.BaseStream.Position < nodeTableOffset + nodeTableCount * nodeTableSize)
                {
                    var position = br.BaseStream.Position;
                    var value = new CryXmlNode
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
                    /*
                     * TODO: Write this to Debug Log File
                    if (writeLog)
                    {
                        Console.WriteLine(
                            "0x{0:X6}: {1:X8} {2:X8} attr:{3:X4} {4:X4} {5:X8} {6:X8} {7:X8} {8:X8}",
                            position,
                            value.NodeNameOffset,
                            value.ContentOffset,
                            value.AttributeCount,
                            value.ChildCount,
                            value.ParentNodeID,
                            value.FirstAttributeIndex,
                            value.FirstChildIndex,
                            value.Reserved);
                    }
                    */
                }

                /*
                 * TODO: Write this to Debug Log File
                if (writeLog)
                {
                    Console.WriteLine("");
                    Console.WriteLine("Reference Table");
                }
                */

                List<CryXmlReference> attributeTable = new() { };
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
                    /*
                     * TODO: Write this to Debug Log File
                    if (writeLog)
                    {
                        Console.WriteLine("0x{0:X6}: {1:X8} {2:X8}", position, value.NameOffset, value.ValueOffset);
                    }
                     */
                }
                /*
                 * TODO: Write this to Debug Log File
                if (writeLog)
                {
                    Console.WriteLine("");
                    Console.WriteLine("Order Table");
                }
                */

                List<int> parentTable = new() { };
                br.BaseStream.Seek(childTableOffset, SeekOrigin.Begin);
                while (br.BaseStream.Position < childTableOffset + childTableCount * length3)
                {
                    long position = br.BaseStream.Position;
                    int value = br.ReadInt32(byteOrder);
                    parentTable.Add(value);
                    /*
                     * TODO: Write this to Debug Log File
                    if (writeLog)
                    {
                        Console.WriteLine("0x{0:X6}: {1:X8}", position, value);
                    }
                    */
                }
                /*
                 * TODO: Write this to Debug Log File
                if (writeLog)
                {
                    Console.WriteLine("");
                    Console.WriteLine("Dynamic Dictionary");
                }
                */

                List<CryXmlValue> dataTable = new() { };
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
                    /*
                     * TODO: Write this to Debug Log File
                    if (writeLog)
                    {
                        Console.WriteLine("0x{0:X6}: {1:X8} {2}", position, value.Offset, value.Value);
                    }
                    */
                }

                Dictionary<int, string> dataMap = dataTable.ToDictionary(k => k.Offset, v => v.Value);
                int attributeIndex = 0;
                XmlDocument xmlDoc = new();
                Dictionary<int, XmlElement> xmlMap = new() { };
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
                return xmlDoc;
            }
        }

        public static TObject Deserialize<TObject>(string inFile, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian, bool writeLog = false) where TObject : class
        {
            using (MemoryStream ms = new())
            {
                XmlDocument xmlDoc = ReadFile(inFile, byteOrder, writeLog);
                xmlDoc.Save(ms);
                ms.Seek(0, SeekOrigin.Begin);
                XmlSerializer xs = new(typeof(TObject));
                return xs.Deserialize(ms) as TObject;
            }
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
}
