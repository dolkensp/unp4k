using System.Xml;

namespace unforge;

internal class DataForgeGuid : DataForgeSerializable<Guid>
{
    internal DataForgeGuid(DataForgeIndex index) : base(index, index.Reader.ReadGuid(false).Value) { }

    internal override XmlElement Serialise()
    {
        XmlElement element = Index.Writer.CreateElement("Guid");
        XmlAttribute attribute = Index.Writer.CreateAttribute("value");
        attribute.Value = Value.ToString();
        element.Attributes.Append(attribute);
        return element;
    }
}