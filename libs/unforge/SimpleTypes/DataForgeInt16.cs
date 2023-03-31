using System.Xml;

namespace unforge;

internal class DataForgeInt16 : DataForgeSerializable<short>
{
    internal DataForgeInt16(DataForgeIndex index) : base(index, index.Reader.ReadInt16()) { }

    internal override XmlElement Serialise()
    {
        XmlElement element = Index.Writer.CreateElement("Int16");
        XmlAttribute attribute = Index.Writer.CreateAttribute("value");
        attribute.Value = Value.ToString();
        element.Attributes.Append(attribute);
        return element;
    }
}