using Dolkens.Framework.BinaryExtensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Windows.Markup;
using System.Xml;
using System.Xml.Linq;

namespace unforge
{
	public class DataForgeStructDefinition : DataForgeTypeReader
	{
		public static Int32 RecordSizeInBytes = 16;

		public UInt32 NameOffset { get; }
		public String Name { get => this.StreamReader.ReadBlobAtOffset(this.NameOffset); }

        public UInt32 ParentTypeIndex { get; }

		public DataForgeStructDefinition ParentType
		{
			get
			{
				if (this.ParentTypeIndex == 0xFFFFFFFF) return null;
				
				return this.StreamReader.ReadStructDefinitionAtIndex(this.ParentTypeIndex);
			}
		}


        public UInt16 PropertyCount { get; }

        public UInt16 FirstPropertyIndex { get; }

        public UInt32 RecordSize { get; }
		

		public static DataForgeStructDefinition ReadFromStream(DataForge baseStream) => new DataForgeStructDefinition(baseStream);

		private DataForgeStructDefinition(DataForge reader) : base(reader)
		{
			this.NameOffset = reader.ReadUInt32();
			this.ParentTypeIndex = reader.ReadUInt32();
			this.PropertyCount = reader.ReadUInt16();
			this.FirstPropertyIndex = reader.ReadUInt16();
			this.RecordSize = reader.ReadUInt32();
		}

		public IEnumerable<DataForgeStructDefinition> Hierarchy
		{
			get
			{
				if (this.ParentTypeIndex != 0xFFFFFFFF)
					foreach (var parent in this.ParentType.Hierarchy)
						yield return parent;

				yield return this;
			}
		}

		public IEnumerable<DataForgePropertyDefinition> PropertyDefinitions
		{
			get
			{
				foreach (var dataStruct in this.Hierarchy)
				{
					for (var propertyIndex = dataStruct.FirstPropertyIndex; propertyIndex < dataStruct.FirstPropertyIndex + dataStruct.PropertyCount; propertyIndex++)
					{
						yield return this.StreamReader.ReadPropertyDefinitionAtIndex(propertyIndex);
					}
				}
			}
		}

		public IEnumerable<XmlNode> ReadAsXml(XmlNode parentNode)
		{
			foreach (var propertyDefinition in this.PropertyDefinitions)
			{
				if (propertyDefinition.ConversionType == EConversionType.varAttribute) yield return this.ReadValueAsXml(parentNode, propertyDefinition);
				else
				{
					var xmlNode = parentNode.OwnerDocument.CreateElement(propertyDefinition.Name);
					
					foreach (var childNode in this.ReadArrayAsXml(xmlNode, propertyDefinition).Where(x => x != null))
					{
						xmlNode.AppendChild(childNode);
					}

					if (xmlNode.ChildNodes.Count == 0 && xmlNode.Attributes.Count == 0) continue;

					yield return xmlNode;
				}
			}
		}

		public XmlNode ReadValueAsXml(XmlNode parentNode, DataForgePropertyDefinition propertyDefinition, String nameOverride = null)
		{
			try
			{
				switch (propertyDefinition.DataType)
				{
					case EDataType.varClass:
						{
							var dataStruct = this.StreamReader.ReadStructDefinitionAtIndex(propertyDefinition.Index);

							var xmlNode = parentNode.OwnerDocument.CreateElement(nameOverride ?? propertyDefinition.Name);

							foreach (var childNode in dataStruct.ReadAsXml(xmlNode).Where(x => x != null))
							{
								if (childNode is XmlAttribute attribute) xmlNode.Attributes.Append(attribute);
								else if (childNode is XmlElement element) xmlNode.AppendChild(element);
							}

							if (xmlNode.ChildNodes.Count == 0 && xmlNode.Attributes.Count == 0) return null;

							return xmlNode;
						}
					case EDataType.varReference:
						{
							var dataForgeReference = DataForgeReference.ReadFromStream(this.StreamReader);

							if (dataForgeReference.IsNull) return null;

							if (!this.StreamReader.FollowReferences) return parentNode.CreateAttributeWithValue(nameOverride ?? propertyDefinition.Name, dataForgeReference.Value);
							
							var xmlNode = this.StreamReader.ReadRecordByReferenceAsXml(parentNode, dataForgeReference.Value);

							if (xmlNode.ChildNodes.Count == 0 && xmlNode.Attributes.Count == 0) return null;

							return xmlNode;
						}
					case EDataType.varStrongPointer:
						{
							var pointer = DataForgePointer.ReadFromStream(this.StreamReader);

							if (pointer.IsNull) return null;

							var dataStruct = this.StreamReader.ReadStructDefinitionAtIndex(pointer.StructIndex);

							if (dataStruct == null) return null;
							
							if (!this.StreamReader.FollowStrongPointers) return parentNode.CreateElementWithValue(nameOverride ?? propertyDefinition.Name, String.Format("{1}[{2:X4}]", propertyDefinition.DataType, dataStruct.Name, pointer.VariantIndex, pointer.Padding));

							var xmlNode = this.StreamReader.ReadStructAtIndexAsXml(parentNode.OwnerDocument.CreateElement(dataStruct.Name), pointer.StructIndex, pointer.VariantIndex);

							if (xmlNode.ChildNodes.Count == 0 && xmlNode.Attributes.Count == 0) return null;

							if (xmlNode != null) return parentNode.CreateElementWithValue(nameOverride ?? propertyDefinition.Name, xmlNode);

							return null;
						}
					case EDataType.varWeakPointer:
						{
							var pointer = DataForgePointer.ReadFromStream(this.StreamReader);

							return null;

							if (pointer.IsNull) return null;

							var dataStruct = this.StreamReader.ReadStructDefinitionAtIndex(pointer.StructIndex);

							if (dataStruct == null) return null;

							var result = this.StreamReader.ReadStructAtIndexAsXml(parentNode.OwnerDocument.CreateElement(dataStruct.Name), pointer.StructIndex, pointer.VariantIndex);

							if (result != null) return parentNode.CreateElementWithValue(nameOverride ?? propertyDefinition.Name, result);

							return null;
						}
					case EDataType.varLocale:
						{
							var localeIndex = this.StreamReader.ReadUInt32();

							return parentNode.CreateAttributeWithValue(nameOverride ?? propertyDefinition.Name, this.StreamReader.ReadTextAtOffset(localeIndex));
						}
					case EDataType.varString: return parentNode.CreateAttributeWithValue(nameOverride ?? propertyDefinition.Name, this.StreamReader.ReadTextAtOffset(this.StreamReader.ReadUInt32()));
					case EDataType.varEnum: return parentNode.CreateAttributeWithValue(nameOverride ?? propertyDefinition.Name, this.StreamReader.ReadEnumAtOffset(this.StreamReader.ReadUInt32()));
					case EDataType.varBoolean: return parentNode.CreateAttributeWithValue(nameOverride ?? propertyDefinition.Name, this.StreamReader.ReadByte());
					case EDataType.varSingle: return parentNode.CreateAttributeWithValue(nameOverride ?? propertyDefinition.Name, this.StreamReader.ReadSingle());
					case EDataType.varDouble: return parentNode.CreateAttributeWithValue(nameOverride ?? propertyDefinition.Name, this.StreamReader.ReadDouble());
					case EDataType.varGuid: return parentNode.CreateAttributeWithValue(nameOverride ?? propertyDefinition.Name, this.StreamReader.ReadGuid(false));
					case EDataType.varInt8: return parentNode.CreateAttributeWithValue(nameOverride ?? propertyDefinition.Name, this.StreamReader.ReadSByte());
					case EDataType.varInt16: return parentNode.CreateAttributeWithValue(nameOverride ?? propertyDefinition.Name, this.StreamReader.ReadInt16());
					case EDataType.varInt32: return parentNode.CreateAttributeWithValue(nameOverride ?? propertyDefinition.Name, this.StreamReader.ReadInt32());
					case EDataType.varInt64: return parentNode.CreateAttributeWithValue(nameOverride ?? propertyDefinition.Name, this.StreamReader.ReadInt64());
					case EDataType.varUInt8: return parentNode.CreateAttributeWithValue(nameOverride ?? propertyDefinition.Name, this.StreamReader.ReadByte());
					case EDataType.varUInt16: return parentNode.CreateAttributeWithValue(nameOverride ?? propertyDefinition.Name, this.StreamReader.ReadUInt16());
					case EDataType.varUInt32: return parentNode.CreateAttributeWithValue(nameOverride ?? propertyDefinition.Name, this.StreamReader.ReadUInt32());
					case EDataType.varUInt64: return parentNode.CreateAttributeWithValue(nameOverride ?? propertyDefinition.Name, this.StreamReader.ReadUInt64());
				}
			}
			catch (Exception ex)
			{
				return parentNode.CreateAttributeWithValue(nameOverride ?? propertyDefinition.Name, $"Error reading property {propertyDefinition.Name} of type {propertyDefinition.DataType}: {ex}");
			}

			return parentNode.CreateAttributeWithValue(nameOverride ?? propertyDefinition.Name, $"Unhandled Type {propertyDefinition.DataType}");
		}

		public IEnumerable<XmlElement> ReadArrayAsXml(XmlNode parentNode, DataForgePropertyDefinition propertyDefinition)
		{
			var arrayCount = this.StreamReader.ReadUInt32();
			var firstIndex = this.StreamReader.ReadUInt32();

			for (UInt16 i = 0; i < arrayCount; i++)
			{
				yield return this.ReadArrayValueAsXml(parentNode, propertyDefinition, firstIndex, i);
			}
		}

		public XmlElement ReadArrayValueAsXml(XmlNode parentNode, DataForgePropertyDefinition propertyDefinition, UInt32 firstIndex, UInt16 offset)
		{
			try
			{
				switch (propertyDefinition.DataType)
				{
					case EDataType.varBoolean: return parentNode.CreateElementWithValue($"Bool", this.StreamReader.ReadBooleanAtIndex(firstIndex + offset).Value ? 1 : 0);

					case EDataType.varSingle: return parentNode.CreateElementWithValue($"Single", this.StreamReader.ReadSingleAtIndex(firstIndex + offset).Value);
					case EDataType.varDouble: return parentNode.CreateElementWithValue($"Double", this.StreamReader.ReadDoubleAtIndex(firstIndex + offset).Value);

					case EDataType.varGuid: return parentNode.CreateElementWithValue($"Guid", this.StreamReader.ReadGuidAtIndex(firstIndex + offset).Value);
					case EDataType.varReference:

						if (this.StreamReader.FollowReferences)
						{
							var dataForgeReference = this.StreamReader.ReadReferenceAtIndex(firstIndex + offset);
							return this.StreamReader.ReadRecordByReferenceAsXml(parentNode, dataForgeReference.Value) as XmlElement;
						}

						return parentNode.CreateElementWithValue($"Reference", this.StreamReader.ReadReferenceAtIndex(firstIndex + offset).Value);
				
					case EDataType.varUInt8: return parentNode.CreateElementWithValue($"UInt8", this.StreamReader.ReadUInt8AtIndex(firstIndex + offset).Value);
					case EDataType.varUInt16: return parentNode.CreateElementWithValue($"UInt16", this.StreamReader.ReadUInt16AtIndex(firstIndex + offset).Value);
					case EDataType.varUInt32: return parentNode.CreateElementWithValue($"UInt32", this.StreamReader.ReadUInt32AtIndex(firstIndex + offset).Value);
					case EDataType.varUInt64: return parentNode.CreateElementWithValue($"UInt64", this.StreamReader.ReadUInt64AtIndex(firstIndex + offset).Value);
					
					case EDataType.varInt8: return parentNode.CreateElementWithValue($"Int8", this.StreamReader.ReadInt8AtIndex(firstIndex + offset).Value);
					case EDataType.varInt16: return parentNode.CreateElementWithValue($"Int16", this.StreamReader.ReadInt16AtIndex(firstIndex + offset).Value);
					case EDataType.varInt32: return parentNode.CreateElementWithValue($"Int32", this.StreamReader.ReadInt32AtIndex(firstIndex + offset).Value);
					case EDataType.varInt64: return parentNode.CreateElementWithValue($"Int64", this.StreamReader.ReadInt64AtIndex(firstIndex + offset).Value);
					
					case EDataType.varString: return parentNode.CreateElementWithValue($"String", this.StreamReader.ReadStringAtIndex(firstIndex + offset).Value);
					case EDataType.varLocale: return parentNode.CreateElementWithValue($"LocID", this.StreamReader.ReadLocaleAtIndex(firstIndex + offset).Value);
					case EDataType.varEnum: return parentNode.CreateElementWithValue($"Enum", this.StreamReader.ReadEnumValueAtIndex(firstIndex + offset).Value);

					case EDataType.varWeakPointer:
						{
							var pointer = this.StreamReader.ReadWeakPointerAtIndex(offset + firstIndex);
							var dataMapping = this.StreamReader.ReadDataMappingAtIndex(pointer.StructIndex);

							return this.StreamReader.ReadStructAtIndexAsXml(parentNode.OwnerDocument.CreateElement(dataMapping.Name), pointer.StructIndex, pointer.VariantIndex);
						}
					case EDataType.varStrongPointer:
						{
							var pointer = this.StreamReader.ReadStrongPointerAtIndex(offset + firstIndex);
							var dataMapping = this.StreamReader.ReadDataMappingAtIndex(pointer.StructIndex);

							return this.StreamReader.ReadStructAtIndexAsXml(parentNode.OwnerDocument.CreateElement(dataMapping.Name), pointer.StructIndex, pointer.VariantIndex);
						}
					case EDataType.varClass:
						{
							var structIndex = (UInt32)(propertyDefinition.Index);
							var dataMapping = this.StreamReader.ReadDataMappingAtIndex(structIndex);
							var dataStruct = this.StreamReader.ReadStructDefinitionAtIndex(structIndex);

							return this.StreamReader.ReadStructAtIndexAsXml(parentNode.OwnerDocument.CreateElement(dataMapping.Name), propertyDefinition.Index, firstIndex + offset);
						}
				}
			}
			catch (Exception ex)
			{
				return parentNode.CreateElementWithValue(propertyDefinition.Name, $"Error reading array property {propertyDefinition.Name} of type {propertyDefinition.DataType}: {ex}");
			}

			return parentNode.CreateElementWithValue(propertyDefinition.Name, "TBC");
		}

		public override String ToString()
        {
            return String.Format("<{0} />", this.Name);
        }
    }
}
