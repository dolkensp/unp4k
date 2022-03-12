using System;
using System.Threading.Tasks;

namespace unforge;
internal class DataForgeBoolean : DataForgeSerializable<bool>
{
    internal DataForgeBoolean(DataForgeIndex index) : base(index, index.Reader.ReadBoolean()) { }

    internal override async Task Serialise()
    {
        await Index.Writer.WriteStartElementAsync(null, "Bool", null);
        await Index.Writer.WriteAttributeStringAsync(null, "Value", null, Convert.ToInt32(Value).ToString());
        await Index.Writer.WriteEndElementAsync();
    }
}