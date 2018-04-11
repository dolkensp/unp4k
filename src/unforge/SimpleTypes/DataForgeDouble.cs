using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace unforge
{
    public class DataForgeDouble : _DataForgeSerializable
    {
        public Double Value { get; set; }

        public DataForgeDouble(DataForge documentRoot)
            : base(documentRoot)
        {
            this.Value = this._br.ReadDouble();
        }

        public override String ToString()
        {
            return String.Format("{0}", this.Value);
        }

        public XmlElement Read()
        {
            var element = this.DocumentRoot.CreateElement("Double");
            var attribute = this.DocumentRoot.CreateAttribute("value");
            attribute.Value = this.Value.ToString();
            element.Attributes.Append(attribute);
            return element;
        }
    }
}
