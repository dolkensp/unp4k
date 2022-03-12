using System.Threading.Tasks;

namespace unforge;
internal class DataForgeEnum : DataForgeSerializable<string>
{
    internal DataForgeEnum(DataForgeIndex index) : base(index, index.ValueMap[index.Reader.ReadUInt32()]) { }

    internal override async Task Serialise()
    {
        await Index.Writer.WriteStartElementAsync(null, "Enum", null);
        await Index.Writer.WriteAttributeStringAsync(null, "Value", null, Value);
        await Index.Writer.WriteEndElementAsync();
    }
}