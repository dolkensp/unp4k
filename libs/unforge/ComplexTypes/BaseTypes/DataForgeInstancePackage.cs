using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace unforge;
public class DataForgeInstancePackage
{
    internal FileInfo InFile { get; set; }
    internal FileInfo OutFile { get; set; }

    internal BinaryReader Br { get; set; }
    internal int FileVersion { get; set; }

    internal DataForgeStructDefinition[] StructDefinitionTable { get; set; }
    internal DataForgePropertyDefinition[] PropertyDefinitionTable { get; set; }
    internal DataForgeEnumDefinition[] EnumDefinitionTable { get; set; }
    internal DataForgeDataMapping[] DataMappingTable { get; set; }
    internal DataForgeRecord[] RecordDefinitionTable { get; set; }
    internal DataForgeStringLookup[] EnumOptionTable { get; set; }
    internal DataForgeString[] ValueTable { get; set; }

    internal DataForgeReference[] Array_ReferenceValues { get; set; }
    internal DataForgeGuid[] Array_GuidValues { get; set; }
    internal DataForgeStringLookup[] Array_StringValues { get; set; }
    internal DataForgeLocale[] Array_LocaleValues { get; set; }
    internal DataForgeEnum[] Array_EnumValues { get; set; }
    internal DataForgeInt8[] Array_Int8Values { get; set; }
    internal DataForgeInt16[] Array_Int16Values { get; set; }
    internal DataForgeInt32[] Array_Int32Values { get; set; }
    internal DataForgeInt64[] Array_Int64Values { get; set; }
    internal DataForgeUInt8[] Array_UInt8Values { get; set; }
    internal DataForgeUInt16[] Array_UInt16Values { get; set; }
    internal DataForgeUInt32[] Array_UInt32Values { get; set; }
    internal DataForgeUInt64[] Array_UInt64Values { get; set; }
    internal DataForgeBoolean[] Array_BooleanValues { get; set; }
    internal DataForgeSingle[] Array_SingleValues { get; set; }
    internal DataForgeDouble[] Array_DoubleValues { get; set; }
    internal DataForgePointer[] Array_StrongValues { get; set; }
    internal DataForgePointer[] Array_WeakValues { get; set; }

    internal Dictionary<uint, string> ValueMap { get; set; }
    internal Dictionary<uint, List<XmlElement>> DataMap { get; set; }
    internal List<XmlElement> DataTable { get; set; }

    public DataForgeInstancePackage(FileInfo inFile, FileInfo outFile)
    {
        U[] ReadArray<U>(int arraySize) where U : DataForgeSerializable
        {
            if (arraySize is -1) return null;
            else
            {
                U[] o = new U[arraySize];
                for (int i = 0; i < arraySize; i++) o[i] = (U)Activator.CreateInstance(typeof(U), this);
                return o;
            }
        }

        InFile = inFile;
        OutFile = outFile;

        Br = new(inFile.Open(FileMode.Open, FileAccess.Read, FileShare.None));
        Br.ReadInt32(); // Offset
        FileVersion = Br.ReadInt32();

        ValueMap = new();
        DataTable = new();
        DataMap = new();

        for (int i = 0; i < 4; i++) Br.ReadUInt16(); // Offset

        int structDefinitionCount = Br.ReadInt32();
        int propertyDefinitionCount = Br.ReadInt32();
        int enumDefinitionCount = Br.ReadInt32();
        int dataMappingCount = Br.ReadInt32();
        int recordDefinitionCount = Br.ReadInt32();

        int booleanValueCount = Br.ReadInt32();
        int int8ValueCount = Br.ReadInt32();
        int int16ValueCount = Br.ReadInt32();
        int int32ValueCount = Br.ReadInt32();
        int int64ValueCount = Br.ReadInt32();
        int uint8ValueCount = Br.ReadInt32();
        int uint16ValueCount = Br.ReadInt32();
        int uint32ValueCount = Br.ReadInt32();
        int uint64ValueCount = Br.ReadInt32();

        int singleValueCount = Br.ReadInt32();
        int doubleValueCount = Br.ReadInt32();
        int guidValueCount = Br.ReadInt32();
        int stringValueCount = Br.ReadInt32();
        int localeValueCount = Br.ReadInt32();
        int enumValueCount = Br.ReadInt32();
        int strongValueCount = Br.ReadInt32();
        int weakValueCount = Br.ReadInt32();

        int referenceValueCount = Br.ReadInt32();
        int enumOptionCount = Br.ReadInt32();
        uint textLength = Br.ReadUInt32();
        Br.ReadUInt32(); // Offset

        StructDefinitionTable = ReadArray<DataForgeStructDefinition>(structDefinitionCount);
        PropertyDefinitionTable = ReadArray<DataForgePropertyDefinition>(propertyDefinitionCount);
        EnumDefinitionTable = ReadArray<DataForgeEnumDefinition>(enumDefinitionCount);
        DataMappingTable = ReadArray<DataForgeDataMapping>(dataMappingCount);
        RecordDefinitionTable = ReadArray<DataForgeRecord>(recordDefinitionCount);

        Array_Int8Values = ReadArray<DataForgeInt8>(int8ValueCount);
        Array_Int16Values = ReadArray<DataForgeInt16>(int16ValueCount);
        Array_Int32Values = ReadArray<DataForgeInt32>(int32ValueCount);
        Array_Int64Values = ReadArray<DataForgeInt64>(int64ValueCount);
        Array_UInt8Values = ReadArray<DataForgeUInt8>(uint8ValueCount);
        Array_UInt16Values = ReadArray<DataForgeUInt16>(uint16ValueCount);
        Array_UInt32Values = ReadArray<DataForgeUInt32>(uint32ValueCount);
        Array_UInt64Values = ReadArray<DataForgeUInt64>(uint64ValueCount);
        Array_BooleanValues = ReadArray<DataForgeBoolean>(booleanValueCount);
        Array_SingleValues = ReadArray<DataForgeSingle>(singleValueCount);
        Array_DoubleValues = ReadArray<DataForgeDouble>(doubleValueCount);
        Array_GuidValues = ReadArray<DataForgeGuid>(guidValueCount);
        Array_StringValues = ReadArray<DataForgeStringLookup>(stringValueCount);
        Array_LocaleValues = ReadArray<DataForgeLocale>(localeValueCount);
        Array_EnumValues = ReadArray<DataForgeEnum>(enumValueCount);
        Array_StrongValues = ReadArray<DataForgePointer>(strongValueCount);
        Array_WeakValues = ReadArray<DataForgePointer>(weakValueCount);

        Array_ReferenceValues = ReadArray<DataForgeReference>(referenceValueCount);
        EnumOptionTable = ReadArray<DataForgeStringLookup>(enumOptionCount);

        List<DataForgeString> buffer = new();
        long maxPosition = Br.BaseStream.Position + textLength;
        long startPosition = Br.BaseStream.Position;
        while (Br.BaseStream.Position < maxPosition)
        {
            long offset = Br.BaseStream.Position - startPosition;
            DataForgeString dfString = new(this);
            buffer.Add(dfString);
            ValueMap[(uint)offset] = dfString.Value;
        }
        ValueTable = buffer.ToArray();
    }
}