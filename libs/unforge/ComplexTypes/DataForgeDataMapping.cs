namespace unforge
{
    public class DataForgeDataMapping : DataForgeSerializable
    {
        public uint StructIndex { get; set; }
        public uint StructCount { get; set; }
        public uint NameOffset { get; set; }
        public string Name { get { return DocumentRoot.ValueMap[NameOffset]; } }

        public DataForgeDataMapping(DataForge documentRoot) : base(documentRoot)
        {
			if (DocumentRoot.FileVersion >= 5) 
            {
				StructCount = br.ReadUInt32();
            	StructIndex = br.ReadUInt32();
			} 
            else 
            {
            	StructCount = br.ReadUInt16();
            	StructIndex = br.ReadUInt16();
			}
            NameOffset = documentRoot.StructDefinitionTable[StructIndex].NameOffset;
        }

        public override string ToString() => string.Format("0x{1:X4} {2}[0x{0:X4}]", StructCount, StructIndex, Name);
    }
}
