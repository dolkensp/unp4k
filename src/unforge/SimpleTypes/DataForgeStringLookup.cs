using System;

namespace unforge
{
	public class DataForgeStringLookup : DataForgeTypeReader
    {
		public static Int32 RecordSizeInBytes = 4;

		private UInt32 ValueOffset { get; }
		public String Value { get => this.StreamReader.ReadTextAtOffset(this.ValueOffset); }

		public static DataForgeStringLookup ReadFromStream(DataForge baseStream) => new DataForgeStringLookup(baseStream);

		private DataForgeStringLookup(DataForge baseStream) : base(baseStream)
        {
            this.ValueOffset = this.StreamReader.ReadUInt32();
        }

		public override String ToString() => this.Value;
    }
}
