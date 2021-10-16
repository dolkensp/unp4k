using System.Xml;

namespace unforge
{
    public class DataForgeInt8 : DataForgeSerializable
    {
        public sbyte Value { get; set; }

        public DataForgeInt8(DataForge documentRoot) : base(documentRoot) { Value = br.ReadSByte(); }

        public override string ToString() => string.Format("{0}", Value);

        public XmlElement Read()
        {
            XmlElement element = DocumentRoot.CreateElement("Int8");
            XmlAttribute attribute = DocumentRoot.CreateAttribute("value");
            attribute.Value = Value.ToString();
            element.Attributes.Append(attribute);
            return element;
        }
    }
}
