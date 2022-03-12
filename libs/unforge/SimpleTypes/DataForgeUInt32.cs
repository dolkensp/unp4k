using System.Xml;

namespace unforge;

internal class DataForgeUInt32 : DataForgeSerializable<uint>
{
    internal DataForgeUInt32(DataForgeIndex index) : base(index, index.Reader.ReadUInt32()) { }

    internal override XmlElement Serialise()
    {
        XmlElement element = Index.Writer.CreateElement("UInt32");
        XmlAttribute attribute = Index.Writer.CreateAttribute("value");
        attribute.Value = Value.ToString();
        element.Attributes.Append(attribute);
        return element;
    }
}