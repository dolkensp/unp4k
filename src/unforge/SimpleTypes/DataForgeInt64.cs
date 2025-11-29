using System;

namespace unforge
{
	public class DataForgeInt64 : DataForgeTypeReader
    {
		public static Int32 RecordSizeInBytes = 8;

		public Int64 Value { get; }

		public static DataForgeInt64 ReadFromStream(DataForge baseStream) => new DataForgeInt64(baseStream);

		private DataForgeInt64(DataForge reader) : base(reader)
        {
            this.Value = this.StreamReader.ReadInt64();
        }

        public override String ToString()
        {
            return String.Format("{0}", this.Value);
        }
    }
}
