using System;
using System.IO;
using System.Text;
using System.Xml;

namespace unforge
{
    public class DataForgePropertyDefinition : DataForgeSerializable
    {
        public uint NameOffset { get; set; }
        public string Name { get { return DocumentRoot.ValueMap[NameOffset]; } }
        public ushort StructIndex { get; set; }
        public EDataType DataType { get; set; }
        public EConversionType ConversionType { get; set; }
        public ushort Padding { get; set; }

        public DataForgePropertyDefinition(DataForge documentRoot) : base(documentRoot)
        {
            NameOffset = br.ReadUInt32();
            StructIndex = br.ReadUInt16();
            DataType = (EDataType)br.ReadUInt16();
            ConversionType = (EConversionType)br.ReadUInt16();
            Padding = br.ReadUInt16();
        }

        public XmlAttribute Read()
        {
            XmlAttribute attribute = DocumentRoot.CreateAttribute(Name);

            switch (DataType)
            {
                case EDataType.varReference:
                    attribute.Value = string.Format("{2}", DataType, br.ReadUInt32(), br.ReadGuid(false));
                    break;
                case EDataType.varLocale:
                    attribute.Value = string.Format("{1}", DataType, DocumentRoot.ValueMap[br.ReadUInt32()]);
                    break;
                case EDataType.varStrongPointer:
                    attribute.Value = string.Format("{0}:{1:X8} {2:X8}", DataType, br.ReadUInt32(), br.ReadUInt32());
                    break;
                case EDataType.varWeakPointer:
                    uint structIndex = br.ReadUInt32();
                    uint itemIndex = br.ReadUInt32();
                    attribute.Value = string.Format("{0}:{1:X8} {1:X8}", DataType, structIndex, itemIndex);
                    DocumentRoot.Require_WeakMapping2.Add(new ClassMapping { Node = attribute, StructIndex = (ushort)structIndex, RecordIndex = (int)itemIndex });
                    break;
                case EDataType.varString:
                    uint stringKey = br.ReadUInt32();
                    attribute.Value = DocumentRoot.ValueMap.ContainsKey(stringKey) ? DocumentRoot.ValueMap[stringKey] : DataType.ToString();
                    break;
                case EDataType.varBoolean:
                    attribute.Value = br.ReadByte().ToString();
                    break;
                case EDataType.varSingle:
                    attribute.Value = br.ReadSingle().ToString();
                    break;
                case EDataType.varDouble:
                    attribute.Value = br.ReadDouble().ToString();
                    break;
                case EDataType.varGuid:
                    attribute.Value = br.ReadGuid(false).ToString();
                    break;
                case EDataType.varSByte:
                    attribute.Value = br.ReadSByte().ToString();
                    break;
                case EDataType.varInt16:
                    attribute.Value = br.ReadInt16().ToString();
                    break;
                case EDataType.varInt32:
                    attribute.Value = br.ReadInt32().ToString();
                    break;
                case EDataType.varInt64:
                    attribute.Value = br.ReadInt64().ToString();
                    break;
                case EDataType.varByte:
                    attribute.Value = br.ReadByte().ToString();
                    break;
                case EDataType.varUInt16:
                    attribute.Value = br.ReadUInt16().ToString();
                    break;
                case EDataType.varUInt32:
                    attribute.Value = br.ReadUInt32().ToString();
                    break;
                case EDataType.varUInt64:
                    attribute.Value = br.ReadUInt64().ToString();
                    break;
                case EDataType.varEnum:
                    DataForgeEnumDefinition enumDefinition = DocumentRoot.EnumDefinitionTable[StructIndex];
                    uint enumKey = br.ReadUInt32();
                    attribute.Value = DocumentRoot.ValueMap.ContainsKey(enumKey) ? DocumentRoot.ValueMap[enumKey] : enumDefinition.Name;
                    break;
                default:
                    throw new NotImplementedException();
            }
            return attribute;
        }

        public override string ToString() => string.Format("<{0} />", Name);

        public string Export()
        {
            StringBuilder sb = new();
            sb.AppendFormat(@"        [XmlArrayItem(Type = typeof({0}))]", DocumentRoot.StructDefinitionTable[StructIndex].Name);
            sb.AppendLine();
            foreach (var structDefinition in DocumentRoot.StructDefinitionTable)
            {
                bool allowed = false;
                DataForgeStructDefinition baseStruct = structDefinition;
                while (baseStruct.ParentTypeIndex != 0xFFFFFFFF && !allowed)
                {
                    allowed |= (baseStruct.ParentTypeIndex == StructIndex);
                    baseStruct = DocumentRoot.StructDefinitionTable[baseStruct.ParentTypeIndex];
                }

                if (allowed)
                {
                    sb.AppendFormat(@"        [XmlArrayItem(Type = typeof({0}))]", structDefinition.Name);
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }
    }
}
