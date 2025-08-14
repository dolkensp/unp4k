using System;
using System.Xml;

namespace unforge
{
	public class DataForgeSingle : _DataForgeSerializable
    {
        public Single Value { get; set; }

        public DataForgeSingle(DataForge documentRoot)
            : base(documentRoot)
        {
            this.Value = this._br.ReadSingle();
        }

        public override String ToString()
        {
            return String.Format("{0}", this.Value);
        }

        public XmlElement Read()
        {
            var element = this.DocumentRoot.CreateElement("Single");
            var attribute = this.DocumentRoot.CreateAttribute("value");
            attribute.Value = this.Value.ToString();
            element.Attributes.Append(attribute);
            return element;
        }
    }
}
