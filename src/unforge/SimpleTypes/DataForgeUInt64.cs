using System;

namespace unforge
{
	public class DataForgeUInt64 : DataForgeTypeReader
    {
		public static Int32 RecordSizeInBytes = 8;

		public UInt64 Value { get; }

		public static DataForgeUInt64 ReadFromStream(DataForge baseStream) => new DataForgeUInt64(baseStream);

		private DataForgeUInt64(DataForge reader) : base(reader)
        {
            this.Value = this.StreamReader.ReadUInt64();
        }

        public override String ToString()
        {
            return String.Format("{0}", this.Value);
        }
    }
}
