using System;

namespace unforge
{
	public class DataForgeInt32 : DataForgeTypeReader
    {
		public static Int32 RecordSizeInBytes = 4;

		public Int32 Value { get; }

		public static DataForgeInt32 ReadFromStream(DataForge baseStream) => new DataForgeInt32(baseStream);

		private DataForgeInt32(DataForge reader) : base(reader)
        {
            this.Value = this.StreamReader.ReadInt32();
        }

        public override String ToString()
        {
            return String.Format("{0}", this.Value);
        }
    }
}
