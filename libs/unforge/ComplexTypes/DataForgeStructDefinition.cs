using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace unforge;

internal class DataForgeStructDefinition : DataForgeSerializable
{
    internal string Name => Index.ValueMap[NameOffset];
    internal uint NameOffset { get; set; }

    internal string __parentTypeIndex => $"{ParentTypeIndex:X4}";
    internal uint ParentTypeIndex { get; set; }

    internal string __attributeCount => $"{AttributeCount:X4}";
    internal ushort AttributeCount { get; set; }

    internal string __firstAttributeIndex => $"{FirstAttributeIndex:X4}";
    internal ushort FirstAttributeIndex { get; set; }

    internal string __nodeType => $"{NodeType:X4}";
    internal uint NodeType { get; set; }

    internal DataForgeStructDefinition(DataForgeIndex index) : base(index)
    {
        NameOffset = Index.Reader.ReadUInt32();
        ParentTypeIndex = Index.Reader.ReadUInt32();
        AttributeCount = Index.Reader.ReadUInt16();
        FirstAttributeIndex = Index.Reader.ReadUInt16();
        NodeType = Index.Reader.ReadUInt32();
    }

    internal override Task PreSerialise() => Task.CompletedTask;

    internal override XmlElement Serialise(string name = null)
    {
        XmlAttribute attribute;
        DataForgeStructDefinition baseStruct = this;
        List<DataForgePropertyDefinition> properties = new();
        properties.InsertRange(0,
            from index in Enumerable.Range(FirstAttributeIndex, AttributeCount)
            let property = Index.PropertyDefinitionTable[index]
            select property);
        while (baseStruct.ParentTypeIndex != 0xFFFFFFFF)
        {
            baseStruct = Index.StructDefinitionTable[baseStruct.ParentTypeIndex];
            properties.InsertRange(0,
                from index in Enumerable.Range(baseStruct.FirstAttributeIndex, baseStruct.AttributeCount)
                let property = Index.PropertyDefinitionTable[index]
                select property);
        }

        XmlElement element = Index.Writer.CreateElement(name ?? baseStruct.Name);
        properties.ForEach(node => 
        {
            node.ConversionType = (EConversionType)((int)node.ConversionType & 0xFF);
            if (node.ConversionType == EConversionType.varAttribute)
            {
                if (node.DataType == EDataType.varClass)
                {
                    DataForgeStructDefinition dataStruct = Index.StructDefinitionTable[node.StructIndex];
                    XmlElement child = dataStruct.Serialise(node.Name);
                    element.AppendChild(child);
                }
                else if (node.DataType == EDataType.varStrongPointer)
                {
                    XmlElement parentSP = Index.Writer.CreateElement(node.Name);
                    XmlElement emptySP = Index.Writer.CreateElement(node.DataType.ToString());
                    parentSP.AppendChild(emptySP);
                    element.AppendChild(parentSP);
                    Index.Require_ClassMapping.Add(new ClassMapping { Node = emptySP, StructIndex = (ushort)Index.Reader.ReadUInt32(), RecordIndex = (int)Index.Reader.ReadUInt32() });
                }
                else
                {
                    XmlAttribute childAttribute = node.Serialise();
                    element.Attributes.Append(childAttribute);
                }
            }
            else
            {
                int arrayCount = (int)Index.Reader.ReadUInt32();
                int firstIndex = (int)Index.Reader.ReadUInt32();
                XmlElement child = Index.Writer.CreateElement(node.Name);
                for (int i = 0; i < arrayCount; i++)
                {
                    switch (node.DataType)
                    {
                        case EDataType.varBoolean:
                            child.AppendChild(Index.BooleanValues[firstIndex + i].Serialise());
                            break;
                        case EDataType.varDouble:
                            child.AppendChild(Index.DoubleValues[firstIndex + i].Serialise());
                            break;
                        case EDataType.varEnum:
                            child.AppendChild(Index.EnumValues[firstIndex + i].Serialise());
                            break;
                        case EDataType.varGuid:
                            child.AppendChild(Index.GuidValues[firstIndex + i].Serialise());
                            break;
                        case EDataType.varInt16:
                            child.AppendChild(Index.Int16Values[firstIndex + i].Serialise());
                            break;
                        case EDataType.varInt32:
                            child.AppendChild(Index.Int32Values[firstIndex + i].Serialise());
                            break;
                        case EDataType.varInt64:
                            child.AppendChild(Index.Int64Values[firstIndex + i].Serialise());
                            break;
                        case EDataType.varSByte:
                            child.AppendChild(Index.Int8Values[firstIndex + i].Serialise());
                            break;
                        case EDataType.varLocale:
                            child.AppendChild(Index.LocaleValues[firstIndex + i].Serialise());
                            break;
                        case EDataType.varReference:
                            child.AppendChild(Index.ReferenceValues[firstIndex + i].Serialise());
                            break;
                        case EDataType.varSingle:
                            child.AppendChild(Index.SingleValues[firstIndex + i].Serialise());
                            break;
                        case EDataType.varString:
                            child.AppendChild(Index.StringValues[firstIndex + i].Serialise());
                            break;
                        case EDataType.varUInt16:
                            child.AppendChild(Index.UInt16Values[firstIndex + i].Serialise());
                            break;
                        case EDataType.varUInt32:
                            child.AppendChild(Index.UInt32Values[firstIndex + i].Serialise());
                            break;
                        case EDataType.varUInt64:
                            child.AppendChild(Index.UInt64Values[firstIndex + i].Serialise());
                            break;
                        case EDataType.varByte:
                            child.AppendChild(Index.UInt8Values[firstIndex + i].Serialise());
                            break;
                        case EDataType.varClass:
                            XmlElement emptyC = Index.Writer.CreateElement(node.DataType.ToString());
                            child.AppendChild(emptyC);
                            Index.Require_ClassMapping.Add(new ClassMapping { Node = emptyC, StructIndex = node.StructIndex, RecordIndex = firstIndex + i });
                            break;
                        case EDataType.varStrongPointer:
                            XmlElement emptySP = Index.Writer.CreateElement(node.DataType.ToString());
                            child.AppendChild(emptySP);
                            Index.Require_StrongMapping.Add(new ClassMapping { Node = emptySP, StructIndex = node.StructIndex, RecordIndex = firstIndex + i });
                            break;
                        case EDataType.varWeakPointer:
                            XmlElement weakPointerElement = Index.Writer.CreateElement("WeakPointer");
                            XmlAttribute weakPointerAttribute = Index.Writer.CreateAttribute(node.Name);
                            weakPointerElement.Attributes.Append(weakPointerAttribute);
                            child.AppendChild(weakPointerElement);
                            Index.Require_WeakMapping1.Add(new ClassMapping { Node = weakPointerAttribute, StructIndex = node.StructIndex, RecordIndex = firstIndex + i });
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
                element.AppendChild(child);
            }
        });
        attribute = Index.Writer.CreateAttribute("__type");
        attribute.Value = baseStruct.Name;
        element.Attributes.Append(attribute);
        if (ParentTypeIndex != 0xFFFFFFFF)
        {
            attribute = Index.Writer.CreateAttribute("__polymorphicType");
            attribute.Value = Name;
            element.Attributes.Append(attribute);
        }
        return element;
    }
}