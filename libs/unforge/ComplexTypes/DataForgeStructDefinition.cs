using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace unforge;
internal class DataForgeStructDefinition : DataForgeSerializable
{
    internal string Name => Index.ValueMap[NameOffset];
    internal uint NameOffset { get; set; }
    internal uint ParentTypeIndex { get; set; }
    internal ushort AttributeCount { get; set; }
    internal ushort FirstAttributeIndex { get; set; }
    internal uint NodeType { get; set; }

    internal DataForgeStructDefinition(DataForgeIndex index) : base(index)
    {
        NameOffset = Index.Reader.ReadUInt32();
        ParentTypeIndex = Index.Reader.ReadUInt32();
        AttributeCount = Index.Reader.ReadUInt16();
        FirstAttributeIndex = Index.Reader.ReadUInt16();
        NodeType = Index.Reader.ReadUInt32();
    }

    internal override async Task Serialise(string name = null)
    {
        DataForgeStructDefinition baseStruct = this;
        List<DataForgePropertyDefinition> properties = new();

        properties.InsertRange(0,
            from index in Enumerable.Range(FirstAttributeIndex, AttributeCount)
            let property = Index.PropertyDefinitionTable[index]
            select property);

        while (baseStruct.ParentTypeIndex != 0xFFFFFFFF)
        {
            baseStruct = Index.StructDefinitionTable[(int)baseStruct.ParentTypeIndex];
            properties.InsertRange(0,
                from index in Enumerable.Range(baseStruct.FirstAttributeIndex, baseStruct.AttributeCount)
                let property = Index.PropertyDefinitionTable[index]
                select property);
        }

        await Index.Writer.WriteStartElementAsync(null, name ?? baseStruct.Name, null); // Master Element
        foreach (DataForgePropertyDefinition node in
            properties.Where(x => (EConversionType)((int)x.ConversionType & 0xFF) is EConversionType.varAttribute &&
            x.DataType is not EDataType.varClass &&
            x.DataType is not EDataType.varStrongPointer))
            await node.ReadAttribute(Index.Writer);
        await Index.Writer.WriteAttributeStringAsync(null, "__type", null, baseStruct.Name);
        if (ParentTypeIndex != 0xFFFFFFFF) await Index.Writer.WriteAttributeStringAsync(null, "__polymorphicType", null, Name);

        foreach (DataForgePropertyDefinition node in properties)
        {
            node.ConversionType = (EConversionType)((int)node.ConversionType & 0xFF);
            if (node.ConversionType is EConversionType.varAttribute)
            {
                if (node.DataType is EDataType.varClass)
                {
                    DataForgeStructDefinition dataStruct = Index.StructDefinitionTable[node.StructIndex];
                    await Index.Writer.WriteStartElementAsync(null, node.Name, null);
                    await dataStruct.Serialise();
                    await Index.Writer.WriteEndElementAsync();
                }
                else if (node.DataType is EDataType.varStrongPointer)
                {
                    await Index.Writer.WriteStartElementAsync(null, node.Name, null);
                    await Index.Writer.WriteStartElementAsync(null, node.DataType.ToString(), null);
                    await Index.Writer.WriteEndElementAsync();
                    await Index.Writer.WriteEndElementAsync();
                }
            }
            else
            {
                int arrayCount = (int)Index.Reader.ReadUInt32();
                int firstIndex = (int)Index.Reader.ReadUInt32();
                await Index.Writer.WriteStartElementAsync(null, node.Name, null);
                for (int i = 0; i < arrayCount; i++)
                {
                    switch (node.DataType)
                    {
                        case EDataType.varBoolean:
                            await Index.BooleanValues[firstIndex + i].Serialise();
                            break;
                        case EDataType.varDouble:
                            await Index.DoubleValues[firstIndex + i].Serialise();
                            break;
                        case EDataType.varEnum:
                            await Index.EnumValues[firstIndex + i].Serialise();
                            break;
                        case EDataType.varGuid:
                            await Index.GuidValues[firstIndex + i].Serialise();
                            break;
                        case EDataType.varInt16:
                            await Index.Int16Values[firstIndex + i].Serialise();
                            break;
                        case EDataType.varInt32:
                            await Index.Int32Values[firstIndex + i].Serialise();
                            break;
                        case EDataType.varInt64:
                            await Index.Int64Values[firstIndex + i].Serialise();
                            break;
                        case EDataType.varSByte:
                            await Index.Int8Values[firstIndex + i].Serialise();
                            break;
                        case EDataType.varLocale:
                            await Index.LocaleValues[firstIndex + i].Serialise();
                            break;
                        case EDataType.varReference:
                            await Index.ReferenceValues[firstIndex + i].Serialise();
                            break;
                        case EDataType.varSingle:
                            await Index.SingleValues[firstIndex + i].Serialise();
                            break;
                        case EDataType.varString:
                            await Index.StringValues[firstIndex + i].Serialise();
                            break;
                        case EDataType.varUInt16:
                            await Index.UInt16Values[firstIndex + i].Serialise();
                            break;
                        case EDataType.varUInt32:
                            await Index.UInt32Values[firstIndex + i].Serialise();
                            break;
                        case EDataType.varUInt64:
                            await Index.UInt64Values[firstIndex + i].Serialise();
                            break;
                        case EDataType.varByte:
                            await Index.UInt8Values[firstIndex + i].Serialise();
                            break;
                        case EDataType.varClass:
                            await Index.Writer.WriteStartElementAsync(null, node.DataType.ToString(), null);
                            await Index.Writer.WriteEndElementAsync();
                            break;
                        case EDataType.varStrongPointer:
                            await Index.Writer.WriteStartElementAsync(null, node.DataType.ToString(), null);
                            await Index.Writer.WriteEndElementAsync();
                            break;
                        case EDataType.varWeakPointer:
                            await Index.Writer.WriteStartElementAsync(null, "WeakPointer", null);
                            await Index.Writer.WriteAttributeStringAsync(null, node.Name, null, null);
                            await Index.Writer.WriteEndElementAsync();
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
                await Index.Writer.WriteEndElementAsync();
            }
        }
        await Index.Writer.WriteEndElementAsync(); // MasterElement
    }
}