using System;
using System.IO;
using System.Xml;
using System.Threading.Tasks;

namespace unforge;
public class DataForgePropertyDefinition : DataForgeSerializable
{
    public uint NameOffset { get; set; }
    public string Name { get { return DocumentRoot.ValueMap[NameOffset]; } }
    public ushort StructIndex { get; set; }
    public EDataType DataType { get; set; }
    public EConversionType ConversionType { get; set; }
    public ushort Padding { get; set; }

    public DataForgePropertyDefinition(DataForgeIndex documentRoot) : base(documentRoot)
    {
        NameOffset = Br.ReadUInt32();
        StructIndex = Br.ReadUInt16();
        DataType = (EDataType)Br.ReadUInt16();
        ConversionType = (EConversionType)Br.ReadUInt16();
        Padding = Br.ReadUInt16();
    }

    public async Task ReadAttribute(XmlWriter writer)
    {
        string val;
        switch (DataType)
        {
            case EDataType.varReference:
                val = Br.ReadGuid(false).ToString();
                break;
            case EDataType.varLocale:
                val = DocumentRoot.ValueMap[Br.ReadUInt32()];
                break;
            case EDataType.varStrongPointer:
                val = string.Format("{0}:{1:X8} {2:X8}", DataType, Br.ReadUInt32(), Br.ReadUInt32());
                break;
            case EDataType.varWeakPointer:
                uint structIndex = Br.ReadUInt32();
                Br.ReadUInt32(); // Item Index offset
                val = string.Format("{0}:{1:X8} {1:X8}", DataType, structIndex);
                break;
            case EDataType.varString:
                uint stringKey = Br.ReadUInt32();
                val = DocumentRoot.ValueMap.ContainsKey(stringKey) ? DocumentRoot.ValueMap[stringKey] : DataType.ToString();
                break;
            case EDataType.varBoolean:
                val = Br.ReadByte().ToString();
                break;
            case EDataType.varSingle:
                val = Br.ReadSingle().ToString();
                break;
            case EDataType.varDouble:
                val = Br.ReadDouble().ToString();
                break;
            case EDataType.varGuid:
                val = Br.ReadGuid(false).ToString();
                break;
            case EDataType.varSByte:
                val = Br.ReadSByte().ToString();
                break;
            case EDataType.varInt16:
                val = Br.ReadInt16().ToString();
                break;
            case EDataType.varInt32:
                val = Br.ReadInt32().ToString();
                break;
            case EDataType.varInt64:
                val = Br.ReadInt64().ToString();
                break;
            case EDataType.varByte:
                val = Br.ReadByte().ToString();
                break;
            case EDataType.varUInt16:
                val = Br.ReadUInt16().ToString();
                break;
            case EDataType.varUInt32:
                val = Br.ReadUInt32().ToString();
                break;
            case EDataType.varUInt64:
                val = Br.ReadUInt64().ToString();
                break;
            case EDataType.varEnum:
                DataForgeEnumDefinition enumDefinition = DocumentRoot.EnumDefinitionTable[StructIndex];
                uint enumKey = Br.ReadUInt32();
                val = DocumentRoot.ValueMap.ContainsKey(enumKey) ? DocumentRoot.ValueMap[enumKey] : enumDefinition.Name;
                break;
            default:
                throw new NotImplementedException();
        }
        await writer.WriteAttributeStringAsync(null, Name, null, val);
    }

    public override string ToString() => string.Format("<{0} />", Name);
}