using System;
using System.IO;

namespace unforge
{
	public class DataForgeRecord : _DataForgeSerializable
	{
		public static Int32 RecordSizeInBytes = 32;

		public UInt32 NameOffset { get; set; }
		public String Name
		{
			get
			{
				if (this._br is DataForgeStream br) return br.ReadBlobAtOffset(this.NameOffset);
				return this.DocumentRoot.BlobMap[this.NameOffset];
			}
		}

		public String FileName
		{
			get
			{
				if (this._br is DataForgeStream br) return br.ReadTextAtOffset(this.FileNameOffset);
				return this.DocumentRoot.TextMap[this.FileNameOffset];
			}
		}
		public UInt32 FileNameOffset { get; set; }

		public String __structIndex { get { return String.Format("{0:X4}", this.StructIndex); } }
		public UInt32 StructIndex { get; set; }

		public Guid? Hash { get; set; }

		public String __variantIndex { get { return String.Format("{0:X4}", this.VariantIndex); } }
		public UInt16 VariantIndex { get; set; }

		public String __otherIndex { get { return String.Format("{0:X4}", this.OtherIndex); } }
		public UInt16 OtherIndex { get; set; }

		public DataForgeRecord(DataForge documentRoot)
			: this(documentRoot._br, documentRoot.IsLegacy) { }

		protected DataForgeRecord(BinaryReader br, Boolean isLegacy)
		{
			this._br = br;

			this.NameOffset = br.ReadUInt32();

			if (!isLegacy)
			{
				this.FileNameOffset = br.ReadUInt32();
			}

			this.StructIndex = br.ReadUInt32();
			this.Hash = br.ReadGuid(false);

			this.VariantIndex = br.ReadUInt16();
			this.OtherIndex = br.ReadUInt16();
		}

		public static DataForgeRecord Read(BinaryReader br, Boolean isLegacy) => new DataForgeRecord(br, isLegacy);

		public override String ToString()
		{
			return String.Format("<{0} {1:X4} />", this.Name, this.StructIndex);
		}
	}
}
