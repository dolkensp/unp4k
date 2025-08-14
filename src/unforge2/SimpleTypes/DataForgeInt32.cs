using System;
using System.Xml;

namespace unforge
{
	public class DataForgeInt32 : _DataForgeSerializable
    {
        public Int32 Value { get; set; }

        public DataForgeInt32(DataForge documentRoot)
            : base(documentRoot)
        {
            this.Value = this._br.ReadInt32();
        }

        public override String ToString()
        {
            return String.Format("{0}", this.Value);
        }

        public XmlElement Read()
        {
            var element = this.DocumentRoot.CreateElement("Int32");
            var attribute = this.DocumentRoot.CreateAttribute("value");
            attribute.Value = this.Value.ToString();
            element.Attributes.Append(attribute);
            return element;
        }
    }
}
