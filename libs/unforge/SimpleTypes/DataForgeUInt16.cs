using System.Xml;
using System.Threading.Tasks;

namespace unforge;
public class DataForgeUInt16 : DataForgeSerializable
{
    public ushort Value { get; set; }

    public DataForgeUInt16(DataForgeIndex documentRoot) : base(documentRoot) { Value = Br.ReadUInt16(); }

    public override string ToString() => string.Format("{0}", Value);

    public async Task Read(XmlWriter writer)
    {
        await writer.WriteStartElementAsync(null, "UInt16", null);
        await writer.WriteAttributeStringAsync(null, "Value", null, Value.ToString());
        await writer.WriteEndElementAsync();
    }
}