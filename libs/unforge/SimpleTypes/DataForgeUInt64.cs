using System.Xml;
using System.Threading.Tasks;

namespace unforge
{
    public class DataForgeUInt64 : DataForgeSerializable
    {
        public ulong Value { get; set; }

        public DataForgeUInt64(DataForgeInstancePackage documentRoot) : base(documentRoot) { Value = Br.ReadUInt64(); }

        public override string ToString() => string.Format("{0}", Value);

        public async Task Read(XmlWriter writer)
        {
            await writer.WriteStartElementAsync(null, "UInt64", null);
            await writer.WriteAttributeStringAsync(null, "Value", null, Value.ToString());
            await writer.WriteEndElementAsync();
        }
    }
}
