using System;
using System.IO;
using System.Xml;
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

    internal async Task ReadAttribute(XmlWriter writer)
    {
        string val;
        switch (DataType)
        {
            case EDataType.varReference:
                val = Index.Reader.ReadGuid(false).ToString();
                break;
            case EDataType.varLocale:
                val = Index.ValueMap[Index.Reader.ReadUInt32()];
                break;
            case EDataType.varStrongPointer:
                val = $"{DataType}:{Index.Reader.ReadUInt32():X8} {Index.Reader.ReadUInt32():X8}";
                break;
            case EDataType.varWeakPointer:
                uint structIndex = Index.Reader.ReadUInt32();
                Index.Reader.ReadUInt32(); // Item Index offset
                val = $"{DataType}:{structIndex:X8} {structIndex:X8}";
                break;
            case EDataType.varString:
                uint stringKey = Index.Reader.ReadUInt32();
                val = Index.ValueMap.ContainsKey(stringKey) ? Index.ValueMap[stringKey] : DataType.ToString();
                break;
            case EDataType.varBoolean:
                val = Index.Reader.ReadByte().ToString();
                break;
            case EDataType.varSingle:
                val = Index.Reader.ReadSingle().ToString();
                break;
            case EDataType.varDouble:
                val = Index.Reader.ReadDouble().ToString();
                break;
            case EDataType.varGuid:
                val = Index.Reader.ReadGuid(false).ToString();
                break;
            case EDataType.varSByte:
                val = Index.Reader.ReadSByte().ToString();
                break;
            case EDataType.varInt16:
                val = Index.Reader.ReadInt16().ToString();
                break;
            case EDataType.varInt32:
                val = Index.Reader.ReadInt32().ToString();
                break;
            case EDataType.varInt64:
                val = Index.Reader.ReadInt64().ToString();
                break;
            case EDataType.varByte:
                val = Index.Reader.ReadByte().ToString();
                break;
            case EDataType.varUInt16:
                val = Index.Reader.ReadUInt16().ToString();
                break;
            case EDataType.varUInt32:
                val = Index.Reader.ReadUInt32().ToString();
                break;
            case EDataType.varUInt64:
                val = Index.Reader.ReadUInt64().ToString();
                break;
            case EDataType.varEnum:
                DataForgeEnumDefinition enumDefinition = Index.EnumDefinitionTable[StructIndex];
                uint enumKey = Index.Reader.ReadUInt32();
                val = Index.ValueMap.ContainsKey(enumKey) ? Index.ValueMap[enumKey] : enumDefinition.Name;
                break;
            default:
                throw new NotImplementedException();
        }
        await writer.WriteAttributeStringAsync(null, Name, null, val);
    }

    internal override Task Serialise(string name = null) => Task.CompletedTask;
}