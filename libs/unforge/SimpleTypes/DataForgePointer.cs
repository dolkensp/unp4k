using System.Xml;

namespace unforge
{
    public class DataForgePointer : DataForgeSerializable
    {
        public uint StructType { get; set; }
        public uint Index { get; set; }

        public DataForgePointer(DataForge documentRoot) : base(documentRoot)
        {
            StructType = br.ReadUInt32();
            Index = br.ReadUInt32();
        }

        public override string ToString() => string.Format("0x{0:X8} 0x{1:X8}", StructType, Index);

        public XmlElement Read()
        {
            XmlElement element = DocumentRoot.CreateElement("Pointer");
            XmlAttribute attribute = DocumentRoot.CreateAttribute("typeIndex");
            attribute.Value = string.Format("{0:X4}", StructType);
            element.Attributes.Append(attribute);
            attribute = DocumentRoot.CreateAttribute("firstIndex");
            attribute.Value = string.Format("{0:X4}", Index);
            element.Attributes.Append(attribute);
            return element;
        }
    }
}
