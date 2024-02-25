using System.Xml;

namespace unforge;

internal class DataForgeSingle : DataForgeSerializable<float>
{
    internal DataForgeSingle(DataForgeIndex index) : base(index, index.Reader.ReadSingle()) { }

    internal override XmlElement Serialise()
    {
        XmlElement element = Index.Writer.CreateElement("Single");
        XmlAttribute attribute = Index.Writer.CreateAttribute("value");
        attribute.Value = Value.ToString();
        element.Attributes.Append(attribute);
        return element;
    }
}