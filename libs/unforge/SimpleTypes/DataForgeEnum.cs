using System.Xml;

namespace unforge
{
    public class DataForgeEnum : DataForgeSerializable
    {
        private uint _value;
        public string Value { get { return DocumentRoot.ValueMap[_value]; } }

        public DataForgeEnum(DataForge documentRoot) : base(documentRoot) { _value = br.ReadUInt32(); }

        public override string ToString() => Value;

        public XmlElement Read()
        {
            XmlElement element = DocumentRoot.CreateElement("Enum");
            XmlAttribute attribute = DocumentRoot.CreateAttribute("value");
            attribute.Value = Value;
            element.Attributes.Append(attribute);
            return element;
        }
    }
}
