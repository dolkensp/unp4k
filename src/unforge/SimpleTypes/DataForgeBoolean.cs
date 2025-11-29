using System;

namespace unforge
{
	public class DataForgeBoolean : DataForgeTypeReader
    {
		public static Int32 RecordSizeInBytes = 1;

		public Boolean Value { get; private set; }

		public static DataForgeBoolean ReadFromStream(DataForge baseStream) => new DataForgeBoolean(baseStream);

		private DataForgeBoolean(DataForge baseStream) : base(baseStream)
        {
            this.Value = this.StreamReader.ReadBoolean();
        }

		public override String ToString()
        {
            return String.Format("{0}", this.Value ? "1" : "0");
        }
    }
}
