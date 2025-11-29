using System;
using System.Xml;

namespace unforge
{
	public class DataForgeInt8 : DataForgeTypeReader
    {
		public static Int32 RecordSizeInBytes = 1;

		public SByte Value { get; }

		public static DataForgeInt8 ReadFromStream(DataForge baseStream) => new DataForgeInt8(baseStream);

		private DataForgeInt8(DataForge reader) : base(reader)
        {
            this.Value = this.StreamReader.ReadSByte();
        }

        public override String ToString()
        {
            return String.Format("{0}", this.Value);
        }
    }
}
