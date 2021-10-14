using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace unforge
{
    public class DataForgeStructDefinition : _DataForgeSerializable
    {
        public uint NameOffset { get; set; }
        public string Name { get { return DocumentRoot.ValueMap[NameOffset]; } }

        public string __parentTypeIndex { get { return string.Format("{0:X4}", ParentTypeIndex); } }
        public uint ParentTypeIndex { get; set; }

        public string __attributeCount { get { return string.Format("{0:X4}", AttributeCount); } }
        public ushort AttributeCount { get; set; }

        public string __firstAttributeIndex { get { return string.Format("{0:X4}", FirstAttributeIndex); } }
        public ushort FirstAttributeIndex { get; set; }

        public string __nodeType { get { return string.Format("{0:X4}", NodeType); } }
        public uint NodeType { get; set; }

        public DataForgeStructDefinition(DataForge documentRoot) : base(documentRoot)
        {
            NameOffset = _br.ReadUInt32();
            ParentTypeIndex = _br.ReadUInt32();
            AttributeCount = _br.ReadUInt16();
            FirstAttributeIndex = _br.ReadUInt16();
            NodeType = _br.ReadUInt32();
        }

        public XmlElement Read(string name = null)
        {
            XmlAttribute attribute;
            DataForgeStructDefinition baseStruct = this;
            List<DataForgePropertyDefinition> properties = new() { };

            // TODO: Do we need to handle property overrides

            properties.InsertRange(0,
                from index in Enumerable.Range(FirstAttributeIndex, AttributeCount)
                let property = DocumentRoot.PropertyDefinitionTable[index]
                // where !properties.Select(p => p.Name).Contains(property.Name)
                select property);

            while (baseStruct.ParentTypeIndex != 0xFFFFFFFF)
            {
                baseStruct = DocumentRoot.StructDefinitionTable[baseStruct.ParentTypeIndex];
                properties.InsertRange(0,
                    from index in Enumerable.Range(baseStruct.FirstAttributeIndex, baseStruct.AttributeCount)
                    let property = DocumentRoot.PropertyDefinitionTable[index]
                    // where !properties.Contains(property)
                    select property);
            }

            XmlElement element = DocumentRoot.CreateElement(name ?? baseStruct.Name);
            foreach (DataForgePropertyDefinition node in properties)
            {
                node.ConversionType = (EConversionType)((int)node.ConversionType & 0xFF);
                if (node.ConversionType == EConversionType.varAttribute)
                {
                    if (node.DataType == EDataType.varClass)
                    {
                        DataForgeStructDefinition dataStruct = DocumentRoot.StructDefinitionTable[node.StructIndex];
                        XmlElement child = dataStruct.Read(node.Name);
                        element.AppendChild(child);
                    }
                    else if (node.DataType == EDataType.varStrongPointer)
                    {
                        XmlElement parentSP = DocumentRoot.CreateElement(node.Name);
                        XmlElement emptySP = DocumentRoot.CreateElement(string.Format("{0}", node.DataType));
                        parentSP.AppendChild(emptySP);
                        element.AppendChild(parentSP);
                        DocumentRoot.Require_ClassMapping.Add(new ClassMapping { Node = emptySP, StructIndex = (ushort)_br.ReadUInt32(), RecordIndex = (int)_br.ReadUInt32() });
                    }
                    else
                    {
                        XmlAttribute childAttribute = node.Read();
                        element.Attributes.Append(childAttribute);
                    }
                }
                else
                {
                    uint arrayCount = _br.ReadUInt32();
                    uint firstIndex = _br.ReadUInt32();
                    XmlElement child = DocumentRoot.CreateElement(node.Name);
                    for (int i = 0; i < arrayCount; i++)
                    {
                        switch (node.DataType)
                        {
                            case EDataType.varBoolean:
                                child.AppendChild(DocumentRoot.Array_BooleanValues[firstIndex + i].Read());
                                break;
                            case EDataType.varDouble:
                                child.AppendChild(DocumentRoot.Array_DoubleValues[firstIndex + i].Read());
                                break;
                            case EDataType.varEnum:
                                child.AppendChild(DocumentRoot.Array_EnumValues[firstIndex + i].Read());
                                break;
                            case EDataType.varGuid:
                                child.AppendChild(DocumentRoot.Array_GuidValues[firstIndex + i].Read());
                                break;
                            case EDataType.varInt16:
                                child.AppendChild(DocumentRoot.Array_Int16Values[firstIndex + i].Read());
                                break;
                            case EDataType.varInt32:
                                child.AppendChild(DocumentRoot.Array_Int32Values[firstIndex + i].Read());
                                break;
                            case EDataType.varInt64:
                                child.AppendChild(DocumentRoot.Array_Int64Values[firstIndex + i].Read());
                                break;
                            case EDataType.varSByte:
                                child.AppendChild(DocumentRoot.Array_Int8Values[firstIndex + i].Read());
                                break;
                            case EDataType.varLocale:
                                child.AppendChild(DocumentRoot.Array_LocaleValues[firstIndex + i].Read());
                                break;
                            case EDataType.varReference:
                                child.AppendChild(DocumentRoot.Array_ReferenceValues[firstIndex + i].Read());
                                break;
                            case EDataType.varSingle:
                                child.AppendChild(DocumentRoot.Array_SingleValues[firstIndex + i].Read());
                                break;
                            case EDataType.varString:
                                child.AppendChild(DocumentRoot.Array_StringValues[firstIndex + i].Read());
                                break;
                            case EDataType.varUInt16:
                                child.AppendChild(DocumentRoot.Array_UInt16Values[firstIndex + i].Read());
                                break;
                            case EDataType.varUInt32:
                                child.AppendChild(DocumentRoot.Array_UInt32Values[firstIndex + i].Read());
                                break;
                            case EDataType.varUInt64:
                                child.AppendChild(DocumentRoot.Array_UInt64Values[firstIndex + i].Read());
                                break;
                            case EDataType.varByte:
                                child.AppendChild(DocumentRoot.Array_UInt8Values[firstIndex + i].Read());
                                break;
                            case EDataType.varClass:
                                XmlElement emptyC = DocumentRoot.CreateElement(string.Format("{0}", node.DataType));
                                child.AppendChild(emptyC);
                                DocumentRoot.Require_ClassMapping.Add(new ClassMapping { Node = emptyC, StructIndex = node.StructIndex, RecordIndex = (int)(firstIndex + i) });
                                break;
                            case EDataType.varStrongPointer:
                                XmlElement emptySP = DocumentRoot.CreateElement(string.Format("{0}", node.DataType));
                                child.AppendChild(emptySP);
                                DocumentRoot.Require_StrongMapping.Add(new ClassMapping { Node = emptySP, StructIndex = node.StructIndex, RecordIndex = (int)(firstIndex + i) });
                                break;
                            case EDataType.varWeakPointer:
                                XmlElement weakPointerElement = DocumentRoot.CreateElement("WeakPointer");
                                XmlAttribute weakPointerAttribute = DocumentRoot.CreateAttribute(node.Name);
                                weakPointerElement.Attributes.Append(weakPointerAttribute);
                                child.AppendChild(weakPointerElement);
                                DocumentRoot.Require_WeakMapping1.Add(new ClassMapping { Node = weakPointerAttribute, StructIndex = node.StructIndex, RecordIndex = (int)(firstIndex + i) });
                                break;
                            default:
                                throw new NotImplementedException();

                                // var tempe = DocumentRoot.CreateElement(string.Format("{0}", node.DataType));
                                // var tempa = DocumentRoot.CreateAttribute("__child");
                                // tempa.Value = (firstIndex + i).Tostring();
                                // tempe.Attributes.Append(tempa);
                                // var tempb = DocumentRoot.CreateAttribute("__parent");
                                // tempb.Value = node.StructIndex.Tostring();
                                // tempe.Attributes.Append(tempb);
                                // child.AppendChild(tempe);
                                // break;
                        }
                    }
                    element.AppendChild(child);
                }
            }
            attribute = DocumentRoot.CreateAttribute("__type");
            attribute.Value = baseStruct.Name;
            element.Attributes.Append(attribute);

            if (ParentTypeIndex != 0xFFFFFFFF)
            {
                attribute = DocumentRoot.CreateAttribute("__polymorphicType");
                attribute.Value = Name;
                element.Attributes.Append(attribute);
            }
            return element;
        }

        public string Export(string assemblyName = "HoloXPLOR.Data.DataForge")
        {
            StringBuilder sb = new();

            sb.AppendLine(@"using System;");
            sb.AppendLine(@"using System.Xml.Serialization;");
            sb.AppendLine(@"");
            sb.AppendFormat(@"namespace {0}", assemblyName);
            sb.AppendLine();
            sb.AppendLine(@"{");
            sb.AppendFormat(@"    [XmlRoot(ElementName = ""{0}"")]", Name);
            sb.AppendLine();
            sb.AppendFormat(@"    public partial class {0}", Name);
            if (ParentTypeIndex != 0xFFFFFFFF) sb.AppendFormat(" : {0}", DocumentRoot.StructDefinitionTable[ParentTypeIndex].Name);
            sb.AppendLine();
            sb.AppendLine(@"    {");

            for (uint i = FirstAttributeIndex, j = (uint)(FirstAttributeIndex + AttributeCount); i < j; i++)
            {
                DataForgePropertyDefinition property = DocumentRoot.PropertyDefinitionTable[i];
                property.ConversionType = (EConversionType)((int)property.ConversionType | 0x6900);
                string arraySuffix = string.Empty;
                switch (property.ConversionType)
                {
                    case EConversionType.varAttribute:
                        if (property.DataType == EDataType.varClass) sb.AppendFormat(@"        [XmlElement(ElementName = ""{0}"")]", property.Name);
                        else if (property.DataType == EDataType.varStrongPointer)
                        {
                            sb.AppendFormat(@"        [XmlArray(ElementName = ""{0}"")]", property.Name);
                            arraySuffix = "[]";
                        }
                        else sb.AppendFormat(@"        [XmlAttribute(AttributeName = ""{0}"")]", property.Name);
                        break;
                    case EConversionType.varComplexArray:
                    case EConversionType.varSimpleArray:
                        sb.AppendFormat(@"        [XmlArray(ElementName = ""{0}"")]", property.Name);
                        arraySuffix = "[]";
                        break;
                }

                sb.AppendLine();
                var arrayPrefix = "";
                if (arraySuffix == "[]")
                {
                    if (property.DataType == EDataType.varClass || property.DataType == EDataType.varStrongPointer) sb.Append(property.Export());
                    else if (property.DataType == EDataType.varEnum)
                    {
                        arrayPrefix = "_";
                        sb.AppendFormat(@"        [XmlArrayItem(ElementName = ""Enum"", Type=typeof(_{0}))]", DocumentRoot.EnumDefinitionTable[property.StructIndex].Name);
                        sb.AppendLine();
                    }
                    else if (property.DataType == EDataType.varSByte)
                    {
                        arrayPrefix = "_";
                        sb.AppendFormat(@"        [XmlArrayItem(ElementName = ""Int8"", Type=typeof(_{0}))]", property.DataType.ToString().Replace("var", ""));
                        sb.AppendLine();
                    }
                    else if (property.DataType == EDataType.varByte)
                    {
                        arrayPrefix = "_";
                        sb.AppendFormat(@"        [XmlArrayItem(ElementName = ""UInt8"", Type=typeof(_{0}))]", property.DataType.ToString().Replace("var", ""));
                        sb.AppendLine();
                    }
                    else
                    {
                        arrayPrefix = "_";
                        sb.AppendFormat(@"        [XmlArrayItem(ElementName = ""{0}"", Type=typeof(_{0}))]", property.DataType.ToString().Replace("var", ""));
                        sb.AppendLine();
                    }
                }

                HashSet<string> keywords = new()
                {
                    "Dynamic",
                    "Int16",
                    "int",
                    "Int64",
                    "ushort",
                    "uint",
                    "UInt64",
                    "Double",
                    "Single",
                };
                string propertyName = property.Name;
                propertyName = string.Format("{0}{1}", propertyName[0].ToString().ToUpper(), propertyName.Substring(1));
                if (keywords.Contains(propertyName)) propertyName = string.Format("@{0}", propertyName);

                switch (property.DataType)
                {
                    case EDataType.varClass:
                    case EDataType.varStrongPointer:
                        sb.AppendFormat("        public {0}{2} {1} {{ get; set; }}", DocumentRoot.StructDefinitionTable[property.StructIndex].Name, propertyName, arraySuffix);
                        break;
                    case EDataType.varEnum:
                        sb.AppendFormat("        public {3}{0}{2} {1} {{ get; set; }}", DocumentRoot.EnumDefinitionTable[property.StructIndex].Name, propertyName, arraySuffix, arrayPrefix);
                        break;
                    case EDataType.varReference:
                        if (arraySuffix == "[]") sb.AppendFormat("        public {3}{0}{2} {1} {{ get; set; }}", property.DataType.ToString().Replace("var", ""), propertyName, arraySuffix, arrayPrefix);
                        else sb.AppendFormat("        public Guid{2} {1} {{ get; set; }}", DocumentRoot.StructDefinitionTable[property.StructIndex].Name, propertyName, arraySuffix);
                        break;
                    case EDataType.varLocale:
                    case EDataType.varWeakPointer:
                        if (arraySuffix == "[]") sb.AppendFormat("        public {3}{0}{2} {1} {{ get; set; }}", property.DataType.ToString().Replace("var", ""), propertyName, arraySuffix, arrayPrefix);
                        else sb.AppendFormat("        public string{2} {1} {{ get; set; }}", DocumentRoot.StructDefinitionTable[property.StructIndex].Name, propertyName, arraySuffix);
                        break;
                    default:
                        sb.AppendFormat("        public {3}{0}{2} {1} {{ get; set; }}", property.DataType.ToString().Replace("var", ""), propertyName, arraySuffix, arrayPrefix);
                        break;
                }
                sb.AppendLine();
                sb.AppendLine();
            }
            sb.AppendLine(@"    }");
            sb.AppendLine(@"}");
            return sb.ToString();
        }

        public override string Tostring() => string.Format("<{0} />", Name);
    }
}
