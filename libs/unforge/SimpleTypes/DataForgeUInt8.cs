using System.Xml;

namespace unforge;

internal class DataForgeUInt8 : DataForgeSerializable<byte>
{
    internal DataForgeUInt8(DataForgeIndex index) : base(index, index.Reader.ReadByte()) { }

    internal override XmlElement Serialise()
    {
        XmlElement element = Index.Writer.CreateElement("UInt8");
        XmlAttribute attribute = Index.Writer.CreateAttribute("value");
        attribute.Value = Value.ToString();
        element.Attributes.Append(attribute);
        return element;
    }
}
