using System;
using System.IO;
using System.Text;
using System.Xml;

namespace unforge
{
    public class DataForgePropertyDefinition : _DataForgeSerializable
    {
        public uint NameOffset { get; set; }
        public string Name { get { return DocumentRoot.ValueMap[NameOffset]; } }
        public ushort StructIndex { get; set; }
        public EDataType DataType { get; set; }
        public EConversionType ConversionType { get; set; }
        public ushort Padding { get; set; }

        public DataForgePropertyDefinition(DataForge documentRoot) : base(documentRoot)
        {
            NameOffset = _br.ReadUInt32();
            StructIndex = _br.ReadUInt16();
            DataType = (EDataType)_br.ReadUInt16();
            ConversionType = (EConversionType)_br.ReadUInt16();
            Padding = _br.ReadUInt16();
        }

        public XmlAttribute Read()
        {
            XmlAttribute attribute = DocumentRoot.CreateAttribute(Name);

            switch (DataType)
            {
                case EDataType.varReference:
                    attribute.Value = string.Format("{2}", DataType, _br.ReadUInt32(), _br.ReadGuid(false));
                    break;
                case EDataType.varLocale:
                    attribute.Value = string.Format("{1}", DataType, DocumentRoot.ValueMap[_br.ReadUInt32()]);
                    break;
                case EDataType.varStrongPointer:
                    attribute.Value = string.Format("{0}:{1:X8} {2:X8}", DataType, _br.ReadUInt32(), _br.ReadUInt32());
                    break;
                case EDataType.varWeakPointer:
                    uint structIndex = _br.ReadUInt32();
                    uint itemIndex = _br.ReadUInt32();
                    attribute.Value = string.Format("{0}:{1:X8} {1:X8}", DataType, structIndex, itemIndex);
                    DocumentRoot.Require_WeakMapping2.Add(new ClassMapping { Node = attribute, StructIndex = (ushort)structIndex, RecordIndex = (int)itemIndex });
                    break;
                case EDataType.varString:
                    attribute.Value = string.Format("{1}", DataType, DocumentRoot.ValueMap[_br.ReadUInt32()]);
                    break;
                case EDataType.varBoolean:
                    attribute.Value = string.Format("{1}", DataType, _br.ReadByte());
                    break;
                case EDataType.varSingle:
                    attribute.Value = string.Format("{1}", DataType, _br.ReadSingle());
                    break;
                case EDataType.varDouble:
                    attribute.Value = string.Format("{1}", DataType, _br.ReadDouble());
                    break;
                case EDataType.varGuid:
                    attribute.Value = string.Format("{1}", DataType, _br.ReadGuid(false));
                    break;
                case EDataType.varSByte:
                    attribute.Value = string.Format("{1}", DataType, _br.ReadSByte());
                    break;
                case EDataType.varInt16:
                    attribute.Value = string.Format("{1}", DataType, _br.ReadInt16());
                    break;
                case EDataType.varInt32:
                    attribute.Value = string.Format("{1}", DataType, _br.ReadInt32());
                    break;
                case EDataType.varInt64:
                    attribute.Value = string.Format("{1}", DataType, _br.ReadInt64());
                    break;
                case EDataType.varByte:
                    attribute.Value = string.Format("{1}", DataType, _br.ReadByte());
                    break;
                case EDataType.varUInt16:
                    attribute.Value = string.Format("{1}", DataType, _br.ReadUInt16());
                    break;
                case EDataType.varUInt32:
                    attribute.Value = string.Format("{1}", DataType, _br.ReadUInt32());
                    break;
                case EDataType.varUInt64:
                    attribute.Value = string.Format("{1}", DataType, _br.ReadUInt64());
                    break;
                case EDataType.varEnum:
                    var enumDefinition = DocumentRoot.EnumDefinitionTable[StructIndex];
                    attribute.Value = string.Format("{1}", enumDefinition.Name, DocumentRoot.ValueMap[_br.ReadUInt32()]);
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
