using System.Xml;

namespace unforge;

internal class DataForgeUInt64 : DataForgeSerializable<ulong>
{
    internal DataForgeUInt64(DataForgeIndex index) : base(index, index.Reader.ReadUInt64()) { }

    internal override XmlElement Serialise()
    {
        XmlElement element = Index.Writer.CreateElement("UInt64");
        XmlAttribute attribute = Index.Writer.CreateAttribute("value");
        attribute.Value = Value.ToString();
        element.Attributes.Append(attribute);
        return element;
    }
}