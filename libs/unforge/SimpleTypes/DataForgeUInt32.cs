using System.Xml;

namespace unforge
{
    public class DataForgeUInt32 : DataForgeSerializable
    {
        public uint Value { get; set; }

        public DataForgeUInt32(DataForge documentRoot) : base(documentRoot) { Value = br.ReadUInt32(); }

        public override string ToString() => string.Format("{0}", Value);

        public XmlElement Read()
        {
            XmlElement element = DocumentRoot.CreateElement("UInt32");
            XmlAttribute attribute = DocumentRoot.CreateAttribute("value");
            attribute.Value = Value.ToString();
            element.Attributes.Append(attribute);
            return element;
        }
    }
}
