using System.Xml;

namespace unforge;

internal class DataForgePointer : DataForgeSerializable<uint>
{
    internal uint StructType { get; set; }

    internal DataForgePointer(DataForgeIndex index) : base(index, 0) 
    { 
        StructType = index.Reader.ReadUInt32();
        Value = index.Reader.ReadUInt32();
    }

    internal override XmlElement Serialise()
    {
        XmlElement element = Index.Writer.CreateElement("Pointer");
        XmlAttribute attribute = Index.Writer.CreateAttribute("typeIndex");
        attribute.Value = $"{StructType:X4}";
        element.Attributes.Append(attribute);
        attribute = Index.Writer.CreateAttribute("firstIndex");
        attribute.Value = $"{Value:X4}";
        element.Attributes.Append(attribute);
        return element;
    }
}