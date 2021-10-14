using System.Xml;

namespace unforge
{
    public class DataForgeInt32 : _DataForgeSerializable
    {
        public int Value { get; set; }

        public DataForgeInt32(DataForge documentRoot) : base(documentRoot) { Value = _br.ReadInt32(); }

        public override string ToString() => string.Format("{0}", Value);

        public XmlElement Read()
        {
            XmlElement element = DocumentRoot.CreateElement("Int32");
            XmlAttribute attribute = DocumentRoot.CreateAttribute("value");
            attribute.Value = Value.ToString();
            element.Attributes.Append(attribute);
            return element;
        }
    }
}
