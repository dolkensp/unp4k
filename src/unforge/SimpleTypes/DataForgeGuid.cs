using System;
using System.IO;

namespace unforge
{
	public class DataForgeGuid : DataForgeTypeReader
    {
		public static Int32 RecordSizeInBytes = 16;

		public Guid Value { get; }
		
		public static DataForgeGuid ReadFromStream(DataForge baseStream) => new DataForgeGuid(baseStream);

		private DataForgeGuid(DataForge baseStream) : base(baseStream)
        {
            this.Value = this.StreamReader.ReadGuid(false).Value;
        }

        public override String ToString()
        {
            return this.Value.ToString();
        }
    }
}
