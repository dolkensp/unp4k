using System.Threading.Tasks;

namespace unforge;
internal class DataForgeLocale : DataForgeSerializable<string>
{
    private static string TryValue;

    internal DataForgeLocale(DataForgeIndex index) : base(index, index.ValueMap.TryGetValue(index.Reader.ReadUInt32(), out TryValue) ? TryValue : string.Empty) { }

    internal override async Task Serialise()
    {
        await Index.Writer.WriteStartElementAsync(null, "LocID", null);
        await Index.Writer.WriteAttributeStringAsync(null, "Value", null, Value.ToString());
        await Index.Writer.WriteEndElementAsync();
    }
}