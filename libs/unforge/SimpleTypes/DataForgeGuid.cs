using System;
using System.IO;
using System.Xml;

namespace unforge
{
    public class DataForgeGuid : DataForgeSerializable
    {
        public Guid Value { get; set; }

        public DataForgeGuid(DataForge documentRoot)  : base(documentRoot) { Value = br.ReadGuid(false).Value; }

        public override string ToString() => Value.ToString();

        public XmlElement Read()
        {
            XmlElement element = DocumentRoot.CreateElement("Guid");
            XmlAttribute attribute = DocumentRoot.CreateAttribute("value");
            attribute.Value = Value.ToString();
            element.Attributes.Append(attribute);
            return element;
        }
    }
}
