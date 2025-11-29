using System;
using System.Reflection.PortableExecutable;

namespace unforge
{
	public class DataForgeDataMapping : DataForgeTypeReader
    {
		public UInt32 NameOffset { get => this.StreamReader.ReadStructDefinitionAtIndex(this.StructIndex).NameOffset; }
		public String Name { get => this.StreamReader.ReadBlobAtOffset(this.NameOffset); }

		public static Int32 RecordSizeInBytes = 4;
		public static Int32 RecordSizeInBytesV6 = 8;

		public UInt32 StructIndex { get; }
        public UInt32 StructCount { get; }

		private DataForgeDataMapping(DataForge reader) : base(reader)
		{
			if (this.StreamReader.FileVersion >= 5)
			{
				this.StructCount = this.StreamReader.ReadUInt32();
				this.StructIndex = this.StreamReader.ReadUInt32();
			}
			else
			{
				this.StructCount = this.StreamReader.ReadUInt16();
				this.StructIndex = this.StreamReader.ReadUInt16();
			}
		}

		public static DataForgeDataMapping ReadFromStream(DataForge reader) => new DataForgeDataMapping(reader);

        public override String ToString()
        {
            return String.Format("0x{1:X4} {2}[0x{0:X4}]", this.StructCount, this.StructIndex, this.Name);
        }
    }
}
