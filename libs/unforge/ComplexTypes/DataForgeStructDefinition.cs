using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Threading.Tasks;

namespace unforge;
public class DataForgeStructDefinition : DataForgeSerializable
{
    internal uint NameOffset { get; set; }
    internal string Name { get { return DocumentRoot.ValueMap[NameOffset]; } }

    internal string __parentTypeIndex { get { return string.Format("{0:X4}", ParentTypeIndex); } }
    internal uint ParentTypeIndex { get; set; }

    internal string __attributeCount { get { return string.Format("{0:X4}", AttributeCount); } }
    internal ushort AttributeCount { get; set; }

    internal string __firstAttributeIndex { get { return string.Format("{0:X4}", FirstAttributeIndex); } }
    internal ushort FirstAttributeIndex { get; set; }

    internal string __nodeType { get { return string.Format("{0:X4}", NodeType); } }
    internal uint NodeType { get; set; }

    public DataForgeStructDefinition(DataForgeInstancePackage documentRoot) : base(documentRoot)
    {
        NameOffset = Br.ReadUInt32();
        ParentTypeIndex = Br.ReadUInt32();
        AttributeCount = Br.ReadUInt16();
        FirstAttributeIndex = Br.ReadUInt16();
        NodeType = Br.ReadUInt32();
    }

    public async Task Read(XmlWriter writer, string name = null)
    {
        DataForgeStructDefinition baseStruct = this;
        List<DataForgePropertyDefinition> properties = new();

        properties.InsertRange(0,
            from index in Enumerable.Range(FirstAttributeIndex, AttributeCount)
            let property = DocumentRoot.PropertyDefinitionTable[index]
            select property);

        while (baseStruct.ParentTypeIndex != 0xFFFFFFFF)
        {
            baseStruct = DocumentRoot.StructDefinitionTable[baseStruct.ParentTypeIndex];
            properties.InsertRange(0,
                from index in Enumerable.Range(baseStruct.FirstAttributeIndex, baseStruct.AttributeCount)
                let property = DocumentRoot.PropertyDefinitionTable[index]
                select property);
        }

        await writer.WriteStartElementAsync(null, name ?? baseStruct.Name, null); // Master Element
        foreach (DataForgePropertyDefinition node in properties.Where(x => (EConversionType)((int)x.ConversionType & 0xFF) is EConversionType.varAttribute &&
            x.DataType is not EDataType.varClass && x.DataType is not EDataType.varStrongPointer)) await node.ReadAttribute(writer);
        await writer.WriteAttributeStringAsync(null, "__type", null, baseStruct.Name);
        if (ParentTypeIndex != 0xFFFFFFFF) await writer.WriteAttributeStringAsync(null, "__polymorphicType", null, Name);
        foreach (DataForgePropertyDefinition node in properties)
        {
            node.ConversionType = (EConversionType)((int)node.ConversionType & 0xFF);
            if (node.ConversionType is EConversionType.varAttribute)
            {
                if (node.DataType is EDataType.varClass)
                {
                    DataForgeStructDefinition dataStruct = DocumentRoot.StructDefinitionTable[node.StructIndex];
                    await writer.WriteStartElementAsync(null, node.Name, null);
                    await dataStruct.Read(writer);
                    await writer.WriteEndElementAsync();
                }
                else if (node.DataType is EDataType.varStrongPointer)
                {
                    await writer.WriteStartElementAsync(null, node.Name, null);
                    await writer.WriteStartElementAsync(null, node.DataType.ToString(), null);
                    await writer.WriteEndElementAsync();
                    await writer.WriteEndElementAsync();
                }
            }
            else
            {
                uint arrayCount = Br.ReadUInt32();
                uint firstIndex = Br.ReadUInt32();
                await writer.WriteStartElementAsync(null, node.Name, null);
                for (int i = 0; i < arrayCount; i++)
                {
                    switch (node.DataType)
                    {
                        case EDataType.varBoolean:
                            await DocumentRoot.Array_BooleanValues[firstIndex + i].Read(writer);
                            break;
                        case EDataType.varDouble:
                            await DocumentRoot.Array_DoubleValues[firstIndex + i].Read(writer);
                            break;
                        case EDataType.varEnum:
                            await DocumentRoot.Array_EnumValues[firstIndex + i].Read(writer);
                            break;
                        case EDataType.varGuid:
                            await DocumentRoot.Array_GuidValues[firstIndex + i].Read(writer);
                            break;
                        case EDataType.varInt16:
                            await DocumentRoot.Array_Int16Values[firstIndex + i].Read(writer);
                            break;
                        case EDataType.varInt32:
                            await DocumentRoot.Array_Int32Values[firstIndex + i].Read(writer);
                            break;
                        case EDataType.varInt64:
                            await DocumentRoot.Array_Int64Values[firstIndex + i].Read(writer);
                            break;
                        case EDataType.varSByte:
                            await DocumentRoot.Array_Int8Values[firstIndex + i].Read(writer);
                            break;
                        case EDataType.varLocale:
                            await DocumentRoot.Array_LocaleValues[firstIndex + i].Read(writer);
                            break;
                        case EDataType.varReference:
                            await DocumentRoot.Array_ReferenceValues[firstIndex + i].Read(writer);
                            break;
                        case EDataType.varSingle:
                            await DocumentRoot.Array_SingleValues[firstIndex + i].Read(writer);
                            break;
                        case EDataType.varString:
                            await DocumentRoot.Array_StringValues[firstIndex + i].Read(writer);
                            break;
                        case EDataType.varUInt16:
                            await DocumentRoot.Array_UInt16Values[firstIndex + i].Read(writer);
                            break;
                        case EDataType.varUInt32:
                            await DocumentRoot.Array_UInt32Values[firstIndex + i].Read(writer);
                            break;
                        case EDataType.varUInt64:
                            await DocumentRoot.Array_UInt64Values[firstIndex + i].Read(writer);
                            break;
                        case EDataType.varByte:
                            await DocumentRoot.Array_UInt8Values[firstIndex + i].Read(writer);
                            break;
                        case EDataType.varClass:
                            await writer.WriteStartElementAsync(null, node.DataType.ToString(), null);
                            await writer.WriteEndElementAsync();
                            break;
                        case EDataType.varStrongPointer:
                            await writer.WriteStartElementAsync(null, node.DataType.ToString(), null);
                            await writer.WriteEndElementAsync();
                            break;
                        case EDataType.varWeakPointer:
                            await writer.WriteStartElementAsync(null, "WeakPointer", null);
                            await writer.WriteAttributeStringAsync(null, node.Name, null, null);
                            await writer.WriteEndElementAsync();
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
                await writer.WriteEndElementAsync();
            }
        }
        await writer.WriteEndElementAsync(); // MasterElement
    }

    public override string ToString() => string.Format("<{0} />", Name);
}