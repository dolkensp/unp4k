using System;
using System.IO;
using System.Threading.Tasks;

namespace unforge;
internal class DataForgePropertyDefinition : DataForgeSerializable
{
    internal string Name => Index.ValueMap[NameOffset];
    internal uint NameOffset { get; set; }
    internal ushort StructIndex { get; set; }
    internal EDataType DataType { get; set; }
    internal EConversionType ConversionType { get; set; }
    internal ushort Padding { get; set; }

    internal DataForgePropertyDefinition(DataForgeIndex index) : base(index)
    {
        NameOffset = Index.Reader.ReadUInt32();
        StructIndex = Index.Reader.ReadUInt16();
        DataType = (EDataType)Index.Reader.ReadUInt16();
        ConversionType = (EConversionType)Index.Reader.ReadUInt16();
        Padding = Index.Reader.ReadUInt16();
    }

    internal string ReadAttribute()
    {
        switch (DataType)
        {
            case EDataType.varReference:
                return Index.Reader.ReadGuid(false).ToString();
            case EDataType.varLocale:
                return Index.ValueMap[Index.Reader.ReadUInt32()];
            case EDataType.varStrongPointer:
                return $"{DataType}:{Index.Reader.ReadUInt32():X8} {Index.Reader.ReadUInt32():X8}";
            case EDataType.varWeakPointer:
                uint structIndex = Index.Reader.ReadUInt32();
                Index.Reader.ReadUInt32(); // Item Index offset
                return $"{DataType}:{structIndex:X8} {structIndex:X8}";
            case EDataType.varString:
                uint stringKey = Index.Reader.ReadUInt32();
                return Index.ValueMap.ContainsKey(stringKey) ? Index.ValueMap[stringKey] : DataType.ToString();
            case EDataType.varBoolean:
                return Index.Reader.ReadByte().ToString();
            case EDataType.varSingle:
                return Index.Reader.ReadSingle().ToString();
            case EDataType.varDouble:
                return Index.Reader.ReadDouble().ToString();
            case EDataType.varGuid:
                return Index.Reader.ReadGuid(false).ToString();
            case EDataType.varSByte:
                return Index.Reader.ReadSByte().ToString();
            case EDataType.varInt16:
                return Index.Reader.ReadInt16().ToString();
            case EDataType.varInt32:
                return Index.Reader.ReadInt32().ToString();
            case EDataType.varInt64:
                return Index.Reader.ReadInt64().ToString();
            case EDataType.varByte:
                return Index.Reader.ReadByte().ToString();
            case EDataType.varUInt16:
                return Index.Reader.ReadUInt16().ToString();
            case EDataType.varUInt32:
                return Index.Reader.ReadUInt32().ToString();
            case EDataType.varUInt64:
                return Index.Reader.ReadUInt64().ToString();
            case EDataType.varEnum:
                DataForgeEnumDefinition enumDefinition = Index.EnumDefinitionTable[StructIndex];
                uint enumKey = Index.Reader.ReadUInt32();
                return Index.ValueMap.ContainsKey(enumKey) ? Index.ValueMap[enumKey] : enumDefinition.Name;
            default:
                throw new NotImplementedException();
        }
    }

    internal override Task PreSerialise() => Task.CompletedTask;
    internal override Task Serialise(string name = null) => Task.CompletedTask;
}