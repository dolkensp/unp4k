using System.Threading.Tasks;
using System.Xml;

namespace unforge;

internal abstract class DataForgeSerializable
{
    internal DataForgeIndex Index { get; private set; }

    internal DataForgeSerializable(DataForgeIndex index) { Index = index; }

    internal abstract Task PreSerialise();
    internal abstract XmlElement Serialise(string name = null);
}

internal abstract class DataForgeSerializable<U>
{
    internal DataForgeIndex Index { get; private set; }

    internal U Value { get; set; }

    internal DataForgeSerializable(DataForgeIndex index, U value)
    {
        Index = index;
        Value = value;
    }

    internal abstract XmlElement Serialise();
}