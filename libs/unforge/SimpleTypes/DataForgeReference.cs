using System;
using System.IO;
using System.Xml;

namespace unforge
{
    public class DataForgeReference : DataForgeSerializable
    {
        public uint Item1 { get; set; }
        public Guid Value { get; set; }

        public DataForgeReference(DataForge documentRoot) : base(documentRoot)
        {
            Item1 = br.ReadUInt32();
            Value = br.ReadGuid(false).Value;
        }

        public override string ToString() => string.Format("0x{0:X8} 0x{1}", Item1, Value);

        public XmlElement Read()
        {
            XmlElement element = DocumentRoot.CreateElement("Reference");
            XmlAttribute attribute = DocumentRoot.CreateAttribute("value");
            // TODO: More work here
            attribute.Value = $"{Value}";
            element.Attributes.Append(attribute);
            return element;
        }
    }
}
