using System;
using System.IO;
using System.Threading.Tasks;

namespace unforge;
internal class DataForgeReference : DataForgeSerializable<Guid>
{
    internal uint Item { get; private set; }

    internal DataForgeReference(DataForgeIndex index) : base(index, index.Reader.ReadGuid(false).Value) { Item = index.Reader.ReadUInt32(); }

    internal override async Task Serialise()
    {
        await Index.Writer.WriteStartElementAsync(null, "Reference", null);
        await Index.Writer.WriteAttributeStringAsync(null, "Value", null, $"{Value}");
        await Index.Writer.WriteEndElementAsync();
    }
}