using System;
using System.Xml;

namespace unforge
{
	public class DataForgePointer : _DataForgeSerializable
    {
        public UInt32 StructType { get; set; }
        public UInt32 Index { get; set; }

        public DataForgePointer(DataForge documentRoot)
            : base(documentRoot)
        {
            this.StructType = this._br.ReadUInt32();
            this.Index = this._br.ReadUInt32();
        }

        public override String ToString()
        {
            return String.Format("0x{0:X8} 0x{1:X8}", this.StructType, this.Index);
        }

        public XmlElement Read()
        {
            var element = this.DocumentRoot.CreateElement("Pointer");

            var attribute = this.DocumentRoot.CreateAttribute("typeIndex");
            attribute.Value = String.Format("{0:X4}", this.StructType);
            element.Attributes.Append(attribute);

            attribute = this.DocumentRoot.CreateAttribute("firstIndex");
            attribute.Value = String.Format("{0:X4}", this.Index);
            element.Attributes.Append(attribute);

            return element;
        }
    }
}
