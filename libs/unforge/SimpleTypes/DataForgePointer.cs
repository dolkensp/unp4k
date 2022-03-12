using System.Threading.Tasks;

namespace unforge;
internal class DataForgePointer : DataForgeSerializable<uint>
{
    internal uint StructType { get; set; }

    internal DataForgePointer(DataForgeIndex index) : base(index, index.Reader.ReadUInt32()) { StructType = index.Reader.ReadUInt32(); }

    internal override async Task Serialise()
    {
        await Index.Writer.WriteStartElementAsync(null, "UInt64", null);
        await Index.Writer.WriteAttributeStringAsync(null, "typeIndex", null, $"{StructType:X4}");
        await Index.Writer.WriteAttributeStringAsync(null, "firstIndex", null, $"{Index:X4}");
        await Index.Writer.WriteEndElementAsync();
    }
}