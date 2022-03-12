using System.Threading.Tasks;

namespace unforge;
internal class DataForgeInt32 : DataForgeSerializable<int>
{
    internal DataForgeInt32(DataForgeIndex index) : base(index, index.Reader.ReadInt32()) { }

    internal override async Task Serialise()
    {
        await Index.Writer.WriteStartElementAsync(null, "Int32", null);
        await Index.Writer.WriteAttributeStringAsync(null, "Value", null, Value.ToString());
        await Index.Writer.WriteEndElementAsync();
    }
}