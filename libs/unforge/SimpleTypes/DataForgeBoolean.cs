using System.Xml;

namespace unforge;

internal class DataForgeBoolean : DataForgeSerializable<bool>
{
    internal DataForgeBoolean(DataForgeIndex index) : base(index, index.Reader.ReadBoolean()) { }

    internal override XmlElement Serialise()
    {
        XmlElement element = Index.Writer.CreateElement("Bool");
        XmlAttribute attribute = Index.Writer.CreateAttribute("value");
        attribute.Value = Convert.ToInt32(Value).ToString();
        element.Attributes.Append(attribute);
        return element;
    }
}