using System.Threading.Tasks;

namespace unforge;
internal class DataForgeInt8 : DataForgeSerializable<sbyte>
{
    internal DataForgeInt8(DataForgeIndex index) : base(index, index.Reader.ReadSByte()) { }

    internal override async Task Serialise()
    {
        await Index.Writer.WriteStartElementAsync(null, "Int8", null);
        await Index.Writer.WriteAttributeStringAsync(null, "Value", null, Value.ToString());
        await Index.Writer.WriteEndElementAsync();
    }
}