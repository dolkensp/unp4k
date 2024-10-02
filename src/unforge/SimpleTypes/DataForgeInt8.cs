using System;
using System.Xml;

namespace unforge
{
	public class DataForgeInt8 : _DataForgeSerializable
    {
        public SByte Value { get; set; }

        public DataForgeInt8(DataForge documentRoot)
            : base(documentRoot)
        {
            this.Value = this._br.ReadSByte();
        }

        public override String ToString()
        {
            return String.Format("{0}", this.Value);
        }

        public XmlElement Read()
        {
            var element = this.DocumentRoot.CreateElement("Int8");
            var attribute = this.DocumentRoot.CreateAttribute("value");
            attribute.Value = this.Value.ToString();
            element.Attributes.Append(attribute);
            return element;
        }
    }
}
