using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace unforge;
internal class DataForgeIndex
{
    internal BinaryReader Reader { get; private set; }
    internal XmlWriter Writer { get; private set; }
    internal int FileVersion { get; private set; }

    internal List<DataForgeStructDefinition> StructDefinitionTable { get; set; }
    internal List<DataForgePropertyDefinition> PropertyDefinitionTable { get; set; }
    internal List<DataForgeEnumDefinition> EnumDefinitionTable { get; set; }
    internal List<DataForgeDataMapping> DataMappingTable { get; set; }
    internal List<DataForgeRecord> RecordDefinitionTable { get; set; }
    internal List<DataForgeStringLookup> EnumOptionTable { get; set; }
    internal List<DataForgeString> ValueTable { get; set; }

    internal List<DataForgeReference> ReferenceValues { get; set; }
    internal List<DataForgeGuid> GuidValues { get; set; }
    internal List<DataForgeStringLookup> StringValues { get; set; }
    internal List<DataForgeLocale> LocaleValues { get; set; }
    internal List<DataForgeEnum> EnumValues { get; set; }
    internal List<DataForgeInt8> Int8Values { get; set; }
    internal List<DataForgeInt16> Int16Values { get; set; }
    internal List<DataForgeInt32> Int32Values { get; set; }
    internal List<DataForgeInt64> Int64Values { get; set; }
    internal List<DataForgeUInt8> UInt8Values { get; set; }
    internal List<DataForgeUInt16> UInt16Values { get; set; }
    internal List<DataForgeUInt32> UInt32Values { get; set; }
    internal List<DataForgeUInt64> UInt64Values { get; set; }
    internal List<DataForgeBoolean> BooleanValues { get; set; }
    internal List<DataForgeSingle> SingleValues { get; set; }
    internal List<DataForgeDouble> DoubleValues { get; set; }
    internal List<DataForgePointer> StrongValues { get; set; }
    internal List<DataForgePointer> WeakValues { get; set; }

    internal Dictionary<uint, string> ValueMap { get; set; } = new();
    internal Dictionary<uint, List<XmlElement>> DataMap { get; set; } = new();
    internal List<XmlElement> DataTable { get; set; } = new();

    internal DataForgeIndex(FileInfo inFile, XmlWriter writer)
    {
        List<U> ReadArray<U>(int size)
        {
            if (size is -1) return null;
            else
            {
                List<U> o = new();
                for (int i = 0; i < size; i++) o.Add((U)typeof(U).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).First().Invoke(new object[] { this }));
                return o;
            }
        }

        Writer = writer;
        Reader = new(inFile.Open(FileMode.Open, FileAccess.Read, FileShare.None));
        Reader.ReadInt32(); // Offset - TODO: Figure out what this is
        FileVersion = Reader.ReadInt32();

        for (int i = 0; i < 4; i++) Reader.ReadUInt16(); // Offset - TODO: Figure out what this is - This might be a Int64?

        int structDefinitionCount =     Reader.ReadInt32();
        int propertyDefinitionCount =   Reader.ReadInt32();
        int enumDefinitionCount =       Reader.ReadInt32();
        int dataMappingCount =          Reader.ReadInt32();
        int recordDefinitionCount =     Reader.ReadInt32();

        int booleanValueCount =         Reader.ReadInt32();
        int int8ValueCount =            Reader.ReadInt32();
        int int16ValueCount =           Reader.ReadInt32();
        int int32ValueCount =           Reader.ReadInt32();
        int int64ValueCount =           Reader.ReadInt32();
        int uint8ValueCount =           Reader.ReadInt32();
        int uint16ValueCount =          Reader.ReadInt32();
        int uint32ValueCount =          Reader.ReadInt32();
        int uint64ValueCount =          Reader.ReadInt32();

        int singleValueCount =          Reader.ReadInt32();
        int doubleValueCount =          Reader.ReadInt32();
        int guidValueCount =            Reader.ReadInt32();
        int stringValueCount =          Reader.ReadInt32();
        int localeValueCount =          Reader.ReadInt32();
        int enumValueCount =            Reader.ReadInt32();
        int strongValueCount =          Reader.ReadInt32();
        int weakValueCount =            Reader.ReadInt32();

        int referenceValueCount =       Reader.ReadInt32();
        int enumOptionCount =           Reader.ReadInt32();

        uint textLength =               Reader.ReadUInt32();
        Reader.ReadUInt32(); // Offset - TODO: Figure out what this is

        StructDefinitionTable =         ReadArray<DataForgeStructDefinition>(structDefinitionCount);
        PropertyDefinitionTable =       ReadArray<DataForgePropertyDefinition>(propertyDefinitionCount);
        EnumDefinitionTable =           ReadArray<DataForgeEnumDefinition>(enumDefinitionCount);
        DataMappingTable =              ReadArray<DataForgeDataMapping>(dataMappingCount);
        RecordDefinitionTable =         ReadArray<DataForgeRecord>(recordDefinitionCount);

        BooleanValues =                 ReadArray<DataForgeBoolean>(booleanValueCount);
        Int8Values =                    ReadArray<DataForgeInt8>(int8ValueCount);
        Int16Values =                   ReadArray<DataForgeInt16>(int16ValueCount);
        Int32Values =                   ReadArray<DataForgeInt32>(int32ValueCount);
        Int64Values =                   ReadArray<DataForgeInt64>(int64ValueCount);
        UInt8Values =                   ReadArray<DataForgeUInt8>(uint8ValueCount);
        UInt16Values =                  ReadArray<DataForgeUInt16>(uint16ValueCount);
        UInt32Values =                  ReadArray<DataForgeUInt32>(uint32ValueCount);
        UInt64Values =                  ReadArray<DataForgeUInt64>(uint64ValueCount);

        SingleValues =                  ReadArray<DataForgeSingle>(singleValueCount);
        DoubleValues =                  ReadArray<DataForgeDouble>(doubleValueCount);
        GuidValues =                    ReadArray<DataForgeGuid>(guidValueCount);
        StringValues =                  ReadArray<DataForgeStringLookup>(stringValueCount);
        LocaleValues =                  ReadArray<DataForgeLocale>(localeValueCount);
        EnumValues =                    ReadArray<DataForgeEnum>(enumValueCount);
        StrongValues =                  ReadArray<DataForgePointer>(strongValueCount);
        WeakValues =                    ReadArray<DataForgePointer>(weakValueCount);

        ReferenceValues =               ReadArray<DataForgeReference>(referenceValueCount);
        EnumOptionTable =               ReadArray<DataForgeStringLookup>(enumOptionCount);

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
        ValueTable = buffer;
    }
}