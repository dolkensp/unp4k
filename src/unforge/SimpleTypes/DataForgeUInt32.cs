using System;

namespace unforge
{
	public class DataForgeUInt32 : DataForgeTypeReader
    {
		public static Int32 RecordSizeInBytes = 4;

		public UInt32 Value { get; }

		public static DataForgeUInt32 ReadFromStream(DataForge baseStream) => new DataForgeUInt32(baseStream);

		private DataForgeUInt32(DataForge reader) : base(reader)
        {
            this.Value = this.StreamReader.ReadUInt32();
        }

        public override String ToString()
        {
            return String.Format("{0}", this.Value);
        }
    }
}
