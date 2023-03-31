using System.Xml;

namespace unforge;

internal class DataForgeReference : DataForgeSerializable<Guid>
{
    internal uint Item { get; private set; }

    internal DataForgeReference(DataForgeIndex index) : base(index, index.Reader.ReadGuid(false).Value) { Item = index.Reader.ReadUInt32(); }

    internal override XmlElement Serialise()
    {
        XmlElement element = Index.Writer.CreateElement("Reference");
        XmlAttribute attribute = Index.Writer.CreateAttribute("value");
        attribute.Value = $"{Value}";
        element.Attributes.Append(attribute);
        return element;
    }
}