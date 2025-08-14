using System;

namespace unforge
{
	public class DataForgeDataMapping : _DataForgeSerializable
    {
        public UInt32 StructIndex { get; set; }
        public UInt32 StructCount { get; set; }
        public UInt32 NameOffset { get; set; }
        public String Name { get { return this.DocumentRoot.BlobMap[this.NameOffset]; } }

        public DataForgeDataMapping(DataForge documentRoot)
            : base(documentRoot)
        {
			if(this.DocumentRoot.FileVersion >= 5) {
				this.StructCount = this._br.ReadUInt32();
            	this.StructIndex = this._br.ReadUInt32();
			} else {
            	this.StructCount = this._br.ReadUInt16();
            	this.StructIndex = this._br.ReadUInt16();
			}
            this.NameOffset = documentRoot.StructDefinitionTable[this.StructIndex].NameOffset;
        }

        public override String ToString()
        {
            return String.Format("0x{1:X4} {2}[0x{0:X4}]", this.StructCount, this.StructIndex, this.Name);
        }
    }
}
