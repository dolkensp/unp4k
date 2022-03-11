using System;

namespace unforge;
internal class DataForgeString : DataForgeSerializable
{
    public string Value { get; set; }

    public DataForgeString(DataForgeIndex documentRoot) : base(documentRoot) { Value = Br.ReadCString(); }

    public override string ToString() => Value;
}