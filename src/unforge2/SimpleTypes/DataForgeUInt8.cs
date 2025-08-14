using System;
using System.Xml;

namespace unforge
{
	public class DataForgeUInt8 : _DataForgeSerializable
    {
        public Byte Value { get; set; }

        public DataForgeUInt8(DataForge documentRoot)
            : base(documentRoot)
        {
            this.Value = this._br.ReadByte();
        }

        public override String ToString()
        {
            return String.Format("{0}", this.Value);
        }

        public XmlElement Read()
        {
            var element = this.DocumentRoot.CreateElement("UInt8");
            var attribute = this.DocumentRoot.CreateAttribute("value");
            attribute.Value = this.Value.ToString();
            element.Attributes.Append(attribute);
            return element;
        }
    }
}
