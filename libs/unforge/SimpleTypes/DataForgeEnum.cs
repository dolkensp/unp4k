using System.Xml;

namespace unforge;

internal class DataForgeEnum : DataForgeSerializable<string>
{
    private static string TryValue;

    internal DataForgeEnum(DataForgeIndex index) : base(index, index.ValueMap.TryGetValue(index.Reader.ReadUInt32(), out TryValue) ? TryValue : string.Empty) { }

    internal override XmlElement Serialise()
    {
        XmlElement element = Index.Writer.CreateElement("Enum");
        XmlAttribute attribute = Index.Writer.CreateAttribute("value");
        attribute.Value = Value;
        element.Attributes.Append(attribute);
        return element;
    }
}