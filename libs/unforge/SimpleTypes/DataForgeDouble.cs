using System.Threading.Tasks;

namespace unforge;
internal class DataForgeDouble : DataForgeSerializable<double>
{
    internal DataForgeDouble(DataForgeIndex index) : base(index, index.Reader.ReadDouble()) { }

    internal override async Task Serialise()
    {
        await Index.Writer.WriteStartElementAsync(null, "Double", null);
        await Index.Writer.WriteAttributeStringAsync(null, "Value", null, Value.ToString());
        await Index.Writer.WriteEndElementAsync();
    }
}