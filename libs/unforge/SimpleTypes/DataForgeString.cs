using System;
using System.Threading.Tasks;

namespace unforge;
internal class DataForgeString : DataForgeSerializable<string>
{
    internal DataForgeString(DataForgeIndex index) : base(index, index.Reader.ReadCString()) { }

    internal override Task Serialise() => Task.CompletedTask;
}