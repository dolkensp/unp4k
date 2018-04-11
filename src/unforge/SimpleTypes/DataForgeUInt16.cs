using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace unforge
{
    public class DataForgeUInt16 : _DataForgeSerializable
    {
        public UInt16 Value { get; set; }

        public DataForgeUInt16(DataForge documentRoot)
            : base(documentRoot)
        {
            this.Value = this._br.ReadUInt16();
        }

        public override String ToString()
        {
            return String.Format("{0}", this.Value);
        }

        public XmlElement Read()
        {
            var element = this.DocumentRoot.CreateElement("UInt16");
            var attribute = this.DocumentRoot.CreateAttribute("value");
            attribute.Value = this.Value.ToString();
            element.Attributes.Append(attribute);
            return element;
        }
    }
}
