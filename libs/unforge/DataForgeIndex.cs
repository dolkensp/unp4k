using System;
using System.Collections.Generic;
using System.IO;
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
                for (int i = 0; i < size; i++) o.Add((U)Activator.CreateInstance(typeof(U), this));
                return o;
            }
        }

        Writer = writer;
        Reader = new(inFile.Open(FileMode.Open, FileAccess.Read, FileShare.None));
        Reader.ReadInt32(); // Offset - TODO: Figure out what this is
        FileVersion = Reader.ReadInt32();

        for (int i = 0; i < 4; i++) Reader.ReadUInt16(); // Offset - TODO: Figure out what this is - This might be a Int64?

        StructDefinitionTable =         ReadArray<DataForgeStructDefinition>(Reader.ReadInt32());
        PropertyDefinitionTable =       ReadArray<DataForgePropertyDefinition>(Reader.ReadInt32());
        EnumDefinitionTable =           ReadArray<DataForgeEnumDefinition>(Reader.ReadInt32());
        DataMappingTable =              ReadArray<DataForgeDataMapping>(Reader.ReadInt32());
        RecordDefinitionTable =         ReadArray<DataForgeRecord>(Reader.ReadInt32());

        Int8Values =                    ReadArray<DataForgeInt8>(Reader.ReadInt32());
        Int16Values =                   ReadArray<DataForgeInt16>(Reader.ReadInt32());
        Int32Values =                   ReadArray<DataForgeInt32>(Reader.ReadInt32());
        Int64Values =                   ReadArray<DataForgeInt64>(Reader.ReadInt32());
        UInt8Values =                   ReadArray<DataForgeUInt8>(Reader.ReadInt32());
        UInt16Values =                  ReadArray<DataForgeUInt16>(Reader.ReadInt32());
        UInt32Values =                  ReadArray<DataForgeUInt32>(Reader.ReadInt32());
        UInt64Values =                  ReadArray<DataForgeUInt64>(Reader.ReadInt32());
        BooleanValues =                 ReadArray<DataForgeBoolean>(Reader.ReadInt32());
        SingleValues =                  ReadArray<DataForgeSingle>(Reader.ReadInt32());
        DoubleValues =                  ReadArray<DataForgeDouble>(Reader.ReadInt32());
        GuidValues =                    ReadArray<DataForgeGuid>(Reader.ReadInt32());
        StringValues =                  ReadArray<DataForgeStringLookup>(Reader.ReadInt32());
        LocaleValues =                  ReadArray<DataForgeLocale>(Reader.ReadInt32());
        EnumValues =                    ReadArray<DataForgeEnum>(Reader.ReadInt32());
        StrongValues =                  ReadArray<DataForgePointer>(Reader.ReadInt32());
        WeakValues =                    ReadArray<DataForgePointer>(Reader.ReadInt32());

        ReferenceValues =               ReadArray<DataForgeReference>(Reader.ReadInt32());
        EnumOptionTable =               ReadArray<DataForgeStringLookup>(Reader.ReadInt32());

        uint textLength = Reader.ReadUInt32();
        Reader.ReadUInt32(); // Offset - TODO: Figure out what this is

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