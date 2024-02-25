using System.Xml;

namespace unforge;

internal class DataForgeInt32 : DataForgeSerializable<int>
{
    internal DataForgeInt32(DataForgeIndex index) : base(index, index.Reader.ReadInt32()) { }

    internal override XmlElement Serialise()
    {
        XmlElement element = Index.Writer.CreateElement("Int32");
        XmlAttribute attribute = Index.Writer.CreateAttribute("value");
        attribute.Value = Value.ToString();
        element.Attributes.Append(attribute);
        return element;
    }
}