using System;

namespace unforge
{
	public abstract class DataForgeTypeReader
    {
		internal Int64 Position { get; }

        internal DataForge StreamReader { get; }

		protected DataForgeTypeReader(DataForge reader)
		{
			this.StreamReader = reader;
			this.Position = reader.Position;
		}
	}
}
