using System;

namespace unforge
{
	public class DataForgeUInt8 : DataForgeTypeReader
    {
		public static Int32 RecordSizeInBytes = 1;

		public Byte Value { get; }

		public static DataForgeUInt8 ReadFromStream(DataForge baseStream) => new DataForgeUInt8(baseStream);

		private DataForgeUInt8(DataForge reader) : base(reader)
        {
            this.Value = this.StreamReader.ReadByte();
        }

        public override String ToString()
        {
            return String.Format("{0}", this.Value);
        }
    }
}
