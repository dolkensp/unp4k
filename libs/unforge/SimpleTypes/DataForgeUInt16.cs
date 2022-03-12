using System.Threading.Tasks;

namespace unforge;
internal class DataForgeUInt16 : DataForgeSerializable<ushort>
{
    internal DataForgeUInt16(DataForgeIndex index) : base(index, index.Reader.ReadUInt16()) { }

    internal override async Task Serialise()
    {
        await Index.Writer.WriteStartElementAsync(null, "UInt16", null);
        await Index.Writer.WriteAttributeStringAsync(null, "Value", null, Value.ToString());
        await Index.Writer.WriteEndElementAsync();
    }
}