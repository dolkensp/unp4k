using System.Xml;

namespace unforge;

internal class DataForgeStringLookup : DataForgeSerializable<string>
{
    private static string TryValue;

    internal DataForgeStringLookup(DataForgeIndex index) : base(index, index.ValueMap.TryGetValue(index.Reader.ReadUInt32(), out TryValue) ? TryValue : string.Empty) { }

    internal override XmlElement Serialise()
    {
        XmlElement element = Index.Writer.CreateElement("String");
        XmlAttribute attribute = Index.Writer.CreateAttribute("value");
        attribute.Value = Value;
        element.Attributes.Append(attribute);
        return element;
    }
}