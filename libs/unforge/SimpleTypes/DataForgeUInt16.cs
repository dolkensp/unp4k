using System.Xml;

namespace unforge;

internal class DataForgeUInt16 : DataForgeSerializable<ushort>
{
    internal DataForgeUInt16(DataForgeIndex index) : base(index, index.Reader.ReadUInt16()) { }

    internal override XmlElement Serialise()
    {
        XmlElement element = Index.Writer.CreateElement("UInt16");
        XmlAttribute attribute = Index.Writer.CreateAttribute("value");
        attribute.Value = Value.ToString();
        element.Attributes.Append(attribute);
        return element;
    }
}