using System.Xml;
using System.Threading.Tasks;

namespace unforge
{
    public class DataForgeInt32 : DataForgeSerializable
    {
        public int Value { get; set; }

        public DataForgeInt32(DataForgeInstancePackage documentRoot) : base(documentRoot) { Value = Br.ReadInt32(); }

        public override string ToString() => string.Format("{0}", Value);

        public async Task Read(XmlWriter writer)
        {
            await writer.WriteStartElementAsync(null, "Int32", null);
            await writer.WriteAttributeStringAsync(null, "Value", null, Value.ToString());
            await writer.WriteEndElementAsync();
        }
    }
}
