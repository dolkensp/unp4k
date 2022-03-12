using System.Xml;

namespace unforge;

internal class DataForgeLocale : DataForgeSerializable<string>
{
    private static string TryValue;

    internal DataForgeLocale(DataForgeIndex index) : base(index, index.ValueMap.TryGetValue(index.Reader.ReadUInt32(), out TryValue) ? TryValue : string.Empty) { }

    internal override XmlElement Serialise()
    {
        XmlElement element = Index.Writer.CreateElement("LocID");
        XmlAttribute attribute = Index.Writer.CreateAttribute("value");
        attribute.Value = Value.ToString();
        element.Attributes.Append(attribute);
        return element;
    }
}