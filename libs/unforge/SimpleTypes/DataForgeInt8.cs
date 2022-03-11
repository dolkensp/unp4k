using System.Xml;
using System.Threading.Tasks;

namespace unforge;
public class DataForgeInt8 : DataForgeSerializable
{
    public sbyte Value { get; set; }

    public DataForgeInt8(DataForgeInstancePackage documentRoot) : base(documentRoot) { Value = Br.ReadSByte(); }

    public override string ToString() => string.Format("{0}", Value);

    public async Task Read(XmlWriter writer)
    {
        await writer.WriteStartElementAsync(null, "Int8", null);
        await writer.WriteAttributeStringAsync(null, "Value", null, Value.ToString());
        await writer.WriteEndElementAsync();
    }
}