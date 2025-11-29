using System;
using System.Xml;

namespace unforge
{
	public class DataForgeLocale : DataForgeTypeReader
    {
		public static Int32 RecordSizeInBytes = 4;

		private UInt32 ValueOffset { get; }

		// HACK: Could be blob - need to check
		public String Value { get => this.StreamReader.ReadTextAtOffset(this.ValueOffset); }
		
		public static DataForgeLocale ReadFromStream(DataForge baseStream) => new DataForgeLocale(baseStream);

		private DataForgeLocale(DataForge reader) : base(reader)
        {
            this.ValueOffset = this.StreamReader.ReadUInt32();
        }

        public override String ToString()
        {
            return this.Value;
        }
    }
}
