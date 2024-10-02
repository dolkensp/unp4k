using System;
using System.Xml;

namespace unforge
{
	public class DataForgeInt16 : _DataForgeSerializable
    {
        public Int16 Value { get; set; }

        public DataForgeInt16(DataForge documentRoot)
            : base(documentRoot)
        {
            this.Value = this._br.ReadInt16();
        }

        public override String ToString()
        {
            return String.Format("{0}", this.Value);
        }

        public XmlElement Read()
        {
            var element = this.DocumentRoot.CreateElement("Int16");
            var attribute = this.DocumentRoot.CreateAttribute("value");
            attribute.Value = this.Value.ToString();
            element.Attributes.Append(attribute);
            return element;
        }
    }
}
