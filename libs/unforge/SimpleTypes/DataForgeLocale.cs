using System.Xml;

namespace unforge
{
    public class DataForgeLocale : _DataForgeSerializable
    {
        private uint _value;
        public string Value { get { return DocumentRoot.ValueMap[_value]; } }

        public DataForgeLocale(DataForge documentRoot)  : base(documentRoot) { _value = _br.ReadUInt32(); }

        public override string ToString() => Value;

        public XmlElement Read()
        {
            XmlElement element = DocumentRoot.CreateElement("LocID");
            XmlAttribute attribute = DocumentRoot.CreateAttribute("value");
            attribute.Value = Value.ToString();
            // TODO: More work here
            element.Attributes.Append(attribute);
            return element;
        }
    }
}
