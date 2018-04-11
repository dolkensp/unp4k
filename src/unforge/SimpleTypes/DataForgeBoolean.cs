using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace unforge
{
    public class DataForgeBoolean : _DataForgeSerializable
    {
        public Boolean Value { get; set; }

        public DataForgeBoolean(DataForge documentRoot)
            : base(documentRoot)
        {
            this.Value = this._br.ReadBoolean();
        }

        public override String ToString()
        {
            return String.Format("{0}", this.Value ? "1" : "0");
        }

        public XmlElement Read()
        {
            var element = this.DocumentRoot.CreateElement("Bool");
            var attribute = this.DocumentRoot.CreateAttribute("value");
            attribute.Value = this.Value ? "1" : "0";
            element.Attributes.Append(attribute);
            return element;
        }
    }
}
