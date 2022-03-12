using System.Threading.Tasks;

namespace unforge;
internal class DataForgeStringLookup : DataForgeSerializable<string>
{
    internal DataForgeStringLookup(DataForgeIndex index) : base(index, index.ValueMap[index.Reader.ReadUInt32()]) { }

    internal override async Task Serialise()
    {
        await Index.Writer.WriteStartElementAsync(null, "String", null);
        await Index.Writer.WriteAttributeStringAsync(null, "Value", null, Value);
        await Index.Writer.WriteEndElementAsync();
    }
}