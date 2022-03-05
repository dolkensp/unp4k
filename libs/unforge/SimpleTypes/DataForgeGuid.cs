using System;
using System.IO;
using System.Xml;
using System.Threading.Tasks;

namespace unforge;
public class DataForgeGuid : DataForgeSerializable
{
    public Guid Value { get; set; }

    public DataForgeGuid(DataForgeInstancePackage documentRoot) : base(documentRoot) { Value = Br.ReadGuid(false).Value; }

    public override string ToString() => Value.ToString();

    public async Task Read(XmlWriter writer)
    {
        await writer.WriteStartElementAsync(null, "Guid", null);
        await writer.WriteAttributeStringAsync(null, "Value", null, Value.ToString());
        await writer.WriteEndElementAsync();
    }
}