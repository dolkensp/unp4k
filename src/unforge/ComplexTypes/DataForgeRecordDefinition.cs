using System;
using System.IO;
using System.Linq;
using System.Xml;

namespace unforge
{
	public class DataForgeRecordDefinition : DataForgeTypeReader
	{
		public String Name { get => this.StreamReader.ReadBlobAtOffset(this.NameOffset); }
		public String FileName { get => this.StreamReader.ReadTextAtOffset(this.FileNameOffset); }
		public DataForgeStructDefinition StructDefinition { get => this.StreamReader.ReadStructDefinitionAtIndex(this.StructIndex); }


		public XmlElement ReadAsXml(XmlNode xmlNode = null)
		{
			// Ensure top level node for record
			if (xmlNode is XmlDocument xmlDocument) xmlNode = xmlDocument.CreateElement(this.Name);
			else if (xmlNode is XmlElement xmlRoot) xmlNode = xmlRoot.OwnerDocument.CreateElement(this.Name);

			var xmlElement = xmlNode as XmlElement;
			
			xmlElement = this.StreamReader.ReadStructAtIndexAsXml(xmlElement, this.StructIndex, this.VariantIndex);

			// if (!this.StreamReader.FollowReferences)
			// {
			xmlElement.AddAttribute("__type", this.StructDefinition.Name);
			xmlElement.AddAttribute("__ref", this.Hash);
			xmlElement.AddAttribute("__path", this.FileName);
			// }

			return xmlElement;
		}

		public static Int32 RecordSizeInBytes = 32;

		public UInt32 NameOffset { get; }
		public UInt32 FileNameOffset { get; }

		public String __structIndex { get { return String.Format("{0:X4}", this.StructIndex); } }
		public UInt32 StructIndex { get; }

		public Guid Hash { get; }

		public String __variantIndex { get { return String.Format("{0:X4}", this.VariantIndex); } }
		public UInt16 VariantIndex { get; }

		public String __recordSize { get { return String.Format("{0:X4}", this.RecordSize); } }
		public UInt16 RecordSize { get; }

		private DataForgeRecordDefinition(DataForge streamReader) : base(streamReader)
		{
			this.NameOffset = this.StreamReader.ReadUInt32();

			if (!this.StreamReader.IsLegacy)
			{
				this.FileNameOffset = this.StreamReader.ReadUInt32();
			}

			this.StructIndex = this.StreamReader.ReadUInt32();
			this.Hash = this.StreamReader.ReadGuid(false) ?? Guid.Empty;

			this.VariantIndex = this.StreamReader.ReadUInt16();
			this.RecordSize = this.StreamReader.ReadUInt16();
		}

		public static DataForgeRecordDefinition ReadFromStream(DataForge reader) => new DataForgeRecordDefinition(reader);
		
		public override String ToString()
		{
			return String.Format("<{0} {1:X4} />", this.Name, this.StructIndex);
		}
	}
}
