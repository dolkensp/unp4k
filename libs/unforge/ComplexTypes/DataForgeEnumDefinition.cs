using System.Xml;

namespace unforge;

internal class DataForgeEnumDefinition : DataForgeSerializable
{
    internal string Name => Index.ValueMap[NameOffset];
    internal uint NameOffset { get; set; }
    internal ushort ValueCount { get; set; }
    internal ushort FirstValueIndex { get; set; }

    internal DataForgeEnumDefinition(DataForgeIndex index) : base(index)
    {
        NameOffset = Index.Reader.ReadUInt32();
        ValueCount = Index.Reader.ReadUInt16();
        FirstValueIndex = Index.Reader.ReadUInt16();
    }

    internal override Task PreSerialise() => Task.CompletedTask;
    internal override XmlElement Serialise(string name = null) => default;
}