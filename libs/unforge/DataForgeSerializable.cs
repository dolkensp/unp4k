using System.Threading.Tasks;

namespace unforge;

internal abstract class DataForgeSerializable
{
    internal DataForgeIndex Index { get; private set; }

    internal DataForgeSerializable(DataForgeIndex index) { Index = index; }

    internal abstract Task Serialise(string name = null);
}

internal abstract class DataForgeSerializable<U>
{
    internal DataForgeIndex Index { get; private set; }

    internal U Value { get; private set; }

    internal DataForgeSerializable(DataForgeIndex index, U value)
    {
        Index = index;
        Value = value;
    }

    internal abstract Task Serialise();
}