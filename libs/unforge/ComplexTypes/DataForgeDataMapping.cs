using System.Threading.Tasks;

namespace unforge;
internal class DataForgeDataMapping : DataForgeSerializable
{
    internal string Name => Index.ValueMap[NameOffset];
    internal uint StructIndex       { get; set; }
    internal uint StructCount       { get; set; }
    internal uint NameOffset        { get; set; }

    internal DataForgeDataMapping(DataForgeIndex index) : base(index)
    {
        if (Index.FileVersion >= 5)
        {
            StructCount = Index.Reader.ReadUInt32();
            StructIndex = Index.Reader.ReadUInt32();
        }
        else
        {
            StructCount = Index.Reader.ReadUInt16();
            StructIndex = Index.Reader.ReadUInt16();
        }
        NameOffset = Index.StructDefinitionTable[(int)StructIndex].NameOffset;
    }

    internal override Task PreSerialise() => Task.CompletedTask;
    internal override Task Serialise(string name = null) => Task.CompletedTask;
}