using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace unforge;

internal class DataForgeRecord : DataForgeSerializable
{
    internal uint NameOffset { get; set; }
    internal string Name => Index.ValueMap[NameOffset];

    internal string FileName => Index.ValueMap[FileNameOffset];
    internal uint FileNameOffset { get; set; }

    internal string __structIndex => $"{StructIndex:X4}";
    internal uint StructIndex { get; set; }

    internal Guid? Hash { get; set; }

    internal string __variantIndex => $"{VariantIndex:X4}";
    internal ushort VariantIndex { get; set; }

    internal string __otherIndex => $"{OtherIndex:X4}";
    internal ushort OtherIndex { get; set; }

    internal DataForgeRecord(DataForgeIndex Index) : base(Index)
    {
        NameOffset = Index.Reader.ReadUInt32();
        if (!Index.IsLegacy) FileNameOffset = Index.Reader.ReadUInt32();
        StructIndex = Index.Reader.ReadUInt32();
        Hash = Index.Reader.ReadGuid(false);
        VariantIndex = Index.Reader.ReadUInt16();
        OtherIndex = Index.Reader.ReadUInt16();
    }

    internal override Task PreSerialise() => Task.CompletedTask;
    internal override XmlElement Serialise(string name = null) => default;
}