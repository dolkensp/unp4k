using System;
using System.Xml;

namespace unforge
{
	public class DataForgeStringLookup : _DataForgeSerializable
    {
        private UInt32 _value;
        public String Value { get { return this.DocumentRoot.TextMap[this._value]; } }

        public DataForgeStringLookup(DataForge documentRoot)
            : base(documentRoot)
        {
            this._value = this._br.ReadUInt32();
        }

        public override String ToString()
        {
            return this.Value;
        }

        public XmlElement Read()
        {
            var element = this.DocumentRoot.CreateElement("String");
            var attribute = this.DocumentRoot.CreateAttribute("value");
            attribute.Value = this.Value;
            element.Attributes.Append(attribute);
            return element;
        }
    }
}
