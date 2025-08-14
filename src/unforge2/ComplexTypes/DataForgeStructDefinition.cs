using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace unforge
{
	public class DataForgeStructDefinition : _DataForgeSerializable
    {
        public UInt32 NameOffset { get; set; }
        public String Name { get { return this.DocumentRoot.BlobMap[this.NameOffset]; } }

        public String __parentTypeIndex { get { return String.Format("{0:X4}", this.ParentTypeIndex); } }
        public UInt32 ParentTypeIndex { get; set; }

        public String __attributeCount { get { return String.Format("{0:X4}", this.AttributeCount); } }
        public UInt16 AttributeCount { get; set; }

        public String __firstAttributeIndex { get { return String.Format("{0:X4}", this.FirstAttributeIndex); } }
        public UInt16 FirstAttributeIndex { get; set; }

        public String __nodeType { get { return String.Format("{0:X4}", this.NodeType); } }
        public UInt32 NodeType { get; set; }

        public DataForgeStructDefinition(DataForge documentRoot)
            : base(documentRoot)
        {
            this.NameOffset = this._br.ReadUInt32();
            this.ParentTypeIndex = this._br.ReadUInt32();
            this.AttributeCount = this._br.ReadUInt16();
            this.FirstAttributeIndex = this._br.ReadUInt16();
            this.NodeType = this._br.ReadUInt32();
        }

        public XmlElement Read(String name = null)
        {
            XmlAttribute attribute;

            var baseStruct = this;
            var properties = new List<DataForgePropertyDefinition> { };

            // TODO: Do we need to handle property overrides

            properties.InsertRange(0,
                from index in Enumerable.Range(this.FirstAttributeIndex, this.AttributeCount)
                let property = this.DocumentRoot.PropertyDefinitionTable[index]
                // where !properties.Select(p => p.Name).Contains(property.Name)
                select property);

            while (baseStruct.ParentTypeIndex != 0xFFFFFFFF)
            {
                baseStruct = this.DocumentRoot.StructDefinitionTable[baseStruct.ParentTypeIndex];

                properties.InsertRange(0,
                    from index in Enumerable.Range(baseStruct.FirstAttributeIndex, baseStruct.AttributeCount)
                    let property = this.DocumentRoot.PropertyDefinitionTable[index]
                    // where !properties.Contains(property)
                    select property);
            }

            var element = this.DocumentRoot.CreateElement(name ?? baseStruct.Name);

            foreach (var node in properties)
            {
                node.ConversionType = (EConversionType)((Int32)node.ConversionType & 0xFF);

                if (node.ConversionType == EConversionType.varAttribute)
                {
                    if (node.DataType == EDataType.varClass)
                    {
                        var dataStruct = this.DocumentRoot.StructDefinitionTable[node.StructIndex];

                        var child = dataStruct.Read(node.Name);

                        element.AppendChild(child);
                    }
                    else if (node.DataType == EDataType.varStrongPointer)
                    {
                        var parentSP = this.DocumentRoot.CreateElement(node.Name);
                        var emptySP = this.DocumentRoot.CreateElement(String.Format("{0}", node.DataType));
                        parentSP.AppendChild(emptySP);
                        element.AppendChild(parentSP);
                        this.DocumentRoot.Require_ClassMapping.Add(new ClassMapping { Node = emptySP, StructIndex = (UInt16)this._br.ReadUInt32(), RecordIndex = (Int32)this._br.ReadUInt32() });
                    }
                    else
                    {
                        var childAttribute = node.Read();
                        element.Attributes.Append(childAttribute);
                    }
                }
                else
                {
                    var arrayCount = this._br.ReadUInt32();
                    var firstIndex = this._br.ReadUInt32();

                    var child = this.DocumentRoot.CreateElement(node.Name);

                    for (var i = 0; i < arrayCount; i++)
                    {
                        switch (node.DataType)
                        {
                            case EDataType.varBoolean:
                                child.AppendChild(this.DocumentRoot.Array_BooleanValues[firstIndex + i].Read());
                                break;
                            case EDataType.varDouble:
                                child.AppendChild(this.DocumentRoot.Array_DoubleValues[firstIndex + i].Read());
                                break;
                            case EDataType.varEnum:
                                child.AppendChild(this.DocumentRoot.Array_EnumValues[firstIndex + i].Read());
                                break;
                            case EDataType.varGuid:
                                child.AppendChild(this.DocumentRoot.Array_GuidValues[firstIndex + i].Read());
                                break;
                            case EDataType.varInt16:
                                child.AppendChild(this.DocumentRoot.Array_Int16Values[firstIndex + i].Read());
                                break;
                            case EDataType.varInt32:
                                child.AppendChild(this.DocumentRoot.Array_Int32Values[firstIndex + i].Read());
                                break;
                            case EDataType.varInt64:
                                child.AppendChild(this.DocumentRoot.Array_Int64Values[firstIndex + i].Read());
                                break;
                            case EDataType.varSByte:
                                child.AppendChild(this.DocumentRoot.Array_Int8Values[firstIndex + i].Read());
                                break;
                            case EDataType.varLocale:
                                child.AppendChild(this.DocumentRoot.Array_LocaleValues[firstIndex + i].Read());
                                break;
                            case EDataType.varReference:
                                child.AppendChild(this.DocumentRoot.Array_ReferenceValues[firstIndex + i].Read());
                                break;
                            case EDataType.varSingle:
                                child.AppendChild(this.DocumentRoot.Array_SingleValues[firstIndex + i].Read());
                                break;
                            case EDataType.varString:
                                child.AppendChild(this.DocumentRoot.Array_StringValues[firstIndex + i].Read());
                                break;
                            case EDataType.varUInt16:
                                child.AppendChild(this.DocumentRoot.Array_UInt16Values[firstIndex + i].Read());
                                break;
                            case EDataType.varUInt32:
                                child.AppendChild(this.DocumentRoot.Array_UInt32Values[firstIndex + i].Read());
                                break;
                            case EDataType.varUInt64:
                                child.AppendChild(this.DocumentRoot.Array_UInt64Values[firstIndex + i].Read());
                                break;
                            case EDataType.varByte:
                                child.AppendChild(this.DocumentRoot.Array_UInt8Values[firstIndex + i].Read());
                                break;
                            case EDataType.varClass:
                                var emptyC = this.DocumentRoot.CreateElement(String.Format("{0}", node.DataType));
                                child.AppendChild(emptyC);
                                this.DocumentRoot.Require_ClassMapping.Add(new ClassMapping { Node = emptyC, StructIndex = node.StructIndex, RecordIndex = (Int32)(firstIndex + i) });
                                break;
                            case EDataType.varStrongPointer:
                                var emptySP = this.DocumentRoot.CreateElement(String.Format("{0}", node.DataType));
                                child.AppendChild(emptySP);
                                this.DocumentRoot.Require_StrongMapping.Add(new ClassMapping { Node = emptySP, StructIndex = node.StructIndex, RecordIndex = (Int32)(firstIndex + i) });
                                break;
                            case EDataType.varWeakPointer:
                                var weakPointerElement = this.DocumentRoot.CreateElement("WeakPointer");
                                var weakPointerAttribute = this.DocumentRoot.CreateAttribute(node.Name);

                                weakPointerElement.Attributes.Append(weakPointerAttribute);
                                child.AppendChild(weakPointerElement);

                                this.DocumentRoot.Require_WeakMapping1.Add(new ClassMapping { Node = weakPointerAttribute, StructIndex = node.StructIndex, RecordIndex = (Int32)(firstIndex + i) });
                                break;
                            default:
                                throw new NotImplementedException();

                                // var tempe = this.DocumentRoot.CreateElement(String.Format("{0}", node.DataType));
                                // var tempa = this.DocumentRoot.CreateAttribute("__child");
                                // tempa.Value = (firstIndex + i).ToString();
                                // tempe.Attributes.Append(tempa);
                                // var tempb = this.DocumentRoot.CreateAttribute("__parent");
                                // tempb.Value = node.StructIndex.ToString();
                                // tempe.Attributes.Append(tempb);
                                // child.AppendChild(tempe);
                                // break;
                        }
                    }

                    element.AppendChild(child);
                }
            }

            attribute = this.DocumentRoot.CreateAttribute("__type");
            attribute.Value = baseStruct.Name;
            element.Attributes.Append(attribute);

            if (this.ParentTypeIndex != 0xFFFFFFFF)
            {
                attribute = this.DocumentRoot.CreateAttribute("__polymorphicType");
                attribute.Value = this.Name;
                element.Attributes.Append(attribute);
            }

            return element;
        }

        public String Export(String assemblyName = "HoloXPLOR.Data.DataForge")
        {
            var sb = new StringBuilder();

            sb.AppendLine(@"using System;");
            sb.AppendLine(@"using System.Xml.Serialization;");
            sb.AppendLine(@"");
            sb.AppendFormat(@"namespace {0}", assemblyName);
            sb.AppendLine();
            sb.AppendLine(@"{");
            sb.AppendFormat(@"    [XmlRoot(ElementName = ""{0}"")]", this.Name);
            sb.AppendLine();
            sb.AppendFormat(@"    public partial class {0}", this.Name);
            if (this.ParentTypeIndex != 0xFFFFFFFF)
            {
                sb.AppendFormat(" : {0}", this.DocumentRoot.StructDefinitionTable[this.ParentTypeIndex].Name);
            }
            sb.AppendLine();
            sb.AppendLine(@"    {");

            for (UInt32 i = this.FirstAttributeIndex, j = (UInt32)(this.FirstAttributeIndex + this.AttributeCount); i < j; i++)
            {
                var property = this.DocumentRoot.PropertyDefinitionTable[i];
                property.ConversionType = (EConversionType)((Int32)property.ConversionType | 0x6900);

                var arraySuffix = String.Empty;

                switch (property.ConversionType)
                {
                    case EConversionType.varAttribute:
                        if (property.DataType == EDataType.varClass)
                        {
                            sb.AppendFormat(@"        [XmlElement(ElementName = ""{0}"")]", property.Name);
                        }
                        else if (property.DataType == EDataType.varStrongPointer)
                        {
                            sb.AppendFormat(@"        [XmlArray(ElementName = ""{0}"")]", property.Name);
                            arraySuffix = "[]";
                        }
                        else
                        {
                            sb.AppendFormat(@"        [XmlAttribute(AttributeName = ""{0}"")]", property.Name);
                        }
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
                    if (property.DataType == EDataType.varClass || property.DataType == EDataType.varStrongPointer)
                    {
                        sb.Append(property.Export());
                    }
                    else if (property.DataType == EDataType.varEnum)
                    {
                        arrayPrefix = "_";
                        sb.AppendFormat(@"        [XmlArrayItem(ElementName = ""Enum"", Type=typeof(_{0}))]", this.DocumentRoot.EnumDefinitionTable[property.StructIndex].Name);
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

                var keywords = new HashSet<String>
                {
                    "Dynamic",
                    "Int16",
                    "Int32",
                    "Int64",
                    "UInt16",
                    "UInt32",
                    "UInt64",
                    "Double",
                    "Single",
                };
                var propertyName = property.Name;

                propertyName = String.Format("{0}{1}", propertyName[0].ToString().ToUpper(), propertyName.Substring(1));

                if (keywords.Contains(propertyName))
                {
                    propertyName = String.Format("@{0}", propertyName);
                }

                switch (property.DataType)
                {
                    case EDataType.varClass:
                    case EDataType.varStrongPointer:
                        sb.AppendFormat("        public {0}{2} {1} {{ get; set; }}", this.DocumentRoot.StructDefinitionTable[property.StructIndex].Name, propertyName, arraySuffix);
                        break;
                    case EDataType.varEnum:
                        sb.AppendFormat("        public {3}{0}{2} {1} {{ get; set; }}", this.DocumentRoot.EnumDefinitionTable[property.StructIndex].Name, propertyName, arraySuffix, arrayPrefix);
                        break;
                    case EDataType.varReference:
                        if (arraySuffix == "[]")
                        {
                            sb.AppendFormat("        public {3}{0}{2} {1} {{ get; set; }}", property.DataType.ToString().Replace("var", ""), propertyName, arraySuffix, arrayPrefix);
                        }
                        else
                        {
                            sb.AppendFormat("        public Guid{2} {1} {{ get; set; }}", this.DocumentRoot.StructDefinitionTable[property.StructIndex].Name, propertyName, arraySuffix);
                        }
                        break;
                    case EDataType.varLocale:
                    case EDataType.varWeakPointer:
                        if (arraySuffix == "[]")
                        {
                            sb.AppendFormat("        public {3}{0}{2} {1} {{ get; set; }}", property.DataType.ToString().Replace("var", ""), propertyName, arraySuffix, arrayPrefix);
                        }
                        else
                        {
                            sb.AppendFormat("        public String{2} {1} {{ get; set; }}", this.DocumentRoot.StructDefinitionTable[property.StructIndex].Name, propertyName, arraySuffix);
                        }
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

        public override String ToString()
        {
            return String.Format("<{0} />", this.Name);
        }
    }
}
