using System;
using System.Xml;

namespace unforge
{
	public class DataForgeInt64 : _DataForgeSerializable
    {
        public Int64 Value { get; set; }

        public DataForgeInt64(DataForge documentRoot)
            : base(documentRoot)
        {
            this.Value = this._br.ReadInt64();
        }

        public override String ToString()
        {
            return String.Format("{0}", this.Value);
        }

        public XmlElement Read()
        {
            var element = this.DocumentRoot.CreateElement("Int64");
            var attribute = this.DocumentRoot.CreateAttribute("value");
            attribute.Value = this.Value.ToString();
            element.Attributes.Append(attribute);
            return element;
        }
    }
}
