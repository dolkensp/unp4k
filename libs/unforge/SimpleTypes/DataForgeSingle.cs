using System.Xml;
using System.Threading.Tasks;

namespace unforge
{
    public class DataForgeSingle : DataForgeSerializable
    {
        public float Value { get; set; }

        public DataForgeSingle(DataForgeInstancePackage documentRoot) : base(documentRoot) { Value = Br.ReadSingle(); }

        public override string ToString() => string.Format("{0}", Value);

        public async Task Read(XmlWriter writer)
        {
            await writer.WriteStartElementAsync(null, "Single", null);
            await writer.WriteAttributeStringAsync(null, "Value", null, Value.ToString());
            await writer.WriteEndElementAsync();
        }
    }
}
