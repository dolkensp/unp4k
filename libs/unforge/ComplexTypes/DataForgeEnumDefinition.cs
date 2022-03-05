namespace unforge;
public class DataForgeEnumDefinition : DataForgeSerializable
{
    public uint NameOffset { get; set; }
    public string Name { get { return DocumentRoot.ValueMap[NameOffset]; } }
    public ushort ValueCount { get; set; }
    public ushort FirstValueIndex { get; set; }

    public DataForgeEnumDefinition(DataForgeInstancePackage documentRoot) : base(documentRoot)
    {
        NameOffset = Br.ReadUInt32();
        ValueCount = Br.ReadUInt16();
        FirstValueIndex = Br.ReadUInt16();
    }

    public override string ToString() => string.Format("<{0} />", Name);
}