using System.IO;
using System.Xml;

namespace unforge;

internal class DataForgeString : DataForgeSerializable<string>
{
    internal DataForgeString(DataForgeIndex index) : base(index, index.Reader.ReadCString()) { }

    internal override XmlElement Serialise() => default;
}