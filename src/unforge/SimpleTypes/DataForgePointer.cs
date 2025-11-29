using System;
using System.Xml;

namespace unforge
{
	public class DataForgePointer : DataForgeTypeReader
    {
		public static Int32 RecordSizeInBytes = 8;


		public DataForgeStructDefinition StructDefinition { get => this.StreamReader.ReadStructDefinitionAtIndex(this.StructIndex); }
		public Boolean IsNull { get => this.StructIndex == 0xFFFFFFFF && this.VariantIndex == 0xFFFF; }

		public UInt32 StructIndex { get; }
        public UInt16 VariantIndex { get; }
        public UInt16 Padding { get; }

        private DataForgePointer(DataForge reader) : base(reader)
        {
            this.StructIndex = this.StreamReader.ReadUInt32();

			this.VariantIndex = this.StreamReader.ReadUInt16();
			this.Padding = this.StreamReader.ReadUInt16();
		}

		public static DataForgePointer ReadFromStream(DataForge baseStream) => new DataForgePointer(baseStream);

        public override String ToString()
        {
            return String.Format("0x{0:X8} 0x{1:X4} 0x{2:X4}", this.StructIndex, this.VariantIndex, this.Padding);
        }
    }
}
