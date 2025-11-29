using System;

namespace unforge
{
	public class DataForgeDouble : DataForgeTypeReader
    {
		public static Int32 RecordSizeInBytes = 8;

		public Double Value { get; private set; }

		public static DataForgeDouble ReadFromStream(DataForge baseStream) => new DataForgeDouble(baseStream);

		private DataForgeDouble(DataForge reader) : base(reader)
		{
            this.Value = this.StreamReader.ReadDouble();
        }

        public override String ToString()
        {
            return String.Format("{0}", this.Value);
        }
    }
}
