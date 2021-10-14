using System.Xml;

namespace unforge
{
    public class DataForgeStringLookup : DataForgeSerializable
    {
        private uint _value;
        public string Value { get { return DocumentRoot.ValueMap[_value]; } }

        public DataForgeStringLookup(DataForge documentRoot) : base(documentRoot) { _value = _br.ReadUInt32(); }

        public override string ToString() => Value;

        public XmlElement Read()
        {
            XmlElement element = DocumentRoot.CreateElement("String");
            XmlAttribute attribute = DocumentRoot.CreateAttribute("value");
            attribute.Value = Value;
            element.Attributes.Append(attribute);
            return element;
        }
    }
}
