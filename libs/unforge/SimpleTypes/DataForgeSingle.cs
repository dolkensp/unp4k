using System.Xml;

namespace unforge
{
    public class DataForgeSingle : DataForgeSerializable
    {
        public float Value { get; set; }

        public DataForgeSingle(DataForge documentRoot) : base(documentRoot) { Value = br.ReadSingle(); }

        public override string ToString() => string.Format("{0}", Value);

        public XmlElement Read()
        {
            XmlElement element = DocumentRoot.CreateElement("Single");
            XmlAttribute attribute = DocumentRoot.CreateAttribute("value");
            attribute.Value = Value.ToString();
            element.Attributes.Append(attribute);
            return element;
        }
    }
}
