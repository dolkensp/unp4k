using System;

namespace unforge
{
	public class DataForgeSingle : DataForgeTypeReader
    {
		public static Int32 RecordSizeInBytes = 4;

		public Single Value { get; set; }

		public static DataForgeSingle ReadFromStream(DataForge baseStream) => new DataForgeSingle(baseStream);

		private DataForgeSingle(DataForge reader) : base(reader)
        {
            this.Value = this.StreamReader.ReadSingle();
        }

        public override String ToString()
        {
            return String.Format("{0}", this.Value);
        }
    }
}
