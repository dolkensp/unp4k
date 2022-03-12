using System;
using System.IO;
using System.Threading.Tasks;

namespace unforge;
internal class DataForgeRecord : DataForgeSerializable
{
    internal uint NameOffset { get; set; }
    internal uint FileNameOffset { get; set; }
    internal uint StructIndex { get; set; }
    internal Guid? Hash { get; set; }
    internal ushort VariantIndex { get; set; }
    internal ushort OtherIndex { get; set; }

    internal DataForgeRecord(DataForgeIndex index) : base(index)
    {
        NameOffset = Index.Reader.ReadUInt32();
        FileNameOffset = Index.Reader.ReadUInt32();
        StructIndex = Index.Reader.ReadUInt32();
        Hash = Index.Reader.ReadGuid(false);
        VariantIndex = Index.Reader.ReadUInt16();
        OtherIndex = Index.Reader.ReadUInt16();
    }

    internal override Task PreSerialise() => Task.CompletedTask;
    internal override Task Serialise(string name = null) => Task.CompletedTask;
}