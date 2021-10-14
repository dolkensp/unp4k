using System.Xml;

namespace unforge
{
    public class DataForgeUInt8 : _DataForgeSerializable
    {
        public byte Value { get; set; }

        public DataForgeUInt8(DataForge documentRoot) : base(documentRoot) { Value = _br.ReadByte(); }

        public override string ToString() => string.Format("{0}", Value);

        public XmlElement Read()
        {
            XmlElement element = DocumentRoot.CreateElement("UInt8");
            XmlAttribute attribute = DocumentRoot.CreateAttribute("value");
            attribute.Value = Value.ToString();
            element.Attributes.Append(attribute);
            return element;
        }
    }
}
