using System.Xml;
using System.Threading.Tasks;

namespace unforge;
public class DataForgeLocale : DataForgeSerializable
{
    private uint _value;
    public string Value { get { return DocumentRoot.ValueMap[_value]; } }

    public DataForgeLocale(DataForgeInstancePackage documentRoot) : base(documentRoot) { _value = Br.ReadUInt32(); }

    public override string ToString() => Value;

    public async Task Read(XmlWriter writer)
    {
        await writer.WriteStartElementAsync(null, "LocID", null);
        await writer.WriteAttributeStringAsync(null, "Value", null, Value.ToString());
        await writer.WriteEndElementAsync();
    }
}