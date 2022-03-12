using System.Threading.Tasks;

namespace unforge;
internal class DataForgeInt64 : DataForgeSerializable<long>
{
    internal DataForgeInt64(DataForgeIndex index) : base(index, index.Reader.ReadInt64()) { }

    internal override async Task Serialise()
    {
        await Index.Writer.WriteStartElementAsync(null, "Int64", null);
        await Index.Writer.WriteAttributeStringAsync(null, "Value", null, Value.ToString());
        await Index.Writer.WriteEndElementAsync();
    }
}