using System.Xml;
using System.Threading.Tasks;

namespace unforge;
public class DataForgeUInt8 : DataForgeSerializable
{
    public byte Value { get; set; }

    public DataForgeUInt8(DataForgeInstancePackage documentRoot) : base(documentRoot) { Value = Br.ReadByte(); }

    public override string ToString() => string.Format("{0}", Value);

    public async Task Read(XmlWriter writer)
    {
        await writer.WriteStartElementAsync(null, "UInt8", null);
        await writer.WriteAttributeStringAsync(null, "Value", null, Value.ToString());
        await writer.WriteEndElementAsync();
    }
}
