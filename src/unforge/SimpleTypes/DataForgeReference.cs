using System;
using System.IO;

namespace unforge
{
	public class DataForgeReference : DataForgeTypeReader
    {
		public static Int32 RecordSizeInBytes = 20;

		public Boolean IsNull => this.Value == Guid.Empty;

		public UInt32 Item1 { get; set; }
        public Guid Value { get; set; }

		public static DataForgeReference ReadFromStream(DataForge baseStream) => new DataForgeReference(baseStream);

		public DataForgeReference(DataForge reader) : base(reader)
        {
            this.Item1 = this.StreamReader.ReadUInt32();
            this.Value = this.StreamReader.ReadGuid(false).Value;
        }

		public override String ToString()
        {
            return String.Format("0x{0:X8} 0x{1}", this.Item1, this.Value);
        }
    }
}
