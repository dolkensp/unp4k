using System.Xml;
using System.Threading.Tasks;

namespace unforge;
public class DataForgeInt16 : DataForgeSerializable
{
    public short Value { get; set; }

    public DataForgeInt16(DataForgeInstancePackage documentRoot) : base(documentRoot) { Value = Br.ReadInt16(); }

    public override string ToString() => string.Format("{0}", Value);

    public async Task Read(XmlWriter writer)
    {
        await writer.WriteStartElementAsync(null, "Int16", null);
        await writer.WriteAttributeStringAsync(null, "Value", null, Value.ToString());
        await writer.WriteEndElementAsync();
    }
}