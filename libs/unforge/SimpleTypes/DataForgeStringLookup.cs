using System.Threading.Tasks;

namespace unforge;
internal class DataForgeStringLookup : DataForgeSerializable<string>
{
    private static string TryValue;

    internal DataForgeStringLookup(DataForgeIndex index) : base(index, index.ValueMap.TryGetValue(index.Reader.ReadUInt32(), out TryValue) ? TryValue : string.Empty) { }

    internal override async Task Serialise()
    {
        await Index.Writer.WriteStartElementAsync(null, "String", null);
        await Index.Writer.WriteAttributeStringAsync(null, "Value", null, Value);
        await Index.Writer.WriteEndElementAsync();
    }
}