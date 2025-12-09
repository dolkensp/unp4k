using Dolkens.Framework.BinaryExtensions;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Xml;

namespace unforge
{
	public class DataForge : BinaryReader
	{
		public Boolean IsLegacy;
		public Boolean FollowReferences { get => DataForge.MaxReferenceDepth > this.StructStack.Count; }
		public Boolean FollowWeakPointers { get; } = false;
		public Boolean FollowStrongPointers { get => DataForge.MaxPointerDepth > this.RecordStack.Count; }

		public static Int32 MaxReferenceDepth { get; set; } = 1;
		public static Int32 MaxPointerDepth { get; set; } = 100;
		public static Int32 MaxNodes { get; set; } = 10000;

		public Int32 FileVersion { get; }
		public Int64 Length { get => this.BaseStream.Length; }
		public Int64 Position
		{
			get => this.BaseStream.Position;
			set => this.BaseStream.Position = value;
		}

		public Int32 StructDefinitionCount { get; }
		public Int32 PropertyDefinitionCount { get; }
		public Int32 EnumDefinitionCount { get; }
		public Int32 DataMappingCount { get; }
		public Int32 RecordDefinitionCount { get; }

		public Int32 BooleanValueCount { get; }
		public Int32 Int8ValueCount { get; }
		public Int32 Int16ValueCount { get; }
		public Int32 Int32ValueCount { get; }
		public Int32 Int64ValueCount { get; }
		public Int32 UInt8ValueCount { get; }
		public Int32 UInt16ValueCount { get; }
		public Int32 UInt32ValueCount { get; }
		public Int32 UInt64ValueCount { get; }

		public Int32 SingleValueCount { get; }
		public Int32 DoubleValueCount { get; }
		public Int32 GuidValueCount { get; }
		public Int32 StringValueCount { get; }
		public Int32 LocaleValueCount { get; }
		public Int32 EnumValueCount { get; }
		public Int32 StrongValueCount { get; }
		public Int32 WeakValueCount { get; }

		public Int32 ReferenceValueCount { get; }
		public Int32 EnumOptionCount { get; }
		public UInt32 TextLength { get; }
		public UInt32 BlobLength { get; }

		private Int64 StructDefinitionOffset { get => this.IsLegacy ? 0x74 : 0x78; }
		public Int64 PropertyDefinitionOffset { get => this.StructDefinitionOffset + this.StructDefinitionCount * DataForgeStructDefinition.RecordSizeInBytes; }
		public Int64 EnumDefinitionOffset { get => this.PropertyDefinitionOffset + this.PropertyDefinitionCount * DataForgePropertyDefinition.RecordSizeInBytes; }
		public Int64 DataMappingOffset { get => this.EnumDefinitionOffset + this.EnumDefinitionCount * DataForgeEnumDefinition.RecordSizeInBytes; }
		public Int64 RecordDefinitionOffset { get => this.DataMappingOffset + this.DataMappingCount * (this.IsLegacy ? DataForgeDataMapping.RecordSizeInBytes : DataForgeDataMapping.RecordSizeInBytesV6); }
		public Int64 Int8ValueOffset { get => this.RecordDefinitionOffset + this.RecordDefinitionCount * DataForgeRecordDefinition.RecordSizeInBytes; }
		public Int64 Int16ValueOffset { get => this.Int8ValueOffset + this.Int8ValueCount * DataForgeInt8.RecordSizeInBytes; }
		public Int64 Int32ValueOffset { get => this.Int16ValueOffset + this.Int16ValueCount * DataForgeInt16.RecordSizeInBytes; }
		public Int64 Int64ValueOffset { get => this.Int32ValueOffset + this.Int32ValueCount * DataForgeInt32.RecordSizeInBytes; }
		public Int64 UInt8ValueOffset { get => this.Int64ValueOffset + this.Int64ValueCount * DataForgeInt64.RecordSizeInBytes; }
		public Int64 UInt16ValueOffset { get => this.UInt8ValueOffset + this.UInt8ValueCount * DataForgeUInt8.RecordSizeInBytes; }
		public Int64 UInt32ValueOffset { get => this.UInt16ValueOffset + this.UInt16ValueCount * DataForgeUInt16.RecordSizeInBytes; }
		public Int64 UInt64ValueOffset { get => this.UInt32ValueOffset + this.UInt32ValueCount * DataForgeUInt32.RecordSizeInBytes; }
		public Int64 BooleanValueOffset { get => this.UInt64ValueOffset + this.UInt64ValueCount * DataForgeUInt64.RecordSizeInBytes; }
		public Int64 SingleValueOffset { get => this.BooleanValueOffset + this.BooleanValueCount * DataForgeBoolean.RecordSizeInBytes; }
		public Int64 DoubleValueOffset { get => this.SingleValueOffset + this.SingleValueCount * DataForgeSingle.RecordSizeInBytes; }
		public Int64 GuidValueOffset { get => this.DoubleValueOffset + this.DoubleValueCount * DataForgeDouble.RecordSizeInBytes; }
		public Int64 StringValueOffset { get => this.GuidValueOffset + this.GuidValueCount * DataForgeGuid.RecordSizeInBytes; }
		public Int64 LocaleValueOffset { get => this.StringValueOffset + this.StringValueCount * DataForgeStringLookup.RecordSizeInBytes; }
		public Int64 EnumValueOffset { get => this.LocaleValueOffset + this.LocaleValueCount * DataForgeLocale.RecordSizeInBytes; }
		public Int64 StrongValueOffset { get => this.EnumValueOffset + this.EnumValueCount * DataForgeEnum.RecordSizeInBytes; }
		public Int64 WeakValueOffset { get => this.StrongValueOffset + this.StrongValueCount * DataForgePointer.RecordSizeInBytes; }
		public Int64 ReferenceValueOffset { get => this.WeakValueOffset + this.WeakValueCount * DataForgePointer.RecordSizeInBytes; }
		public Int64 EnumOptionOffset { get => this.ReferenceValueOffset + this.ReferenceValueCount * DataForgeReference.RecordSizeInBytes; }
		public Int64 TextOffset { get => this.EnumOptionOffset + this.EnumOptionCount * DataForgeStringLookup.RecordSizeInBytes; }
		public Int64 BlobOffset { get => this.TextOffset + this.TextLength; }
		public Int64 DataOffset { get => this.BlobOffset + this.BlobLength; }

		public DataForge(Stream stream) : base(stream)
		{
			this.BaseStream.Seek(0, SeekOrigin.Begin);

			_ = this.ReadUInt16();
			_ = this.ReadUInt16();

			this.FileVersion = this.ReadInt32();

			this.IsLegacy = stream.Length < 0x0e2e00 && this.FileVersion < 6;

			if (!this.IsLegacy)
			{
				_ = this.ReadUInt16();
				_ = this.ReadUInt16();
				_ = this.ReadUInt16();
				_ = this.ReadUInt16();
			}

			this.StructDefinitionCount = this.ReadInt32();
			this.PropertyDefinitionCount = this.ReadInt32();
			this.EnumDefinitionCount = this.ReadInt32();
			this.DataMappingCount = this.ReadInt32();
			this.RecordDefinitionCount = this.ReadInt32();
			this.BooleanValueCount = this.ReadInt32();
			this.Int8ValueCount = this.ReadInt32();
			this.Int16ValueCount = this.ReadInt32();
			this.Int32ValueCount = this.ReadInt32();
			this.Int64ValueCount = this.ReadInt32();
			this.UInt8ValueCount = this.ReadInt32();
			this.UInt16ValueCount = this.ReadInt32();
			this.UInt32ValueCount = this.ReadInt32();
			this.UInt64ValueCount = this.ReadInt32();
			this.SingleValueCount = this.ReadInt32();
			this.DoubleValueCount = this.ReadInt32();
			this.GuidValueCount = this.ReadInt32();
			this.StringValueCount = this.ReadInt32();
			this.LocaleValueCount = this.ReadInt32();
			this.EnumValueCount = this.ReadInt32();
			this.StrongValueCount = this.ReadInt32();
			this.WeakValueCount = this.ReadInt32();
			this.ReferenceValueCount = this.ReadInt32();
			this.EnumOptionCount = this.ReadInt32();

			this.TextLength = this.ReadUInt32();
			this.BlobLength = this.IsLegacy ? 0 : this.ReadUInt32();

			this.PathToRecordMap = new Dictionary<String, Int32> { };
			this.ReferenceToRecordMap = new Dictionary<Guid, Int32> { };

			// this.ReportOffsets();

			foreach (var recordIndex in Enumerable.Range(0, this.RecordDefinitionCount))
			{
				this.Position = this.RecordDefinitionOffset + recordIndex * DataForgeRecordDefinition.RecordSizeInBytes;
				var record = DataForgeRecordDefinition.ReadFromStream(this);

				var filename = record.FileName;
				this.PathToRecordMap[filename] = recordIndex;
				this.ReferenceToRecordMap[record.Hash] = recordIndex;
			}

			var lastOffset = 0;

			this.StructToDataOffsetMap = new Dictionary<UInt32, Int64> { };
			foreach (var dataMappingIndex in Enumerable.Range(0, this.DataMappingCount))
			{
				var dataMapping = this.ReadDataMappingAtIndex(dataMappingIndex);
				var dataStruct = this.ReadStructDefinitionAtIndex(dataMappingIndex);

				if (!this.StructToDataOffsetMap.ContainsKey(dataMapping.StructIndex)) this.StructToDataOffsetMap[dataMapping.StructIndex] = lastOffset;
				lastOffset += (Int32)(dataMapping.StructCount * dataStruct.RecordSize);
			}

			Debug.Assert((this.DataOffset + lastOffset) == this.BaseStream.Length, "Data / Stream length mismatch");
		}

		/// <summary>
		/// Converts Paths to RecordIndexes
		/// </summary>
		public Dictionary<String, Int32> PathToRecordMap { get; }

		/// <summary>
		/// Converts References to RecordIndexes
		/// </summary>
		public Dictionary<Guid, Int32> ReferenceToRecordMap { get; }

		/// <summary>
		/// Converts StructIndex and VariantIndex to dataOffsets
		/// </summary>
		public Dictionary<UInt32, Int64> StructToDataOffsetMap { get; }

		internal String ReadEnumAtOffset(UInt32 enumValueOffset) => this.ReadTextAtOffset(enumValueOffset);

		internal String ReadTextAtOffset(Int64 offset)
		{
			if (offset > this.TextLength) throw new IndexOutOfRangeException($"Offset {offset} is out of range for Text values (length: {this.TextLength})");

			var position = this.Position;

			try
			{
				this.Position = this.TextOffset + offset;

				return this.ReadCString();
			}
			finally
			{
				this.Position = position;
			}
		}

		internal String ReadBlobAtOffset(Int64 offset)
		{
			if (this.FileVersion < 6) return this.ReadTextAtOffset(offset);

			if (offset > this.BlobLength) throw new IndexOutOfRangeException($"Offset {offset} is out of range for Blob values (length: {this.TextLength})");

			var position = this.Position;

			try
			{
				this.Position = this.BlobOffset + offset;

				return this.ReadCString();
			}
			finally
			{
				this.Position = position;
			}
		}

		internal DataForgeDataMapping ReadDataMappingAtIndex(Int64 index)
		{
			if (index > this.DataMappingCount) throw new IndexOutOfRangeException($"Index {index} is out of range for Data Mapping values (count: {this.DataMappingCount})");

			var position = this.Position;

			try
			{
				this.Position = this.DataMappingOffset + index * (this.IsLegacy ? DataForgeDataMapping.RecordSizeInBytes : DataForgeDataMapping.RecordSizeInBytesV6);

				return DataForgeDataMapping.ReadFromStream(this);
			}
			finally
			{
				this.Position = position;
			}
		}

		internal DataForgeRecordDefinition ReadRecordDefinitionAtIndex(Int64 index)
		{
			if (index > this.RecordDefinitionCount) throw new IndexOutOfRangeException($"Index {index} is out of range for Record Definition values (count: {this.RecordDefinitionCount})");

			var position = this.Position;

			try
			{
				this.Position = this.RecordDefinitionOffset + index * DataForgeRecordDefinition.RecordSizeInBytes;

				return DataForgeRecordDefinition.ReadFromStream(this);
			}
			finally
			{
				this.Position = position;
			}
		}

		public List<(UInt32, UInt32)> StructStack { get; set; } = new List<(UInt32, UInt32)> { };

		public XmlElement ReadStructAtIndexAsXml(XmlElement xmlNode, UInt32 structIndex, UInt32 variantIndex)
		{
			var position = this.Position;

			if (this.StructStack.Count > DataForge.MaxPointerDepth || this.StructStack.Contains((structIndex, variantIndex))) return null;

			try
			{
				this.StructStack.Add((structIndex, variantIndex));

				var dataStruct = this.ReadStructDefinitionAtIndex(structIndex);
				var dataMapping = this.ReadDataMappingAtIndex(structIndex);

				if (dataMapping.StructCount < variantIndex) throw new IndexOutOfRangeException($"Variant Index {variantIndex} is out of range for struct {dataStruct.Name} with count {dataMapping.StructCount}");

				if (this.StructToDataOffsetMap.TryGetValue(structIndex, out long value))
				{
					this.Position = this.DataOffset + value + (dataStruct.RecordSize * variantIndex);
				}
				else
				{
					throw new KeyNotFoundException($"Struct Index {structIndex} not found in Struct to Data Offset Map");
					// this.Position = this.DataOffset;
				}

				return this.ReadStructAsXml(xmlNode, dataStruct);
			}
			finally
			{
				this.Position = position;

				this.StructStack.Remove((structIndex, variantIndex));
			}
		}

		public XmlElement ReadStructAsXml(XmlElement xmlNode, DataForgeStructDefinition dataStruct)
		{
			var i = 0;

			foreach (var childNode in dataStruct.ReadAsXml(xmlNode).Where(x => x != null))
			{
				if (childNode is XmlAttribute attribute) xmlNode.Attributes.Append(attribute);
				else if (childNode is XmlElement element) xmlNode.AppendChild(element);

				if (++i >= DataForge.MaxNodes) break;
			}

			if (xmlNode.ChildNodes.Count == 0 && xmlNode.Attributes.Count == 0) return null;

			return xmlNode;
		}

		internal DataForgeStructDefinition ReadStructDefinitionAtIndex(Int64 index)
		{
			if (index > this.StructDefinitionCount) throw new IndexOutOfRangeException($"Index {index} is out of range for Struct Definition values (count: {this.StructDefinitionCount})");

			var position = this.Position;

			try
			{
				this.Position = this.StructDefinitionOffset + index * DataForgeStructDefinition.RecordSizeInBytes;

				return DataForgeStructDefinition.ReadFromStream(this);
			}
			finally
			{
				this.Position = position;
			}
		}

		internal DataForgePropertyDefinition ReadPropertyDefinitionAtIndex(Int64 index)
		{
			if (index > this.PropertyDefinitionCount) throw new IndexOutOfRangeException($"Index {index} is out of range for Property Definition values (count: {this.PropertyDefinitionCount})");

			var position = this.Position;

			try
			{
				this.Position = this.PropertyDefinitionOffset + index * DataForgePropertyDefinition.RecordSizeInBytes;

				return DataForgePropertyDefinition.ReadFromStream(this);
			}
			finally
			{
				this.Position = position;
			}
		}

		internal DataForgeBoolean ReadBooleanAtIndex(Int64 index)
		{
			if (index > this.BooleanValueCount) throw new IndexOutOfRangeException($"Index {index} is out of range for Boolean values (count: {this.BooleanValueCount})");

			var position = this.Position;

			try
			{
				this.Position = this.BooleanValueOffset + index * DataForgeBoolean.RecordSizeInBytes;

				return DataForgeBoolean.ReadFromStream(this);
			}
			finally
			{
				this.Position = position;
			}
		}

		internal DataForgeDouble ReadDoubleAtIndex(Int64 index)
		{
			if (index > this.DoubleValueCount) throw new IndexOutOfRangeException($"Index {index} is out of range for Double values (count: {this.DoubleValueCount})");

			var position = this.Position;

			try
			{
				this.Position = this.DoubleValueOffset + index * DataForgeDouble.RecordSizeInBytes;

				return DataForgeDouble.ReadFromStream(this);
			}
			finally
			{
				this.Position = position;
			}
		}

		internal DataForgeEnum ReadEnumOptionAtIndex(Int64 index)
		{
			if (index > this.EnumValueCount) throw new IndexOutOfRangeException($"Index {index} is out of range for Enum values (count: {this.EnumOptionCount})");

			var position = this.Position;

			try
			{
				this.Position = this.EnumOptionOffset + index * DataForgeEnum.RecordSizeInBytes;

				return DataForgeEnum.ReadFromStream(this);
			}
			finally
			{
				this.Position = position;
			}
		}

		internal DataForgeEnumDefinition ReadEnumDefinitionAtIndex(Int64 index)
		{
			if (index > this.EnumValueCount) throw new IndexOutOfRangeException($"Index {index} is out of range for Enum values (count: {this.EnumDefinitionCount})");

			var position = this.Position;

			try
			{
				this.Position = this.EnumDefinitionOffset + index * DataForgeEnumDefinition.RecordSizeInBytes;

				return DataForgeEnumDefinition.ReadFromStream(this);
			}
			finally
			{
				this.Position = position;
			}
		}

		internal DataForgeEnum ReadEnumValueAtIndex(Int64 index)
		{
			if (index > this.EnumValueCount) throw new IndexOutOfRangeException($"Index {index} is out of range for Enum values (count: {this.EnumValueCount})");

			var position = this.Position;

			try
			{
				this.Position = this.EnumValueOffset + index * DataForgeEnum.RecordSizeInBytes;

				return DataForgeEnum.ReadFromStream(this);
			}
			finally
			{
				this.Position = position;
			}
		}

		internal DataForgeGuid ReadGuidAtIndex(Int64 index)
		{
			if (index > this.GuidValueCount) throw new IndexOutOfRangeException($"Index {index} is out of range for Guid values (count: {this.GuidValueCount})");

			var position = this.Position;

			try
			{
				this.Position = this.GuidValueOffset + index * DataForgeGuid.RecordSizeInBytes;

				return DataForgeGuid.ReadFromStream(this);
			}
			finally
			{
				this.Position = position;
			}
		}

		internal DataForgeInt16 ReadInt16AtIndex(Int64 index)
		{
			if (index > this.Int16ValueCount) throw new IndexOutOfRangeException($"Index {index} is out of range for Int16 values (count: {this.Int16ValueCount})");

			var position = this.Position;

			try
			{
				this.Position = this.Int16ValueOffset + index * DataForgeInt16.RecordSizeInBytes;

				return DataForgeInt16.ReadFromStream(this);
			}
			finally
			{
				this.Position = position;
			}
		}

		internal DataForgeInt32 ReadInt32AtIndex(Int64 index)
		{
			if (index > this.Int32ValueCount) throw new IndexOutOfRangeException($"Index {index} is out of range for Int32 values (count: {this.Int32ValueCount})");

			var position = this.Position;

			try
			{
				this.Position = this.Int32ValueOffset + index * DataForgeInt32.RecordSizeInBytes;

				return DataForgeInt32.ReadFromStream(this);
			}
			finally
			{
				this.Position = position;
			}
		}

		internal DataForgeInt64 ReadInt64AtIndex(Int64 index)
		{
			if (index > this.Int64ValueCount) throw new IndexOutOfRangeException($"Index {index} is out of range for Int64 values (count: {this.Int64ValueCount})");

			var position = this.Position;

			try
			{
				this.Position = this.Int64ValueOffset + index * DataForgeInt64.RecordSizeInBytes;

				return DataForgeInt64.ReadFromStream(this);
			}
			finally
			{
				this.Position = position;
			}
		}

		internal DataForgeInt8 ReadInt8AtIndex(Int64 index)
		{
			if (index > this.Int8ValueCount) throw new IndexOutOfRangeException($"Index {index} is out of range for Int8 values (count: {this.Int8ValueCount})");

			var position = this.Position;

			try
			{
				this.Position = this.Int8ValueOffset + index * DataForgeInt8.RecordSizeInBytes;

				return DataForgeInt8.ReadFromStream(this);
			}
			finally
			{
				this.Position = position;
			}
		}

		internal DataForgeLocale ReadLocaleAtIndex(Int64 index)
		{
			if (index > this.LocaleValueCount) throw new IndexOutOfRangeException($"Index {index} is out of range for Locale values (count: {this.LocaleValueCount})");

			var position = this.Position;

			try
			{
				this.Position = this.LocaleValueOffset + index * DataForgeLocale.RecordSizeInBytes;

				return DataForgeLocale.ReadFromStream(this);
			}
			finally
			{
				this.Position = position;
			}
		}

		internal DataForgeReference ReadReferenceAtIndex(Int64 index)
		{
			if (index > this.ReferenceValueCount) throw new IndexOutOfRangeException($"Index {index} is out of range for Reference values (count: {this.ReferenceValueCount})");

			var position = this.Position;

			try
			{
				this.Position = this.ReferenceValueOffset + index * DataForgeReference.RecordSizeInBytes;

				return DataForgeReference.ReadFromStream(this);
			}
			finally
			{
				this.Position = position;
			}
		}

		internal DataForgeSingle ReadSingleAtIndex(Int64 index)
		{
			if (index > this.SingleValueCount) throw new IndexOutOfRangeException($"Index {index} is out of range for Single values (count: {this.SingleValueCount})");

			var position = this.Position;

			try
			{
				this.Position = this.SingleValueOffset + index * DataForgeSingle.RecordSizeInBytes;

				return DataForgeSingle.ReadFromStream(this);
			}
			finally
			{
				this.Position = position;
			}
		}

		internal DataForgeStringLookup ReadStringAtIndex(Int64 index)
		{
			if (index > this.StringValueCount) throw new IndexOutOfRangeException($"Index {index} is out of range for String values (count: {this.StringValueCount})");

			var position = this.Position;

			try
			{
				this.Position = this.StringValueOffset + index * DataForgeStringLookup.RecordSizeInBytes;

				return DataForgeStringLookup.ReadFromStream(this);
			}
			finally
			{
				this.Position = position;
			}
		}

		internal DataForgeUInt16 ReadUInt16AtIndex(Int64 index)
		{
			if (index > this.UInt16ValueCount) throw new IndexOutOfRangeException($"Index {index} is out of range for UInt16 values (count: {this.UInt16ValueCount})");

			var position = this.Position;

			try
			{
				this.Position = this.UInt16ValueOffset + index * DataForgeUInt16.RecordSizeInBytes;

				return DataForgeUInt16.ReadFromStream(this);
			}
			finally
			{
				this.Position = position;
			}
		}

		internal DataForgeUInt32 ReadUInt32AtIndex(Int64 index)
		{
			if (index > this.UInt32ValueCount) throw new IndexOutOfRangeException($"Index {index} is out of range for UInt32 values (count: {this.UInt32ValueCount})");

			var position = this.Position;

			try
			{
				this.Position = this.UInt32ValueOffset + index * DataForgeUInt32.RecordSizeInBytes;

				return DataForgeUInt32.ReadFromStream(this);
			}
			finally
			{
				this.Position = position;
			}
		}

		internal DataForgeUInt64 ReadUInt64AtIndex(Int64 index)
		{
			if (index > this.UInt64ValueCount) throw new IndexOutOfRangeException($"Index {index} is out of range for UInt64 values (count: {this.UInt64ValueCount})");

			var position = this.Position;

			try
			{
				this.Position = this.UInt64ValueOffset + index * DataForgeUInt64.RecordSizeInBytes;

				return DataForgeUInt64.ReadFromStream(this);
			}
			finally
			{
				this.Position = position;
			}
		}

		internal DataForgeUInt8 ReadUInt8AtIndex(Int64 index)
		{
			if (index > this.UInt8ValueCount) throw new IndexOutOfRangeException($"Index {index} is out of range for UInt8 values (count: {this.UInt8ValueCount})");

			var position = this.Position;

			try
			{
				this.Position = this.UInt8ValueOffset + index * DataForgeUInt8.RecordSizeInBytes;

				return DataForgeUInt8.ReadFromStream(this);
			}
			finally
			{
				this.Position = position;
			}
		}

		internal DataForgePointer ReadWeakPointerAtIndex(Int64 index)
		{
			if (index > this.WeakValueCount) throw new IndexOutOfRangeException($"Index {index} is out of range for Weak Pointer values (count: {this.WeakValueCount})");

			var position = this.Position;

			try
			{
				this.Position = this.WeakValueOffset + index * DataForgePointer.RecordSizeInBytes;

				return DataForgePointer.ReadFromStream(this);
			}
			finally
			{
				this.Position = position;
			}
		}

		internal DataForgePointer ReadStrongPointerAtIndex(Int64 index)
		{
			if (index > this.StrongValueCount) throw new IndexOutOfRangeException($"Index {index} is out of range for Strong Pointer values (count: {this.StrongValueCount})");

			var position = this.Position;

			try
			{
				this.Position = this.StrongValueOffset + index * DataForgePointer.RecordSizeInBytes;

				return DataForgePointer.ReadFromStream(this);
			}
			finally
			{
				this.Position = position;
			}
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

		public XmlElement ReadRecordByPathAsXml(String path)
		{
			if (!this.PathToRecordMap.TryGetValue(path, out var recordIndex)) throw new FileNotFoundException();

			var xml = new XmlDocument();

			lock (this.BaseStream)
			{
				return this.ReadRecordAtIndexAsXml(xml, recordIndex);
			}
		}

		public XmlElement ReadRecordByReferenceAsXml(XmlNode xmlNode, Guid reference)
		{
			if (!this.ReferenceToRecordMap.TryGetValue(reference, out var recordIndex)) throw new FileNotFoundException();

			return this.ReadRecordAtIndexAsXml(xmlNode, recordIndex);
		}

		public List<Int32> RecordStack { get; set; } = new List<Int32> { };

		public XmlElement ReadRecordAtIndexAsXml(XmlNode xmlNode, Int32 recordIndex)
		{
			var position = this.Position;

			if (this.RecordStack.Count > DataForge.MaxReferenceDepth || this.RecordStack.Contains(recordIndex)) return null;

			try
			{
				this.RecordStack.Add(recordIndex);

				var record = this.ReadRecordDefinitionAtIndex(recordIndex);

				return record.ReadAsXml(xmlNode);
			}
			catch (Exception ex)
			{
				return xmlNode.CreateElementWithValue("Error", ex.Message);
			}
			finally
			{
				this.Position = position;

				this.RecordStack.Remove(recordIndex);
			}
		}

		private void ReportOffsets()
		{
			Console.WriteLine($"StructDefinitionOffset:   {this.StructDefinitionOffset:X8}\t{this.StructDefinitionCount:X6}");
			Console.WriteLine($"PropertyDefinitionOffset: {this.PropertyDefinitionOffset:X8}\t{this.PropertyDefinitionCount:X6}");
			Console.WriteLine($"EnumDefinitionOffset:     {this.EnumDefinitionOffset:X8}\t{this.EnumDefinitionCount:X6}");
			Console.WriteLine($"DataMappingOffset:        {this.DataMappingOffset:X8}\t{this.DataMappingCount:X6}");
			Console.WriteLine($"RecordDefinitionOffset:   {this.RecordDefinitionOffset:X8}\t{this.RecordDefinitionCount:X6}");
			Console.WriteLine($"Int8ValueOffset:          {this.Int8ValueOffset:X8}\t{this.Int8ValueCount:X6}");
			Console.WriteLine($"Int16ValueOffset:         {this.Int16ValueOffset:X8}\t{this.Int16ValueCount:X6}");
			Console.WriteLine($"Int32ValueOffset:         {this.Int32ValueOffset:X8}\t{this.Int32ValueCount:X6}");
			Console.WriteLine($"Int64ValueOffset:         {this.Int64ValueOffset:X8}\t{this.Int64ValueCount:X6}");
			Console.WriteLine($"UInt8ValueOffset:         {this.UInt8ValueOffset:X8}\t{this.UInt8ValueCount:X6}");
			Console.WriteLine($"UInt16ValueOffset:        {this.UInt16ValueOffset:X8}\t{this.UInt16ValueCount:X6}");
			Console.WriteLine($"UInt32ValueOffset:        {this.UInt32ValueOffset:X8}\t{this.UInt32ValueCount:X6}");
			Console.WriteLine($"UInt64ValueOffset:        {this.UInt64ValueOffset:X8}\t{this.UInt64ValueCount:X6}");
			Console.WriteLine($"BooleanValueOffset:       {this.BooleanValueOffset:X8}\t{this.BooleanValueCount:X6}");
			Console.WriteLine($"SingleValueOffset:        {this.SingleValueOffset:X8}\t{this.SingleValueCount:X6}");
			Console.WriteLine($"DoubleValueOffset:        {this.DoubleValueOffset:X8}\t{this.DoubleValueCount:X6}");
			Console.WriteLine($"GuidValueOffset:          {this.GuidValueOffset:X8}\t{this.GuidValueCount:X6}");
			Console.WriteLine($"StringValueOffset:        {this.StringValueOffset:X8}\t{this.StringValueCount:X6}");
			Console.WriteLine($"LocaleValueOffset:        {this.LocaleValueOffset:X8}\t{this.LocaleValueCount:X6}");
			Console.WriteLine($"EnumValueOffset:          {this.EnumValueOffset:X8}\t{this.EnumValueCount:X6}");
			Console.WriteLine($"StrongValueOffset:        {this.StrongValueOffset:X8}\t{this.StringValueCount:X6}");
			Console.WriteLine($"WeakValueOffset:          {this.WeakValueOffset:X8}\t{this.WeakValueCount:X6}");
			Console.WriteLine($"ReferenceValueOffset:     {this.ReferenceValueOffset:X8}\t{this.ReferenceValueCount:X6}");
			Console.WriteLine($"EnumOptionOffset:         {this.EnumOptionOffset:X8}\t{this.EnumOptionCount:X6}");
			Console.WriteLine($"TextOffset:               {this.TextOffset:X8}");
			Console.WriteLine($"BlobOffset:               {this.BlobOffset:X8}");
			Console.WriteLine($"DataOffset:               {this.DataOffset:X8}");
			Console.WriteLine($"Length:                   {this.Length:X8}");
		}

		private static readonly XmlWriterSettings _xmlSettings = new XmlWriterSettings
		{
			OmitXmlDeclaration = true,
			Encoding = new UTF8Encoding(false), // UTF-8, no BOM
			Indent = true,
			IndentChars = "  ",
			NewLineChars = "\r\n",
			NewLineHandling = NewLineHandling.Replace,
			ConformanceLevel = ConformanceLevel.Document,
			CheckCharacters = false
		};

		public void Save(String filename)
		{
			foreach (var fileReference in this.PathToRecordMap.Keys)
			{
				var node = this.ReadRecordByPathAsXml(fileReference);

				if (node == null) continue;

				var newPath = Path.Combine(Path.GetDirectoryName(filename), fileReference);

				if (!Directory.Exists(Path.GetDirectoryName(newPath))) Directory.CreateDirectory(Path.GetDirectoryName(newPath));


				using (var fileStream= File.OpenWrite(newPath))
				using (var writer = XmlWriter.Create(fileStream, _xmlSettings))
				{
					node.WriteTo(writer);
					writer.Flush();
				}
			}
		}
	}
}
