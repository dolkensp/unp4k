using System.Xml;
using System.Threading.Tasks;

namespace unforge;
public class DataForgeStringLookup : DataForgeSerializable
{
    private uint _value;
    public string Value { get { return DocumentRoot.ValueMap[_value]; } }

    public DataForgeStringLookup(DataForgeIndex documentRoot) : base(documentRoot) { _value = Br.ReadUInt32(); }

    public override string ToString() => Value;

    public async Task Read(XmlWriter writer)
    {
        await writer.WriteStartElementAsync(null, "String", null);
        await writer.WriteAttributeStringAsync(null, "Value", null, Value);
        await writer.WriteEndElementAsync();
    }
}