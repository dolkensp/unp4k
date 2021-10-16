using System.Xml;

namespace unforge
{
    public class DataForgeDouble : DataForgeSerializable
    {
        public double Value { get; set; }

        public DataForgeDouble(DataForge documentRoot) : base(documentRoot) { Value = br.ReadDouble(); }

        public override string ToString() => string.Format("{0}", Value);

        public XmlElement Read()
        {
            XmlElement element = DocumentRoot.CreateElement("Double");
            XmlAttribute attribute = DocumentRoot.CreateAttribute("value");
            attribute.Value = Value.ToString();
            element.Attributes.Append(attribute);
            return element;
        }
    }
}
