using System.Threading.Tasks;

namespace unforge;
internal class DataForgeInt16 : DataForgeSerializable<short>
{
    internal DataForgeInt16(DataForgeIndex index) : base(index, index.Reader.ReadInt16()) { }

    internal override async Task Serialise()
    {
        await Index.Writer.WriteStartElementAsync(null, "Int16", null);
        await Index.Writer.WriteAttributeStringAsync(null, "Value", null, Value.ToString());
        await Index.Writer.WriteEndElementAsync();
    }
}