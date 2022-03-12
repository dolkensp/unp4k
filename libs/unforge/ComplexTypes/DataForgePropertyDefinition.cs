using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace unforge;

internal class DataForgePropertyDefinition : DataForgeSerializable
{
    internal uint NameOffset { get; set; }
    internal string Name => Index.ValueMap[NameOffset];
    internal ushort StructIndex { get; set; }
    internal EDataType DataType { get; set; }
    internal EConversionType ConversionType { get; set; }
    internal ushort Padding { get; set; }

    internal DataForgePropertyDefinition(DataForgeIndex Index) : base(Index)
    {
        NameOffset = Index.Reader.ReadUInt32();
        StructIndex = Index.Reader.ReadUInt16();
        DataType = (EDataType)Index.Reader.ReadUInt16();
        ConversionType = (EConversionType)Index.Reader.ReadUInt16();
        Padding = Index.Reader.ReadUInt16();
    }

    internal override Task PreSerialise() => Task.CompletedTask;
    internal override XmlElement Serialise(string name = null) => default;

    internal XmlAttribute Serialise()
    {
        XmlAttribute attribute = Index.Writer.CreateAttribute(Name);
        switch (DataType)
        {
            case EDataType.varReference:
                Index.Reader.ReadUInt32(); // Offset
                attribute.Value = Index.Reader.ReadGuid(false).ToString();
                break;
            case EDataType.varLocale:
                attribute.Value = Index.ValueMap[Index.Reader.ReadUInt32()];
                break;
            case EDataType.varStrongPointer:
                attribute.Value = $"{DataType}:{Index.Reader.ReadUInt32():X8} {Index.Reader.ReadUInt32():X8}";
                break;
            case EDataType.varWeakPointer:
                int structIndex = (int)Index.Reader.ReadUInt32();
                int itemIndex = (int)Index.Reader.ReadUInt32();
                attribute.Value = $"{DataType}:{structIndex:X8} {itemIndex:X8}";
                Index.Require_WeakMapping2.Add(new ClassMapping { Node = attribute, StructIndex = (ushort)structIndex, RecordIndex = itemIndex });
                break;
            case EDataType.varString:
                attribute.Value = Index.ValueMap[Index.Reader.ReadUInt32()];
                break;
            case EDataType.varBoolean:
                attribute.Value = Index.Reader.ReadByte().ToString();
                break;
            case EDataType.varSingle:
                attribute.Value = Index.Reader.ReadSingle().ToString();
                break;
            case EDataType.varDouble:
                attribute.Value = Index.Reader.ReadDouble().ToString();
                break;
            case EDataType.varGuid:
                attribute.Value = Index.Reader.ReadGuid(false).ToString();
                break;
            case EDataType.varSByte:
                attribute.Value = Index.Reader.ReadSByte().ToString();
                break;
            case EDataType.varInt16:
                attribute.Value = Index.Reader.ReadInt16().ToString();
                break;
            case EDataType.varInt32:
                attribute.Value = Index.Reader.ReadInt32().ToString();
                break;
            case EDataType.varInt64:
                attribute.Value = Index.Reader.ReadInt64().ToString();
                break;
            case EDataType.varByte:
                attribute.Value = Index.Reader.ReadByte().ToString();
                break;
            case EDataType.varUInt16:
                attribute.Value = Index.Reader.ReadUInt16().ToString();
                break;
            case EDataType.varUInt32:
                attribute.Value = Index.Reader.ReadUInt32().ToString();
                break;
            case EDataType.varUInt64:
                attribute.Value = Index.Reader.ReadUInt64().ToString();
                break;
            case EDataType.varEnum:
                DataForgeEnumDefinition enumDefinition = Index.EnumDefinitionTable[StructIndex];
                attribute.Value = Index.ValueMap[Index.Reader.ReadUInt32()];
                break;
            default:
                throw new NotImplementedException();
        }
        return attribute;
    }
}