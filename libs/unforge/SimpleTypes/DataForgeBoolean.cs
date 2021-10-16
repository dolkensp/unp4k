using System.Xml;

namespace unforge
{
    public class DataForgeBoolean : DataForgeSerializable
    {
        public bool Value { get; set; }

        public DataForgeBoolean(DataForge documentRoot) : base(documentRoot) { Value = br.ReadBoolean(); }

        public override string ToString() => string.Format("{0}", Value ? "1" : "0");

        public XmlElement Read()
        {
            XmlElement element = DocumentRoot.CreateElement("Bool");
            XmlAttribute attribute = DocumentRoot.CreateAttribute("value");
            attribute.Value = Value ? "1" : "0";
            element.Attributes.Append(attribute);
            return element;
        }
    }
}
