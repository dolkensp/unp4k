using System.Threading.Tasks;

namespace unforge;
internal class DataForgeUInt32 : DataForgeSerializable<uint>
{
    internal DataForgeUInt32(DataForgeIndex index) : base(index, index.Reader.ReadUInt32()) { }

    internal override async Task Serialise()
    {
        await Index.Writer.WriteStartElementAsync(null, "UInt32", null);
        await Index.Writer.WriteAttributeStringAsync(null, "Value", null, Value.ToString());
        await Index.Writer.WriteEndElementAsync();
    }
}