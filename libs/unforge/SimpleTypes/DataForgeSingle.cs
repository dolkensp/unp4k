using System.Threading.Tasks;

namespace unforge;
internal class DataForgeSingle : DataForgeSerializable<float>
{
    internal DataForgeSingle(DataForgeIndex index) : base(index, index.Reader.ReadSingle()) { }

    internal override async Task Serialise()
    {
        await Index.Writer.WriteStartElementAsync(null, "Single", null);
        await Index.Writer.WriteAttributeStringAsync(null, "Value", null, Value.ToString());
        await Index.Writer.WriteEndElementAsync();
    }
}