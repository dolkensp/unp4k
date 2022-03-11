using System.Xml;
using System.Threading.Tasks;

namespace unforge;
public class DataForgeInt64 : DataForgeSerializable
{
    public long Value { get; set; }

    public DataForgeInt64(DataForgeInstancePackage documentRoot) : base(documentRoot) { Value = Br.ReadInt64(); }

    public override string ToString() => string.Format("{0}", Value);

    public async Task Read(XmlWriter writer)
    {
        await writer.WriteStartElementAsync(null, "Int64", null);
        await writer.WriteAttributeStringAsync(null, "Value", null, Value.ToString());
        await writer.WriteEndElementAsync();
    }
}