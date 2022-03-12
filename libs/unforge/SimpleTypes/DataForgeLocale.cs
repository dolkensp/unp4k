using System.Threading.Tasks;

namespace unforge;
internal class DataForgeLocale : DataForgeSerializable<string>
{
    internal DataForgeLocale(DataForgeIndex index) : base(index, index.ValueMap[index.Reader.ReadUInt32()]) { }

    internal override async Task Serialise()
    {
        await Index.Writer.WriteStartElementAsync(null, "LocID", null);
        await Index.Writer.WriteAttributeStringAsync(null, "Value", null, Value.ToString());
        await Index.Writer.WriteEndElementAsync();
    }
}