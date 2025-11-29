using System;

namespace unforge
{
	public class DataForgeEnum : DataForgeTypeReader
    {
		public static Int32 RecordSizeInBytes = 4;

		public UInt32 ValueOffset { get; }
        public String Value { get => this.StreamReader.ReadTextAtOffset(this.ValueOffset); }
		// { get { return this.DocumentRoot.TextMap[this._value]; } }

		public static DataForgeEnum ReadFromStream(DataForge baseStream) => new DataForgeEnum(baseStream);

		private DataForgeEnum(DataForge stream) : base(stream)
        {
            this.ValueOffset = this.StreamReader.ReadUInt32();
        }

        public override String ToString()
        {
            return this.Value;
        }
    }
}
