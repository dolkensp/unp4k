using System;

namespace unforge
{
	public class DataForgeString : DataForgeTypeReader
    {
        public String Value { get; }

		public static DataForgeString ReadFromStream(DataForge baseStream) => new DataForgeString(baseStream);

		private DataForgeString(DataForge reader) : base(reader)
        {
            this.Value = this.StreamReader.ReadCString();
        }

		public override String ToString()
        {
            return this.Value;
        }
    }
}
