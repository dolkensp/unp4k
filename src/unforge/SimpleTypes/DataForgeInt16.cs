using System;

namespace unforge
{
	public class DataForgeInt16 : DataForgeTypeReader
    {
		public static Int32 RecordSizeInBytes = 2;

		public Int16 Value { get; }

		public static DataForgeInt16 ReadFromStream(DataForge baseStream) => new DataForgeInt16(baseStream);

		private DataForgeInt16(DataForge reader) : base(reader)
        {
            this.Value = this.StreamReader.ReadInt16();
        }

        public override String ToString()
        {
            return String.Format("{0}", this.Value);
        }
    }
}
