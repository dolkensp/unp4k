using System.Xml;

namespace unforge;

internal class DataForgeInt64 : DataForgeSerializable<long>
{
    internal DataForgeInt64(DataForgeIndex index) : base(index, index.Reader.ReadInt64()) { }

    internal override XmlElement Serialise()
    {
        XmlElement element = Index.Writer.CreateElement("Int64");
        XmlAttribute attribute = Index.Writer.CreateAttribute("value");
        attribute.Value = Value.ToString();
        element.Attributes.Append(attribute);
        return element;
    }
}