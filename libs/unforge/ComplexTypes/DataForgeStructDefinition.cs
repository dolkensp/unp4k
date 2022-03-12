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

    internal DataForgeStructDefinition BaseStruct { get; set; }
    internal List<DataForgePropertyDefinition> Properties { get; set; } = new();
    internal string Attribute { get; set; }

    internal List<int> ArrayCounts { get; set; } = new();
    internal List<int> FirstIndexs { get; set; } = new();

    internal DataForgeStructDefinition(DataForgeIndex index) : base(index)
    {
        NameOffset = Index.Reader.ReadUInt32();
        ParentTypeIndex = Index.Reader.ReadUInt32();
        AttributeCount = Index.Reader.ReadUInt16();
        FirstAttributeIndex = Index.Reader.ReadUInt16();
        NodeType = Index.Reader.ReadUInt32();
    }

    internal override async Task PreSerialise()
    {
        BaseStruct = this;
        Properties.InsertRange(0, from index in Enumerable.Range(FirstAttributeIndex, AttributeCount) let property = Index.PropertyDefinitionTable[index] select property);
        while (BaseStruct.ParentTypeIndex != 0xFFFFFFFF)
        {
            BaseStruct = Index.StructDefinitionTable[(int)BaseStruct.ParentTypeIndex];
            Properties.InsertRange(0, from index in Enumerable.Range(BaseStruct.FirstAttributeIndex, BaseStruct.AttributeCount) let property = Index.PropertyDefinitionTable[index] select property);
        }
        foreach (DataForgePropertyDefinition node in Properties.Where(x => (EConversionType)((int)x.ConversionType & 0xFF) is EConversionType.varAttribute &&
            x.DataType is not EDataType.varClass &&
            x.DataType is not EDataType.varStrongPointer)) Attribute = node.ReadAttribute();
        foreach (DataForgePropertyDefinition node in Properties)
        {
            node.ConversionType = (EConversionType)((int)node.ConversionType & 0xFF);
            if (node.ConversionType is EConversionType.varAttribute)
            {
                if (node.DataType is EDataType.varClass) await Index.StructDefinitionTable[node.StructIndex].PreSerialise();
            }
            else
            {
                ArrayCounts.Add((int)Index.Reader.ReadUInt32());
                FirstIndexs.Add((int)Index.Reader.ReadUInt32());
            }
        }
    }

    internal override async Task Serialise(string name = null)
    {
        await Index.Writer.WriteStartElementAsync(null, name ?? BaseStruct.Name, null); // Master Element
        await Index.Writer.WriteAttributeStringAsync(null, Name, null, Attribute);
        await Index.Writer.WriteAttributeStringAsync(null, "__type", null, BaseStruct.Name);
        if (ParentTypeIndex != 0xFFFFFFFF) await Index.Writer.WriteAttributeStringAsync(null, "__polymorphicType", null, Name);

        foreach (DataForgePropertyDefinition node in Properties)
        {
            if (node.ConversionType is EConversionType.varAttribute)
            {
                if (node.DataType is EDataType.varClass)
                {
                    await Index.Writer.WriteStartElementAsync(null, node.Name, null);
                    await Index.StructDefinitionTable[node.StructIndex].Serialise();
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
                await Index.Writer.WriteStartElementAsync(null, node.Name, null);
                for (int n = 0; n < ArrayCounts.Count; n++)
                {
                    int x = FirstIndexs[n];
                    int y = ArrayCounts[n];
                    switch (node.DataType)
                        {
                            case EDataType.varBoolean:
                                await Index.BooleanValues[x + y].Serialise();
                                break;
                            case EDataType.varDouble:
                                await Index.DoubleValues[x + y].Serialise();
                                break;
                            case EDataType.varEnum:
                                await Index.EnumValues[x + y].Serialise();
                                break;
                            case EDataType.varGuid:
                                await Index.GuidValues[x + y].Serialise();
                                break;
                            case EDataType.varInt16:
                                await Index.Int16Values[x + y].Serialise();
                                break;
                            case EDataType.varInt32:
                                await Index.Int32Values[x + y].Serialise();
                                break;
                            case EDataType.varInt64:
                                await Index.Int64Values[x + y].Serialise();
                                break;
                            case EDataType.varSByte:
                                await Index.Int8Values[x + y].Serialise();
                                break;
                            case EDataType.varLocale:
                                await Index.LocaleValues[x + y].Serialise();
                                break;
                            case EDataType.varReference:
                                await Index.ReferenceValues[x + y].Serialise();
                                break;
                            case EDataType.varSingle:
                                await Index.SingleValues[x + y].Serialise();
                                break;
                            case EDataType.varString:
                                await Index.StringValues[x + y].Serialise();
                                break;
                            case EDataType.varUInt16:
                                await Index.UInt16Values[x + y].Serialise();
                                break;
                            case EDataType.varUInt32:
                                await Index.UInt32Values[x + y].Serialise();
                                break;
                            case EDataType.varUInt64:
                                await Index.UInt64Values[x + y].Serialise();
                                break;
                            case EDataType.varByte:
                                await Index.UInt8Values[x + y].Serialise();
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