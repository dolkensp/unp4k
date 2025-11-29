using System;

namespace unforge
{
	public class DataForgeUInt16 : DataForgeTypeReader
    {
		public static Int32 RecordSizeInBytes = 2;

		public UInt16 Value { get; }

		public static DataForgeUInt16 ReadFromStream(DataForge baseStream) => new DataForgeUInt16(baseStream);

		private DataForgeUInt16(DataForge reader) : base(reader)
        {
            this.Value = this.StreamReader.ReadUInt16();
        }

        public override String ToString()
        {
            return String.Format("{0}", this.Value);
        }
    }
}
