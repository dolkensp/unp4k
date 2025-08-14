using System;
using System.Xml;

namespace unforge
{
	public class DataForgeLocale : _DataForgeSerializable
    {
        private UInt32 _value;
        public String Value
		{
			get
			{
				return this.DocumentRoot.BlobMap.ContainsKey(this._value) ?
					this.DocumentRoot.BlobMap[this._value] :
					this.DocumentRoot.TextMap.ContainsKey(this._value) ?
					this.DocumentRoot.TextMap[this._value] :
					"[MISSING]";
			}
		}

        public DataForgeLocale(DataForge documentRoot)
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
            var element = this.DocumentRoot.CreateElement("LocID");
            var attribute = this.DocumentRoot.CreateAttribute("value");
            attribute.Value = this.Value;
            // TODO: More work here
            element.Attributes.Append(attribute);
            return element;
        }
    }
}
