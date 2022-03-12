using System.Threading.Tasks;

namespace unforge;
internal class DataForgeUInt8 : DataForgeSerializable<byte>
{
    internal DataForgeUInt8(DataForgeIndex index) : base(index, index.Reader.ReadByte()) { }

    internal override async Task Serialise()
    {
        await Index.Writer.WriteStartElementAsync(null, "UInt8", null);
        await Index.Writer.WriteAttributeStringAsync(null, "Value", null, Value.ToString());
        await Index.Writer.WriteEndElementAsync();
    }
}
