using System.Xml;
using System.Threading.Tasks;

namespace unforge;
public class DataForgeDouble : DataForgeSerializable
{
    public double Value { get; set; }

    public DataForgeDouble(DataForgeInstancePackage documentRoot) : base(documentRoot) { Value = Br.ReadDouble(); }

    public override string ToString() => string.Format("{0}", Value);

    public async Task Read(XmlWriter writer)
    {
        await writer.WriteStartElementAsync(null, "Double", null);
        await writer.WriteAttributeStringAsync(null, "Value", null, Value.ToString());
        await writer.WriteEndElementAsync();
    }
}