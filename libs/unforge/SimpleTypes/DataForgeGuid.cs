using System;
using System.IO;
using System.Threading.Tasks;

namespace unforge;
internal class DataForgeGuid : DataForgeSerializable<Guid>
{
    internal DataForgeGuid(DataForgeIndex index) : base(index, index.Reader.ReadGuid(false).Value) { }

    internal override async Task Serialise()
    {
        await Index.Writer.WriteStartElementAsync(null, "Guid", null);
        await Index.Writer.WriteAttributeStringAsync(null, "Value", null, Value.ToString());
        await Index.Writer.WriteEndElementAsync();
    }
}