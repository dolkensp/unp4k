using System.Xml;
using System.Threading.Tasks;

namespace unforge;
public class DataForgePointer : DataForgeSerializable
{
    public uint StructType { get; set; }
    public uint Index { get; set; }

    public DataForgePointer(DataForgeInstancePackage documentRoot) : base(documentRoot)
    {
        StructType = Br.ReadUInt32();
        Index = Br.ReadUInt32();
    }

    public override string ToString() => string.Format("0x{0:X8} 0x{1:X8}", StructType, Index);

    public async Task Read(XmlWriter writer)
    {
        await writer.WriteStartElementAsync(null, "UInt64", null);
        await writer.WriteAttributeStringAsync(null, "typeIndex", null, string.Format("{0:X4}", StructType));
        await writer.WriteAttributeStringAsync(null, "firstIndex", null, string.Format("{0:X4}", Index));
        await writer.WriteEndElementAsync();
    }
}