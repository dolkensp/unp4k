using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace unforge
{
	public class DataForgeRecordDefinition : DataForgeTypeReader
	{
		public String Name { get => this.StreamReader.ReadBlobAtOffset(this.NameOffset); }
		public String FileName { get => this.StreamReader.ReadTextAtOffset(this.FileNameOffset); }
		public DataForgeStructDefinition StructDefinition { get => this.StreamReader.ReadStructDefinitionAtIndex(this.StructIndex); }

		/// <summary>
		/// Sanitizes a string to be a valid XML element name.
		/// Replaces invalid characters with underscores.
		/// </summary>
		private static String SanitizeXmlName(String name)
		{
			if (String.IsNullOrEmpty(name)) return "_";

			var sb = new StringBuilder(name.Length);

			for (int i = 0; i < name.Length; i++)
			{
				char c = name[i];

				// First character must be letter or underscore
				if (i == 0)
				{
					if (Char.IsLetter(c) || c == '_')
						sb.Append(c);
					else
						sb.Append('_');
				}
				// Subsequent characters can be letters, digits, hyphens, underscores, periods
				else
				{
					if (Char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.')
						sb.Append(c);
					else
						sb.Append('_');
				}
			}

			return sb.ToString();
		}

		public XmlElement ReadAsXml(XmlNode xmlNode = null)
		{
			// Ensure top level node for record
			var sanitizedName = SanitizeXmlName(this.Name);
			if (xmlNode is XmlDocument xmlDocument) xmlNode = xmlDocument.CreateElement(sanitizedName);
			else if (xmlNode is XmlElement xmlRoot) xmlNode = xmlRoot.OwnerDocument.CreateElement(sanitizedName);

			var xmlElement = xmlNode as XmlElement;
			
			xmlElement = this.StreamReader.ReadStructAtIndexAsXml(xmlElement, this.StructIndex, this.VariantIndex);

			if (xmlElement == null) return null;

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
