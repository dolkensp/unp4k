using System;

namespace unforge
{
    public class DataForgeString : DataForgeSerializable
    {
        public string Value { get; set; }

        public DataForgeString(DataForge documentRoot) : base(documentRoot) { Value = br.ReadCString(); }

        public override string ToString() => Value;
    }
}
