using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace unforge;

internal class DataForgeIndex
{
    internal BinaryReader Reader;
    internal XmlDocument Writer = new();

    internal bool IsLegacy { get; set; }
    internal int FileVersion { get; set; }

    internal DataForgeStructDefinition[] StructDefinitionTable { get; set; }
    internal DataForgePropertyDefinition[] PropertyDefinitionTable { get; set; }
    internal DataForgeEnumDefinition[] EnumDefinitionTable { get; set; }
    internal DataForgeDataMapping[] DataMappingTable { get; set; }
    internal DataForgeRecord[] RecordDefinitionTable { get; set; }
    internal DataForgeStringLookup[] EnumOptionTable { get; set; }
    internal DataForgeString[] ValueTable { get; set; }

    internal DataForgeReference[] ReferenceValues { get; set; }
    internal DataForgeGuid[] GuidValues { get; set; }
    internal DataForgeStringLookup[] StringValues { get; set; }
    internal DataForgeLocale[] LocaleValues { get; set; }
    internal DataForgeEnum[] EnumValues { get; set; }
    internal DataForgeInt8[] Int8Values { get; set; }
    internal DataForgeInt16[] Int16Values { get; set; }
    internal DataForgeInt32[] Int32Values { get; set; }
    internal DataForgeInt64[] Int64Values { get; set; }
    internal DataForgeUInt8[] UInt8Values { get; set; }
    internal DataForgeUInt16[] UInt16Values { get; set; }
    internal DataForgeUInt32[] UInt32Values { get; set; }
    internal DataForgeUInt64[] UInt64Values { get; set; }
    internal DataForgeBoolean[] BooleanValues { get; set; }
    internal DataForgeSingle[] SingleValues { get; set; }
    internal DataForgeDouble[] DoubleValues { get; set; }
    internal DataForgePointer[] StrongValues { get; set; }
    internal DataForgePointer[] WeakValues { get; set; }

    internal List<ClassMapping> Require_ClassMapping { get; set; }
    internal List<ClassMapping> Require_StrongMapping { get; set; }
    internal List<ClassMapping> Require_WeakMapping1 { get; set; }
    internal List<ClassMapping> Require_WeakMapping2 { get; set; }
    internal Dictionary<uint, string> ValueMap { get; set; }
    internal Dictionary<uint, List<XmlElement>> DataMap { get; set; }
    internal List<XmlElement> DataTable { get; set; }

    internal DataForgeIndex(FileInfo inFile)
    {
        U[] ReadArray<U>(int size)
        {
            if (size is -1) return null;
            else
            {
                U[] array = new U[size];
                for (int i = 0; i < size; i++) array[i] = (U)typeof(U).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).First().Invoke(new object[] { this });
                return array;
            }
        }

        Require_ClassMapping = new();
        Require_StrongMapping = new();
        Require_WeakMapping1 = new();
        Require_WeakMapping2 = new();
        ValueMap = new();
        DataMap = new();
        DataTable = new();

        Reader = new(inFile.Open(FileMode.Open, FileAccess.Read, FileShare.None));
        Reader.ReadInt32(); // Offset - TODO: Figure out what this is
        FileVersion = Reader.ReadInt32();
        IsLegacy = inFile.Length < 0x0e2e00;

        if (!IsLegacy) for (int i = 0; i < 4; i++) Reader.ReadUInt16(); // Offset - TODO: Figure out what this is - This might be a Int64?

        int structDefinitionCount = Reader.ReadInt32();
        int propertyDefinitionCount = Reader.ReadInt32();
        int enumDefinitionCount = Reader.ReadInt32();
        int dataMappingCount = Reader.ReadInt32();
        int recordDefinitionCount = Reader.ReadInt32();

        int booleanValueCount = Reader.ReadInt32();
        int int8ValueCount = Reader.ReadInt32();
        int int16ValueCount = Reader.ReadInt32();
        int int32ValueCount = Reader.ReadInt32();
        int int64ValueCount = Reader.ReadInt32();
        int uint8ValueCount = Reader.ReadInt32();
        int uint16ValueCount = Reader.ReadInt32();
        int uint32ValueCount = Reader.ReadInt32();
        int uint64ValueCount = Reader.ReadInt32();

        int singleValueCount = Reader.ReadInt32();
        int doubleValueCount = Reader.ReadInt32();
        int guidValueCount = Reader.ReadInt32();
        int stringValueCount = Reader.ReadInt32();
        int localeValueCount = Reader.ReadInt32();
        int enumValueCount = Reader.ReadInt32();
        int strongValueCount = Reader.ReadInt32();
        int weakValueCount = Reader.ReadInt32();

        int referenceValueCount = Reader.ReadInt32();
        int enumOptionCount = Reader.ReadInt32();
        uint textLength = Reader.ReadUInt32();
        if (!IsLegacy) Reader.ReadUInt32(); // Offset - TODO: Figure out what this is

        StructDefinitionTable = ReadArray<DataForgeStructDefinition>(structDefinitionCount);
        PropertyDefinitionTable = ReadArray<DataForgePropertyDefinition>(propertyDefinitionCount);
        EnumDefinitionTable = ReadArray<DataForgeEnumDefinition>(enumDefinitionCount);
        DataMappingTable = ReadArray<DataForgeDataMapping>(dataMappingCount);
        RecordDefinitionTable = ReadArray<DataForgeRecord>(recordDefinitionCount);

        Int8Values = ReadArray<DataForgeInt8>(int8ValueCount);
        Int16Values = ReadArray<DataForgeInt16>(int16ValueCount);
        Int32Values = ReadArray<DataForgeInt32>(int32ValueCount);
        Int64Values = ReadArray<DataForgeInt64>(int64ValueCount);
        UInt8Values = ReadArray<DataForgeUInt8>(uint8ValueCount);
        UInt16Values = ReadArray<DataForgeUInt16>(uint16ValueCount);
        UInt32Values = ReadArray<DataForgeUInt32>(uint32ValueCount);
        UInt64Values = ReadArray<DataForgeUInt64>(uint64ValueCount);
        BooleanValues = ReadArray<DataForgeBoolean>(booleanValueCount);
        SingleValues = ReadArray<DataForgeSingle>(singleValueCount);
        DoubleValues = ReadArray<DataForgeDouble>(doubleValueCount);
        GuidValues = ReadArray<DataForgeGuid>(guidValueCount);
        StringValues = ReadArray<DataForgeStringLookup>(stringValueCount);
        LocaleValues = ReadArray<DataForgeLocale>(localeValueCount);
        EnumValues = ReadArray<DataForgeEnum>(enumValueCount);
        StrongValues = ReadArray<DataForgePointer>(strongValueCount);
        WeakValues = ReadArray<DataForgePointer>(weakValueCount);

        ReferenceValues = ReadArray<DataForgeReference>(referenceValueCount);
        EnumOptionTable = ReadArray<DataForgeStringLookup>(enumOptionCount);

        List<DataForgeString> buffer = new();
        long maxPosition = Reader.BaseStream.Position + textLength;
        long startPosition = Reader.BaseStream.Position;
        while (Reader.BaseStream.Position < maxPosition)
        {
            long offset = Reader.BaseStream.Position - startPosition;
            DataForgeString dfString = new(this);
            buffer.Add(dfString);
            ValueMap[(uint)offset] = dfString.Value;
        }
        ValueTable = buffer.ToArray();

        Array.ForEach(DataMappingTable, dataMapping =>
        {
            DataMap[dataMapping.StructIndex] = new();
            DataForgeStructDefinition dataStruct = StructDefinitionTable[dataMapping.StructIndex];
            for (int i = 0; i < dataMapping.StructCount; i++)
            {
                XmlElement node = dataStruct.Serialise(dataMapping.Name);
                DataMap[dataMapping.StructIndex].Add(node);
                DataTable.Add(node);
            }
        });

        Require_ClassMapping.ForEach(dataMapping =>
        {
            if (dataMapping.StructIndex == 0xFFFF) dataMapping.Node.ParentNode.RemoveChild(dataMapping.Node);
            else if (DataMap.ContainsKey(dataMapping.StructIndex) && DataMap[dataMapping.StructIndex].Count > dataMapping.RecordIndex) dataMapping.Node.ParentNode.ReplaceChild(DataMap[dataMapping.StructIndex][dataMapping.RecordIndex], dataMapping.Node);
            else
            {
                XmlElement bugged = Writer.CreateElement("bugged");
                XmlAttribute __class = Writer.CreateAttribute("__class");
                XmlAttribute __index = Writer.CreateAttribute("__index");
                __class.Value = $"{dataMapping.StructIndex:X8}";
                __index.Value = $"{dataMapping.RecordIndex:X8}";
                bugged.Attributes.Append(__class);
                bugged.Attributes.Append(__index);
                dataMapping.Node.ParentNode.ReplaceChild(bugged, dataMapping.Node);
            }
        });
    }

    internal void Serialise(FileInfo outFile, bool detailedLogs)
    {
        int i = 0;
        outFile = new(Path.ChangeExtension(outFile.FullName, "xml"));
        if (string.IsNullOrWhiteSpace(Writer.InnerXml))
        {
            XmlElement root = Writer.CreateElement("unp4k");
            Writer.AppendChild(root);

            Require_StrongMapping.ForEach(dataMapping =>
            {
                DataForgePointer strong = StrongValues[dataMapping.RecordIndex];
                if (strong.Value == 0xFFFFFFFF) dataMapping.Node.ParentNode.RemoveChild(dataMapping.Node);
                else dataMapping.Node.ParentNode.ReplaceChild(DataMap[strong.StructType][(int)strong.Value], dataMapping.Node);
            });

            Require_WeakMapping1.ForEach(dataMapping =>
            {
                DataForgePointer weak = WeakValues[dataMapping.RecordIndex];
                XmlNode weakAttribute = dataMapping.Node;
                if (weak.Value == 0xFFFFFFFF) weakAttribute.Value = string.Format("0");
                else
                {
                    XmlElement targetElement = DataMap[weak.StructType][(int)weak.Value];
                    weakAttribute.Value = targetElement.GetPath();
                }
            });

            Require_WeakMapping2.ForEach(dataMapping =>
            {
                XmlNode weakAttribute = dataMapping.Node;
                if (dataMapping.StructIndex == 0xFFFF) weakAttribute.Value = "null";
                else if (dataMapping.RecordIndex == -1)
                {
                    List<XmlElement> targetElement = DataMap[dataMapping.StructIndex];
                    weakAttribute.Value = targetElement.FirstOrDefault()?.GetPath();
                }
                else
                {
                    XmlElement targetElement = DataMap[dataMapping.StructIndex][dataMapping.RecordIndex];
                    weakAttribute.Value = targetElement.GetPath();
                }
            });

            Array.ForEach(RecordDefinitionTable, record =>
            {
                string fileReference = record.FileName;
                if (fileReference.Split('/').Length == 2) fileReference = fileReference.Split('/')[1];
                if (string.IsNullOrWhiteSpace(fileReference)) fileReference = $@"Dump\{record.Name}_{i++}.xml";
                if (record.Hash.HasValue && record.Hash != Guid.Empty)
                {
                    XmlAttribute hash = Writer.CreateAttribute("__ref");
                    hash.Value = $"{record.Hash}";
                    DataMap[record.StructIndex][record.VariantIndex].Attributes.Append(hash);
                }
                if (!string.IsNullOrWhiteSpace(record.FileName))
                {
                    XmlAttribute path = Writer.CreateAttribute("__path");
                    path.Value = $"{record.FileName}";
                    DataMap[record.StructIndex][record.VariantIndex].Attributes.Append(path);
                }
                DataMap[record.StructIndex][record.VariantIndex] = DataMap[record.StructIndex][record.VariantIndex].Rename(record.Name);
                root.AppendChild(DataMap[record.StructIndex][record.VariantIndex]);
            });
            i = 0;
        }

        Array.ForEach(RecordDefinitionTable, record =>
        {
            string fileReference = record.FileName;
            if (fileReference.Split('/').Length == 2) fileReference = fileReference.Split('/')[1];
            if (string.IsNullOrWhiteSpace(fileReference)) fileReference = string.Format(@"Dump\{0}_{1}.xml", record.Name, i++);
            string newPath = Path.Combine(Path.GetDirectoryName(outFile.FullName), fileReference);
            if (!Directory.Exists(Path.GetDirectoryName(newPath))) Directory.CreateDirectory(Path.GetDirectoryName(newPath));
            XmlDocument doc = new();
            doc.LoadXml(DataMap[record.StructIndex][record.VariantIndex].OuterXml);
            doc.Save(newPath);
        });
        Writer.Save(outFile.FullName);
    }
}

internal class ClassMapping
{
    internal XmlNode Node { get; set; }
    internal ushort StructIndex { get; set; }
    internal int RecordIndex { get; set; }
}