using System.Xml;

namespace unforge;

internal class DataForgeInt8 : DataForgeSerializable<sbyte>
{
    internal DataForgeInt8(DataForgeIndex index) : base(index, index.Reader.ReadSByte()) { }

    internal override XmlElement Serialise()
    {
        XmlElement element = Index.Writer.CreateElement("Int8");
        XmlAttribute attribute = Index.Writer.CreateAttribute("value");
        attribute.Value = Value.ToString();
        element.Attributes.Append(attribute);
        return element;
    }
}