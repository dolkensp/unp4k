using System;

namespace unforge;
internal class DataForgeString : DataForgeSerializable
{
    public string Value { get; set; }

    public DataForgeString(DataForgeInstancePackage documentRoot) : base(documentRoot) { Value = Br.ReadCString(); }

    public override string ToString() => Value;
}