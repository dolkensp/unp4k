using System.Threading.Tasks;

namespace unforge;
internal class DataForgeUInt64 : DataForgeSerializable<ulong>
{
    internal DataForgeUInt64(DataForgeIndex index) : base(index, index.Reader.ReadUInt64()) { }

    internal override async Task Serialise()
    {
        await Index.Writer.WriteStartElementAsync(null, "UInt64", null);
        await Index.Writer.WriteAttributeStringAsync(null, "Value", null, Value.ToString());
        await Index.Writer.WriteEndElementAsync();
    }
}