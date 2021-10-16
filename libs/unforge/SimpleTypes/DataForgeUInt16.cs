using System.Xml;

namespace unforge
{
    public class DataForgeUInt16 : DataForgeSerializable
    {
        public ushort Value { get; set; }

        public DataForgeUInt16(DataForge documentRoot) : base(documentRoot) { Value = br.ReadUInt16(); }

        public override string ToString() => string.Format("{0}", Value);

        public XmlElement Read()
        {
            XmlElement element = DocumentRoot.CreateElement("UInt16");
            XmlAttribute attribute = DocumentRoot.CreateAttribute("value");
            attribute.Value = Value.ToString();
            element.Attributes.Append(attribute);
            return element;
        }
    }
}
