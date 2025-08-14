using System;
using System.Xml;

namespace unforge
{
	public class DataForgeUInt32 : _DataForgeSerializable
    {
        public UInt32 Value { get; set; }

        public DataForgeUInt32(DataForge documentRoot)
            : base(documentRoot)
        {
            this.Value = this._br.ReadUInt32();
        }

        public override String ToString()
        {
            return String.Format("{0}", this.Value);
        }

        public XmlElement Read()
        {
            var element = this.DocumentRoot.CreateElement("UInt32");
            var attribute = this.DocumentRoot.CreateAttribute("value");
            attribute.Value = this.Value.ToString();
            element.Attributes.Append(attribute);
            return element;
        }
    }
}
