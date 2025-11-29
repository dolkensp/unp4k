using System;
using System.Net.Http.Headers;

namespace unforge
{
	public class DataForgePropertyDefinition : DataForgeTypeReader
    {
		public String Name { get => this.StreamReader.ReadBlobAtOffset(this.NameOffset); }
	
		public static Int32 RecordSizeInBytes = 12;

		public UInt32 NameOffset { get; }
		public UInt16 Index { get; set; }
        public EDataType DataType { get; }
        public EConversionType ConversionType { get; }
        public UInt16 VariantIndex { get; }

		public static DataForgePropertyDefinition ReadFromStream(DataForge baseStream) => new DataForgePropertyDefinition(baseStream);

		private DataForgePropertyDefinition(DataForge baseStream) : base(baseStream)
		{
			this.NameOffset = baseStream.ReadUInt32();
			this.Index = baseStream.ReadUInt16();
			this.DataType = (EDataType)baseStream.ReadUInt16();
			this.ConversionType = (EConversionType)(baseStream.ReadUInt16() & 0xFF);
			this.VariantIndex = baseStream.ReadUInt16();
		}

        public override String ToString()
        {
            return String.Format("<{0} />", this.Name);
        }
	}
}
