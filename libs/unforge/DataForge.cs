using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace unforge
{
    public class ClassMapping
    {
        public XmlNode Node { get; set; }
        public ushort StructIndex { get; set; }
        public int RecordIndex { get; set; }
    }

    public class DataForge : IEnumerable
	{
        internal BinaryReader br;
        internal int FileVersion { get; set; }

        internal DataForgeStructDefinition[] StructDefinitionTable { get; set; }
        internal DataForgePropertyDefinition[] PropertyDefinitionTable { get; set; }
        internal DataForgeEnumDefinition[] EnumDefinitionTable { get; set; }
        internal DataForgeDataMapping[] DataMappingTable { get; set; }
        internal DataForgeRecord[] RecordDefinitionTable { get; set; }
        internal DataForgeStringLookup[] EnumOptionTable { get; set; }
        internal DataForgeString[] ValueTable { get; set; }

        internal DataForgeReference[] Array_ReferenceValues { get; set; }
        internal DataForgeGuid[] Array_GuidValues { get; set; }
        internal DataForgeStringLookup[] Array_StringValues { get; set; }
        internal DataForgeLocale[] Array_LocaleValues { get; set; }
        internal DataForgeEnum[] Array_EnumValues { get; set; }
        internal DataForgeInt8[] Array_Int8Values { get; set; }
        internal DataForgeInt16[] Array_Int16Values { get; set; }
        internal DataForgeInt32[] Array_Int32Values { get; set; }
        internal DataForgeInt64[] Array_Int64Values { get; set; }
        internal DataForgeUInt8[] Array_UInt8Values { get; set; }
        internal DataForgeUInt16[] Array_UInt16Values { get; set; }
        internal DataForgeUInt32[] Array_UInt32Values { get; set; }
        internal DataForgeUInt64[] Array_UInt64Values { get; set; }
        internal DataForgeBoolean[] Array_BooleanValues { get; set; }
        internal DataForgeSingle[] Array_SingleValues { get; set; }
        internal DataForgeDouble[] Array_DoubleValues { get; set; }
        internal DataForgePointer[] Array_StrongValues { get; set; }
        internal DataForgePointer[] Array_WeakValues { get; set; }

        internal Dictionary<uint, string> ValueMap { get; set; }
        internal Dictionary<uint, List<XmlElement>> DataMap { get; set; }
        internal List<ClassMapping> Require_ClassMapping { get; set; }
        internal List<ClassMapping> Require_StrongMapping { get; set; }
        internal List<ClassMapping> Require_WeakMapping1 { get; set; }
        internal List<ClassMapping> Require_WeakMapping2 { get; set; }
        internal List<XmlElement> DataTable { get; set; }

        internal U[] ReadArray<U>(int arraySize) where U : DataForgeSerializable
        {
            if (arraySize is -1) return null; 
            return (from i in Enumerable.Range(0, arraySize) let data = (U)Activator.CreateInstance(typeof(U), this) select data).ToArray();
        }

		public DataForge(FileInfo inFile)
		{
            br = new(inFile.Open(FileMode.Open, FileAccess.Read, FileShare.None));
            br.ReadInt32(); // Offset
            FileVersion = br.ReadInt32();

            Require_ClassMapping = new();
			Require_StrongMapping = new();
			Require_WeakMapping1 = new();
			Require_WeakMapping2 = new();

            for (int i = 0; i < 4; i++) br.ReadUInt16(); // Offset

            int structDefinitionCount = br.ReadInt32();
            int propertyDefinitionCount = br.ReadInt32();
            int enumDefinitionCount = br.ReadInt32();
            int dataMappingCount = br.ReadInt32();
            int recordDefinitionCount = br.ReadInt32();

            int booleanValueCount = br.ReadInt32();
            int int8ValueCount = br.ReadInt32();
            int int16ValueCount = br.ReadInt32();
            int int32ValueCount = br.ReadInt32();
            int int64ValueCount = br.ReadInt32();
            int uint8ValueCount = br.ReadInt32();
            int uint16ValueCount = br.ReadInt32();
            int uint32ValueCount = br.ReadInt32();
            int uint64ValueCount = br.ReadInt32();

            int singleValueCount = br.ReadInt32();
            int doubleValueCount = br.ReadInt32();
            int guidValueCount = br.ReadInt32();
            int stringValueCount = br.ReadInt32();
            int localeValueCount = br.ReadInt32();
            int enumValueCount = br.ReadInt32();
            int strongValueCount = br.ReadInt32();
            int weakValueCount = br.ReadInt32();

            int referenceValueCount = br.ReadInt32();
            int enumOptionCount = br.ReadInt32();
            uint textLength = br.ReadUInt32();
            br.ReadUInt32(); // Offset

            StructDefinitionTable = ReadArray<DataForgeStructDefinition>(structDefinitionCount);
			PropertyDefinitionTable = ReadArray<DataForgePropertyDefinition>(propertyDefinitionCount);
			EnumDefinitionTable = ReadArray<DataForgeEnumDefinition>(enumDefinitionCount);
			DataMappingTable = ReadArray<DataForgeDataMapping>(dataMappingCount);
			RecordDefinitionTable = ReadArray<DataForgeRecord>(recordDefinitionCount);

            Array_Int8Values = ReadArray<DataForgeInt8>(int8ValueCount);
            Array_Int16Values = ReadArray<DataForgeInt16>(int16ValueCount);
            Array_Int32Values = ReadArray<DataForgeInt32>(int32ValueCount);
            Array_Int64Values = ReadArray<DataForgeInt64>(int64ValueCount);
            Array_UInt8Values = ReadArray<DataForgeUInt8>(uint8ValueCount);
            Array_UInt16Values = ReadArray<DataForgeUInt16>(uint16ValueCount);
            Array_UInt32Values = ReadArray<DataForgeUInt32>(uint32ValueCount);
            Array_UInt64Values = ReadArray<DataForgeUInt64>(uint64ValueCount);
            Array_BooleanValues = ReadArray<DataForgeBoolean>(booleanValueCount);
            Array_SingleValues = ReadArray<DataForgeSingle>(singleValueCount);
            Array_DoubleValues = ReadArray<DataForgeDouble>(doubleValueCount);
            Array_GuidValues = ReadArray<DataForgeGuid>(guidValueCount);
            Array_StringValues = ReadArray<DataForgeStringLookup>(stringValueCount);
            Array_LocaleValues = ReadArray<DataForgeLocale>(localeValueCount);
            Array_EnumValues = ReadArray<DataForgeEnum>(enumValueCount);
            Array_StrongValues = ReadArray<DataForgePointer>(strongValueCount);
            Array_WeakValues = ReadArray<DataForgePointer>(weakValueCount);

            Array_ReferenceValues = ReadArray<DataForgeReference>(referenceValueCount);
            EnumOptionTable = ReadArray<DataForgeStringLookup>(enumOptionCount);

            List<DataForgeString> buffer = new();
            long maxPosition = br.BaseStream.Position + textLength;
            long startPosition = br.BaseStream.Position;
            ValueMap = new();
            while (br.BaseStream.Position < maxPosition)
            {
                long offset = br.BaseStream.Position - startPosition;
                DataForgeString dfString = new(this);
                buffer.Add(dfString);
                ValueMap[(uint)offset] = dfString.Value;
            }
            ValueTable = buffer.ToArray();
            DataTable = new();
            DataMap = new();

            foreach (DataForgeDataMapping dataMapping in DataMappingTable)
            {
                DataMap[dataMapping.StructIndex] = new();
                DataForgeStructDefinition dataStruct = StructDefinitionTable[dataMapping.StructIndex];
                for (int i = 0; i < dataMapping.StructCount; i++)
                {
                    XmlElement node = dataStruct.Read(dataMapping.Name);
                    DataMap[dataMapping.StructIndex].Add(node);
                    DataTable.Add(node);
                }
            }

            foreach (ClassMapping dataMapping in Require_ClassMapping)
            {
                if (dataMapping.StructIndex is 0xFFFF) dataMapping.Node.ParentNode.RemoveChild(dataMapping.Node);
                else if (DataMap.ContainsKey(dataMapping.StructIndex) && DataMap[dataMapping.StructIndex].Count > dataMapping.RecordIndex) 
                    dataMapping.Node.ParentNode.ReplaceChild(DataMap[dataMapping.StructIndex][dataMapping.RecordIndex], dataMapping.Node);
                else
                {
                    XmlElement bugged = _xmlDocument.CreateElement("bugged");
                    XmlAttribute __class = _xmlDocument.CreateAttribute("__class");
                    XmlAttribute __index = _xmlDocument.CreateAttribute("__index");
                    __class.Value = $"{dataMapping.StructIndex:X8}";
                    __index.Value = $"{dataMapping.RecordIndex:X8}";
                    bugged.Attributes.Append(__class);
                    bugged.Attributes.Append(__index);
                    dataMapping.Node.ParentNode.ReplaceChild(bugged, dataMapping.Node);
                }
            }
        }

        private XmlDocument _xmlDocument = new();
		internal XmlElement CreateElement(string name) { return _xmlDocument.CreateElement(name); }
        internal XmlAttribute CreateAttribute(string name) { return _xmlDocument.CreateAttribute(name); }

        public string OuterXML
		{
			get
			{
				Compile();
				return _xmlDocument.OuterXml;
			}
		}

		internal void Compile()
		{
            if (string.IsNullOrWhiteSpace(_xmlDocument?.InnerXml))
            {
                XmlElement root = _xmlDocument.CreateElement("DataForge");
                _xmlDocument.AppendChild(root);

                foreach (ClassMapping dataMapping in Require_StrongMapping)
                {
                    DataForgePointer strong = Array_StrongValues[dataMapping.RecordIndex];
                    if (strong.Index is 0xFFFFFFFF) dataMapping.Node.ParentNode.RemoveChild(dataMapping.Node);
                    else dataMapping.Node.ParentNode.ReplaceChild(DataMap[strong.StructType][(int)strong.Index], dataMapping.Node);
                }

                foreach (ClassMapping dataMapping in Require_WeakMapping1)
                {
                    DataForgePointer weak = Array_WeakValues[dataMapping.RecordIndex];
                    XmlNode weakAttribute = dataMapping.Node;
                    if (weak.Index is 0xFFFFFFFF) weakAttribute.Value = string.Format("0");
                    else
                    {
                        XmlElement targetElement = DataMap[weak.StructType][(int)weak.Index];
                        weakAttribute.Value = targetElement.GetPath();
                    }
                }

                foreach (ClassMapping dataMapping in Require_WeakMapping2)
                {
                    XmlNode weakAttribute = dataMapping.Node;
                    if (dataMapping.StructIndex is 0xFFFF) weakAttribute.Value = "null";
                    else if (dataMapping.RecordIndex is -1)
                    {
                        List<XmlElement> targetElement = DataMap[dataMapping.StructIndex];
                        weakAttribute.Value = targetElement.FirstOrDefault()?.GetPath();
                    }
                    else
                    {
                        XmlElement targetElement = DataMap[dataMapping.StructIndex][dataMapping.RecordIndex];
                        weakAttribute.Value = targetElement.GetPath();
                    }
                }

                foreach (DataForgeRecord record in RecordDefinitionTable)
                {
                    /*
                     * TODO: Write this to Debug Log File
				if (!record.FileName.ToLowerInvariant().Contains(record.Name.ToLowerInvariant()) &&
					!record.FileName.ToLowerInvariant().Contains(record.Name.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Last().ToLowerInvariant()))
				        Console.WriteLine("Warning {0} doesn't match {1}", record.Name, record.FileName);
                    */
                    if (record.Hash.HasValue && record.Hash != Guid.Empty)
                    {
                        XmlAttribute hash = CreateAttribute("__ref");
                        hash.Value = $"{record.Hash}";
                        DataMap[record.StructIndex][record.VariantIndex].Attributes.Append(hash);
                    }
                    if (!string.IsNullOrWhiteSpace(record.FileName))
                    {
                        XmlAttribute path = CreateAttribute("__path");
                        path.Value = $"{record.FileName}";
                        DataMap[record.StructIndex][record.VariantIndex].Attributes.Append(path);
                    }
                    DataMap[record.StructIndex][record.VariantIndex] = DataMap[record.StructIndex][record.VariantIndex].Rename(record.Name);
                    root.AppendChild(DataMap[record.StructIndex][record.VariantIndex]);
                }
            }
		}

        public void Save(FileInfo outFile)
        {
            Compile();
            foreach (DataForgeRecord record in RecordDefinitionTable)
            {
                XmlDocument doc = new();
                doc.LoadXml(DataMap[record.StructIndex][record.VariantIndex].OuterXml);
                doc.Save(outFile.Open(FileMode.Create, FileAccess.Write, FileShare.None));
            }
            if (_xmlDocument is not null) _xmlDocument.Save(outFile.Open(FileMode.Create, FileAccess.Write, FileShare.None));
        }

        /*
         * TODO: Not sure what this is, it is unused.

		public Stream GetStream()
		{
			Compile();
			MemoryStream outStream = new();
			_xmlDocument.Save(outStream);
			return outStream;
		}

		public void GenerateSerializationClasses(string path = "AutoGen", string assemblyName = "HoloXPLOR.Data.DataForge")
        {
            path = new DirectoryInfo(path).FullName;
            if (Directory.Exists(path) && path != new DirectoryInfo(".").FullName)
            {
                Directory.Delete(path, true);
                while (Directory.Exists(path)) Thread.Sleep(100);
            }
            Directory.CreateDirectory(path);
            while (!Directory.Exists(path)) Thread.Sleep(100);
            StringBuilder sb = new();
            sb.AppendLine(@"using System.Xml.Serialization;");
            sb.AppendLine();
            sb.AppendFormat(@"namespace {0}", assemblyName);
            sb.AppendLine();
            sb.AppendLine(@"{");
            foreach (DataForgeEnumDefinition enumDefinition in EnumDefinitionTable) sb.Append(enumDefinition.Export());
            sb.AppendLine(@"}");
            File.WriteAllText(Path.Combine(path, "Enums.cs"), sb.ToString());
            sb = new();
            sb.AppendLine(@"using System;");
            sb.AppendLine(@"using System.Xml.Serialization;");
            sb.AppendLine();
            sb.AppendFormat(@"namespace {0}", assemblyName);
            sb.AppendLine();
            sb.AppendLine(@"{");
            foreach (EDataType typeDefinition in Enum.GetValues(typeof(EDataType)))
            {
                string typeName = typeDefinition.ToString().Replace("var", "");
                switch (typeDefinition)
                {
                    case EDataType.varStrongPointer:
                    case EDataType.varClass: break;
                    case EDataType.varLocale:
                    case EDataType.varWeakPointer:
                        sb.AppendFormat(@"    public class _{0}", typeName);
                        sb.AppendLine();
                        sb.AppendLine(@"    {");
                        sb.AppendLine(@"        public string Value { get; set; }");
                        sb.AppendLine(@"    }");
                        break;
                    case EDataType.varReference:
                        sb.AppendFormat(@"    public class _{0}", typeName);
                        sb.AppendLine();
                        sb.AppendLine(@"    {");
                        sb.AppendLine(@"        public Guid Value { get; set; }");
                        sb.AppendLine(@"    }");
                        break;
                    default:
                        sb.AppendFormat(@"    public class _{0}", typeName);
                        sb.AppendLine();
                        sb.AppendLine(@"    {");
                        sb.AppendFormat(@"        public {0} Value {{ get; set; }}", typeName);
                        sb.AppendLine();
                        sb.AppendLine(@"    }");
                        break;
                }
            }
            sb.AppendLine(@"}");
            File.WriteAllText(Path.Combine(path, "Arrays.cs"), sb.ToString());
            foreach (DataForgeStructDefinition structDefinition in StructDefinitionTable)
            {
                string code = structDefinition.Export(assemblyName);
                File.WriteAllText(Path.Combine(path, string.Format("{0}.cs", structDefinition.Name)), code);
            }
        }

        public IEnumerator GetEnumerator()
		{
            int i = 0;
            Compile();
			foreach (DataForgeRecord record in RecordDefinitionTable)
			{
				string fileReference = record.FileName;
				if (fileReference.Split('/').Length is 2) fileReference = fileReference.Split('/')[1];
				if (string.IsNullOrWhiteSpace(fileReference)) fileReference = string.Format(@"Dump\{0}_{1}.xml", record.Name, i++);
				string newPath = fileReference;
				if (!Directory.Exists(Path.GetDirectoryName(newPath))) Directory.CreateDirectory(Path.GetDirectoryName(newPath));
				XmlDocument doc = new();
				doc.LoadXml(DataMap[record.StructIndex][record.VariantIndex].OuterXml);
				yield return (FileName: newPath, XmlDocument: doc);
			}
		}

		public int Length => RecordDefinitionTable.Length;
        */
    }
}
