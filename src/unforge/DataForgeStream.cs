using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace unforge
{
	public class DataForgeStream : BinaryReader
	{
		private BinaryReader _br;
		private Boolean _isLegacy;

		public Int32 FileVersion { get; private set; }
		public Int64 Length { get => this.BaseStream.Length; }

		public Int32 StructDefinitionCount { get; private set; }
		public Int32 PropertyDefinitionCount { get; private set; }
		public Int32 EnumDefinitionCount { get; private set; }
		public Int32 DataMappingCount { get; private set; }
		public Int32 RecordDefinitionCount { get; private set; }

		public Int32 BooleanValueCount { get; private set; }
		public Int32 Int8ValueCount { get; private set; }
		public Int32 Int16ValueCount { get; private set; }
		public Int32 Int32ValueCount { get; private set; }
		public Int32 Int64ValueCount { get; private set; }
		public Int32 UInt8ValueCount { get; private set; }
		public Int32 UInt16ValueCount { get; private set; }
		public Int32 UInt32ValueCount { get; private set; }
		public Int32 UInt64ValueCount { get; private set; }

		public Int32 SingleValueCount { get; private set; }
		public Int32 DoubleValueCount { get; private set; }
		public Int32 GuidValueCount { get; private set; }
		public Int32 StringValueCount { get; private set; }
		public Int32 LocaleValueCount { get; private set; }
		public Int32 EnumValueCount { get; private set; }
		public Int32 StrongValueCount { get; private set; }
		public Int32 WeakValueCount { get; private set; }

		public Int32 ReferenceValueCount { get; private set; }
		public Int32 EnumOptionCount { get; private set; }
		public UInt32 TextLength { get; private set; }
		public UInt32 BlobLength { get; private set; }

		private Int64 _structDefinitionOffset { get => this._isLegacy ? 116 : 120; }
		private Int64 _propertyDefinitionOffset { get => this._structDefinitionOffset + this.StructDefinitionCount * DataForgeStructDefinition.RecordSizeInBytes; }
		private Int64 _enumDefinitionOffset { get => this._propertyDefinitionOffset + this.PropertyDefinitionCount * DataForgePropertyDefinition.RecordSizeInBytes; }
		private Int64 _dataMappingOffset { get => this._enumDefinitionOffset + this.EnumDefinitionCount * DataForgeEnumDefinition.RecordSizeInBytes; }
		private Int64 _recordDefinitionOffset { get => this._dataMappingOffset + this.DataMappingCount * (this._isLegacy ? DataForgeDataMapping.RecordSizeInBytes : DataForgeDataMapping.RecordSizeInBytesV6); }
		private Int64 _contentOffset { get => this._recordDefinitionOffset + this.RecordDefinitionCount * DataForgeRecord.RecordSizeInBytes; }
		private Int64 _int8ValueOffset { get => this._contentOffset; }
		private Int64 _int16ValueOffset { get => this._int8ValueOffset + this.Int8ValueCount * DataForgeInt8.RecordSizeInBytes; }
		private Int64 _int32ValueOffset { get => this._int16ValueOffset + this.Int16ValueCount * DataForgeInt16.RecordSizeInBytes; }
		private Int64 _int64ValueOffset { get => this._int32ValueOffset + this.Int32ValueCount * DataForgeInt32.RecordSizeInBytes; }
		private Int64 _uint8ValueOffset { get => this._int64ValueOffset + this.Int64ValueCount * DataForgeInt64.RecordSizeInBytes; }
		private Int64 _uint16ValueOffset { get => this._uint8ValueOffset + this.UInt8ValueCount * DataForgeUInt8.RecordSizeInBytes; }
		private Int64 _uint32ValueOffset { get => this._uint16ValueOffset + this.UInt16ValueCount * DataForgeUInt16.RecordSizeInBytes; }
		private Int64 _uint64ValueOffset { get => this._uint32ValueOffset + this.UInt32ValueCount * DataForgeUInt32.RecordSizeInBytes; }
		private Int64 _booleanValueOffset { get => this._uint64ValueOffset + this.UInt64ValueCount * DataForgeUInt64.RecordSizeInBytes; }
		private Int64 _singleValueOffset { get => this._booleanValueOffset + this.BooleanValueCount * DataForgeBoolean.RecordSizeInBytes; }
		private Int64 _doubleValueOffset { get => this._singleValueOffset + this.SingleValueCount * DataForgeSingle.RecordSizeInBytes; }
		private Int64 _guidValueOffset { get => this._doubleValueOffset + this.DoubleValueCount * DataForgeDouble.RecordSizeInBytes; }
		private Int64 _stringValueOffset { get => this._guidValueOffset + this.GuidValueCount * DataForgeGuid.RecordSizeInBytes; }
		private Int64 _localeValueOffset { get => this._stringValueOffset + this.StringValueCount * DataForgeStringLookup.RecordSizeInBytes; }
		private Int64 _enumValueOffset { get => this._localeValueOffset + this.LocaleValueCount * DataForgeLocale.RecordSizeInBytes; }
		private Int64 _strongValueOffset { get => this._enumValueOffset + this.EnumValueCount * DataForgeEnum.RecordSizeInBytes; }
		private Int64 _weakValueOffset { get => this._strongValueOffset + this.StrongValueCount * DataForgePointer.RecordSizeInBytes; }
		private Int64 _referenceValueOffset { get => this._weakValueOffset + this.WeakValueCount * DataForgePointer.RecordSizeInBytes; }
		private Int64 _enumOptionOffset { get => this._referenceValueOffset + this.ReferenceValueCount * DataForgeReference.RecordSizeInBytes; }
		private Int64 _textOffset { get => this._enumOptionOffset + this.EnumOptionCount * DataForgeStringLookup.RecordSizeInBytes; }
		private Int64 _blobOffset { get => this._textOffset + this.TextLength; }
		private Int64 _dataOffset { get => this._blobOffset + this.BlobLength; }

		public DataForgeStream(BinaryReader br, Boolean isLegacy = false) : base(br.BaseStream)
		{
			this._br = br;
			this._isLegacy = isLegacy;

			this.ReadHeader();
			this.ReportOffsets();

			this.BaseStream.Seek(this._recordDefinitionOffset, SeekOrigin.Begin);

			this.RecordMap = new Dictionary<String, Int32> { };

			foreach (var recordIndex in Enumerable.Range(0, this.RecordDefinitionCount))
			{
				this.BaseStream.Seek(this._recordDefinitionOffset + recordIndex * DataForgeRecord.RecordSizeInBytes, SeekOrigin.Begin);
				var record = DataForgeRecord.Read(this, this._isLegacy);
				var filename = record.FileName;
				var extension = Path.GetExtension(filename);
				filename = filename.Replace(extension, $"{recordIndex}{extension}");
				this.RecordMap[filename] = recordIndex;
			}
		}

		public Dictionary<String, Int32> RecordMap { get; private set; }

		internal String ReadTextAtOffset(Int64 offset)
		{
			this.BaseStream.Position = this._textOffset + offset;
			return this.ReadCString();
		}

		internal String ReadBlobAtOffset(Int64 offset)
		{
			this.BaseStream.Position = this._blobOffset + offset;
			return this.ReadCString();
		}

		private String ReadCString()
		{
			// Small, fast stack buffer for typical strings
			Span<Char> initialBuffer = stackalloc Char[256];
			Span<Char> buffer = initialBuffer;

			Char[] rented = null;
			Int32 length = 0;

			try
			{
				Int32 value;
				while ((value = this.BaseStream.ReadByte()) != -1 && value != 0)
				{
					// Need more space? Rent a bigger buffer.
					if (length == buffer.Length)
					{
						Int32 newSize = buffer.Length * 2;

						Char[] newRented = ArrayPool<Char>.Shared.Rent(newSize);
						buffer.CopyTo(newRented);

						rented = newRented;
						buffer = rented;
					}

					buffer[length++] = (Char)value;
				}

				// One String allocation from the final span slice
				return new String(buffer.Slice(0, length));
			}
			finally
			{
				if (rented != null)
				{
					ArrayPool<Char>.Shared.Return(rented);
				}
			}
		}

		public XmlElement ReadXmlRecord(Int32 recordIndex)
		{
			this.BaseStream.Seek(this._recordDefinitionOffset + recordIndex * DataForgeRecord.RecordSizeInBytes, SeekOrigin.Begin);
			var record = DataForgeRecord.Read(this, this._isLegacy);
			var schema = DataForgeStructDefinition.Read(this, this._isLegacy);

			var xml = new XmlDocument();
			var xmlElement = xml.CreateElement(record.Name);
			if (record.Hash.HasValue && record.Hash != Guid.Empty) xmlElement.AddAttribute("__ref", record.Hash);
			if (!String.IsNullOrWhiteSpace(record.FileName)) xmlElement.AddAttribute("__path", record.FileName);

			return xmlElement;
		}

		private void ReportOffsets()
		{
			Console.WriteLine($"StructDefinitionOffset: {_structDefinitionOffset}");
			Console.WriteLine($"PropertyDefinitionOffset: {_propertyDefinitionOffset}");
			Console.WriteLine($"EnumDefinitionOffset: {_enumDefinitionOffset}");
			Console.WriteLine($"DataMappingOffset: {_dataMappingOffset}");
			Console.WriteLine($"RecordDefinitionOffset: {_recordDefinitionOffset}");
			Console.WriteLine($"ContentOffset: {_contentOffset}");
			Console.WriteLine($"Int8ValueOffset: {_int8ValueOffset}");
			Console.WriteLine($"Int16ValueOffset: {_int16ValueOffset}");
			Console.WriteLine($"Int32ValueOffset: {_int32ValueOffset}");
			Console.WriteLine($"Int64ValueOffset: {_int64ValueOffset}");
			Console.WriteLine($"UInt8ValueOffset: {_uint8ValueOffset}");
			Console.WriteLine($"UInt16ValueOffset: {_uint16ValueOffset}");
			Console.WriteLine($"UInt32ValueOffset: {_uint32ValueOffset}");
			Console.WriteLine($"UInt64ValueOffset: {_uint64ValueOffset}");
			Console.WriteLine($"BooleanValueOffset: {_booleanValueOffset}");
			Console.WriteLine($"SingleValueOffset: {_singleValueOffset}");
			Console.WriteLine($"DoubleValueOffset: {_doubleValueOffset}");
			Console.WriteLine($"GuidValueOffset: {_guidValueOffset}");
			Console.WriteLine($"StringValueOffset: {_stringValueOffset}");
			Console.WriteLine($"LocaleValueOffset: {_localeValueOffset}");
			Console.WriteLine($"EnumValueOffset: {_enumValueOffset}");
			Console.WriteLine($"StrongValueOffset: {_strongValueOffset}");
			Console.WriteLine($"WeakValueOffset: {_weakValueOffset}");
			Console.WriteLine($"ReferenceValueOffset: {_referenceValueOffset}");
			Console.WriteLine($"EnumOptionOffset: {_enumOptionOffset}");
			Console.WriteLine($"TextOffset: {_textOffset}");
			Console.WriteLine($"BlobOffset: {_blobOffset}");
			Console.WriteLine($"DataOffset: {_dataOffset}");
			Console.WriteLine($"Length: {Length}");
		}

		private void ReadHeader()
		{
			this._br.BaseStream.Seek(0, SeekOrigin.Begin);

			_ = this._br.ReadUInt16();
			_ = this._br.ReadUInt16();

			this.FileVersion = this._br.ReadInt32();

			if (!this._isLegacy)
			{
				_ = this._br.ReadUInt16();
				_ = this._br.ReadUInt16();
				_ = this._br.ReadUInt16();
				_ = this._br.ReadUInt16();
			}

			this.StructDefinitionCount = this._br.ReadInt32();
			this.PropertyDefinitionCount = this._br.ReadInt32();
			this.EnumDefinitionCount = this._br.ReadInt32();
			this.DataMappingCount = this._br.ReadInt32();
			this.RecordDefinitionCount = this._br.ReadInt32();
			this.BooleanValueCount = this._br.ReadInt32();
			this.Int8ValueCount = this._br.ReadInt32();
			this.Int16ValueCount = this._br.ReadInt32();
			this.Int32ValueCount = this._br.ReadInt32();
			this.Int64ValueCount = this._br.ReadInt32();
			this.UInt8ValueCount = this._br.ReadInt32();
			this.UInt16ValueCount = this._br.ReadInt32();
			this.UInt32ValueCount = this._br.ReadInt32();
			this.UInt64ValueCount = this._br.ReadInt32();
			this.SingleValueCount = this._br.ReadInt32();
			this.DoubleValueCount = this._br.ReadInt32();
			this.GuidValueCount = this._br.ReadInt32();
			this.StringValueCount = this._br.ReadInt32();
			this.LocaleValueCount = this._br.ReadInt32();
			this.EnumValueCount = this._br.ReadInt32();
			this.StrongValueCount = this._br.ReadInt32();
			this.WeakValueCount = this._br.ReadInt32();
			this.ReferenceValueCount = this._br.ReadInt32();
			this.EnumOptionCount = this._br.ReadInt32();

			this.TextLength = this._br.ReadUInt32();
			this.BlobLength = this._isLegacy ? 0 : this._br.ReadUInt32();
		}
	}
}
