using System.Xml;

namespace unforge
{
    public class DataForgeInt16 : _DataForgeSerializable
    {
        public short Value { get; set; }

        public DataForgeInt16(DataForge documentRoot) : base(documentRoot) { Value = _br.ReadInt16(); }

        public override string ToString() => string.Format("{0}", Value);

        public XmlElement Read()
        {
            XmlElement element = DocumentRoot.CreateElement("Int16");
            XmlAttribute attribute = DocumentRoot.CreateAttribute("value");
            attribute.Value = Value.ToString();
            element.Attributes.Append(attribute);
            return element;
        }
    }
}
