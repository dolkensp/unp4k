using System;
using System.IO;
using System.Xml;
using System.Threading.Tasks;

namespace unforge;
public class DataForgeReference : DataForgeSerializable
{
    public uint Item1 { get; set; }
    public Guid Value { get; set; }

    public DataForgeReference(DataForgeInstancePackage documentRoot) : base(documentRoot)
    {
        Item1 = Br.ReadUInt32();
        Value = Br.ReadGuid(false).Value;
    }

    public override string ToString() => string.Format("0x{0:X8} 0x{1}", Item1, Value);

    public async Task Read(XmlWriter writer)
    {
        await writer.WriteStartElementAsync(null, "Reference", null);
        await writer.WriteAttributeStringAsync(null, "Value", null, $"{Value}");
        await writer.WriteEndElementAsync();
    }
}