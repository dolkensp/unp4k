using System.Xml;

namespace unforge;

internal class DataForgeDouble : DataForgeSerializable<double>
{
    internal DataForgeDouble(DataForgeIndex index) : base(index, index.Reader.ReadDouble()) { }

    internal override XmlElement Serialise()
    {
        XmlElement element = Index.Writer.CreateElement("Double");
        XmlAttribute attribute = Index.Writer.CreateAttribute("value");
        attribute.Value = Value.ToString();
        element.Attributes.Append(attribute);
        return element;
    }
}