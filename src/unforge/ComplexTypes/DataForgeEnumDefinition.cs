using System;
using System.Text;

namespace unforge
{
	public class DataForgeEnumDefinition : DataForgeTypeReader
    {
		public static Int32 RecordSizeInBytes = 8;

		public UInt32 NameOffset { get; set; }
        public String Name { get => this.StreamReader.ReadBlobAtOffset(this.NameOffset); }
        public UInt16 ValueCount { get; set; }
        public UInt16 FirstValueIndex { get; set; }

        private DataForgeEnumDefinition(DataForge baseStream)
            : base(baseStream)
        {
            this.NameOffset = this.StreamReader.ReadUInt32();
            this.ValueCount = this.StreamReader.ReadUInt16();
            this.FirstValueIndex = this.StreamReader.ReadUInt16();
        }

		public static DataForgeEnumDefinition ReadFromStream(DataForge baseStream) => new DataForgeEnumDefinition(baseStream);

		public override String ToString()
        {
            return String.Format("<{0} />", this.Name);
        }
    }
}
