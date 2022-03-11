using System.Xml;
using System.Threading.Tasks;

namespace unforge;
public class DataForgeBoolean : DataForgeSerializable
{
    public bool Value { get; set; }

    public DataForgeBoolean(DataForgeInstancePackage documentRoot) : base(documentRoot) { Value = Br.ReadBoolean(); }

    public override string ToString() => string.Format("{0}", Value ? "1" : "0");

    public async Task Read(XmlWriter writer)
    {
        await writer.WriteStartElementAsync(null, "Bool", null);
        await writer.WriteAttributeStringAsync(null, "Value", null, Value ? "1" : "0");
        await writer.WriteEndElementAsync();
    }
}