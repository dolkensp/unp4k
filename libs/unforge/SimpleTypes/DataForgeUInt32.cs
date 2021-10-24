using System.Xml;
using System.Threading.Tasks;

namespace unforge
{
    public class DataForgeUInt32 : DataForgeSerializable
    {
        public uint Value { get; set; }

        public DataForgeUInt32(DataForgeInstancePackage documentRoot) : base(documentRoot) { Value = Br.ReadUInt32(); }

        public override string ToString() => string.Format("{0}", Value);

        public async Task Read(XmlWriter writer)
        {
            await writer.WriteStartElementAsync(null, "UInt32", null);
            await writer.WriteAttributeStringAsync(null, "Value", null, Value.ToString());
            await writer.WriteEndElementAsync();
        }
    }
}
