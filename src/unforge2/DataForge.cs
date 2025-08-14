#define NONULL

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;

namespace unforge
{
	public class ClassMapping
    {
        public XmlNode Node { get; set; }
        public UInt16 StructIndex { get; set; }
        public Int32 RecordIndex { get; set; }
    }

    public class DataForge : IEnumerable
	{
        internal BinaryReader _br;

        internal Boolean IsLegacy { get; set; }
        internal Int32 FileVersion { get; set; }

        internal DataForgeStructDefinition[] StructDefinitionTable { get; set; }
        internal DataForgePropertyDefinition[] PropertyDefinitionTable { get; set; }
        internal DataForgeEnumDefinition[] EnumDefinitionTable { get; set; }
        internal BinaryDataMappingArray DataMappingTable { get; set; }
        internal DataForgeRecord[] RecordDefinitionTable { get; set; }
        internal DataForgeStringLookup[] EnumOptionTable { get; set; }

        internal BinaryArray<DataForgeReference> Array_ReferenceValues { get; set; }
        internal BinaryArray<DataForgeGuid> Array_GuidValues { get; set; }
        internal BinaryArray<DataForgeStringLookup> Array_StringValues { get; set; }
        internal BinaryArray<DataForgeLocale> Array_LocaleValues { get; set; }
        internal BinaryArray<DataForgeEnum> Array_EnumValues { get; set; }
        internal BinaryArray<DataForgeInt8> Array_Int8Values { get; set; }
        internal BinaryArray<DataForgeInt16> Array_Int16Values { get; set; }
        internal BinaryArray<DataForgeInt32> Array_Int32Values { get; set; }
        internal BinaryArray<DataForgeInt64> Array_Int64Values { get; set; }
        internal BinaryArray<DataForgeUInt8> Array_UInt8Values { get; set; }
        internal BinaryArray<DataForgeUInt16> Array_UInt16Values { get; set; }
        internal BinaryArray<DataForgeUInt32> Array_UInt32Values { get; set; }
        internal BinaryArray<DataForgeUInt64> Array_UInt64Values { get; set; }
        internal BinaryArray<DataForgeBoolean> Array_BooleanValues { get; set; }
        internal BinaryArray<DataForgeSingle> Array_SingleValues { get; set; }
        internal BinaryArray<DataForgeDouble> Array_DoubleValues { get; set; }
        internal BinaryArray<DataForgePointer> Array_StrongValues { get; set; }
        internal BinaryArray<DataForgePointer> Array_WeakValues { get; set; }

        internal BinaryStringMap TextMap { get; set; }
        internal BinaryStringMap BlobMap { get; set; }

        internal Dictionary<UInt32, BinaryStructArray> DataMap { get; set; }
        internal List<ClassMapping> Require_ClassMapping { get; set; }
        internal List<ClassMapping> Require_StrongMapping { get; set; }
        internal List<ClassMapping> Require_WeakMapping1 { get; set; }
        internal List<ClassMapping> Require_WeakMapping2 { get; set; }

        internal U[] ReadArray<U>(Int32 arraySize) where U : _DataForgeSerializable
        {
            if (arraySize == -1)
            {
                return null;
            }

            return (from i in Enumerable.Range(0, arraySize)
                    let data = (U)Activator.CreateInstance(typeof(U), this)
                    // let hack = data._index = i
                    select data).ToArray();
        }

		public DataForge(BinaryReader br, Boolean legacy = false)
		{
			this._br = br;
			var temp00 = this._br.ReadInt32();
			this.FileVersion = this._br.ReadInt32();
			this.IsLegacy = legacy;

			this.Require_ClassMapping = new List<ClassMapping> { };
			this.Require_StrongMapping = new List<ClassMapping> { };
			this.Require_WeakMapping1 = new List<ClassMapping> { };
			this.Require_WeakMapping2 = new List<ClassMapping> { };

			if (!this.IsLegacy)
			{
				var atemp1 = this._br.ReadUInt16();
				var atemp2 = this._br.ReadUInt16();
				var atemp3 = this._br.ReadUInt16();
				var atemp4 = this._br.ReadUInt16();
			}

			var structDefinitionCount = this._br.ReadInt32();
			var propertyDefinitionCount = this._br.ReadInt32();
			var enumDefinitionCount = this._br.ReadInt32();
			var dataMappingCount = this._br.ReadInt32();
			var recordDefinitionCount = this._br.ReadInt32();

			var booleanValueCount = this._br.ReadInt32();
			var int8ValueCount = this._br.ReadInt32();
			var int16ValueCount = this._br.ReadInt32();
			var int32ValueCount = this._br.ReadInt32();
			var int64ValueCount = this._br.ReadInt32();
			var uint8ValueCount = this._br.ReadInt32();
			var uint16ValueCount = this._br.ReadInt32();
			var uint32ValueCount = this._br.ReadInt32();
			var uint64ValueCount = this._br.ReadInt32();

			var singleValueCount = this._br.ReadInt32();
			var doubleValueCount = this._br.ReadInt32();
			var guidValueCount = this._br.ReadInt32();
			var stringValueCount = this._br.ReadInt32();
			var localeValueCount = this._br.ReadInt32();
			var enumValueCount = this._br.ReadInt32();
			var strongValueCount = this._br.ReadInt32();
			var weakValueCount = this._br.ReadInt32();

			var referenceValueCount = this._br.ReadInt32();
			var enumOptionCount = this._br.ReadInt32();
			var textLength = this._br.ReadUInt32();
			var blobLength = (this.IsLegacy) ? 0 : this._br.ReadUInt32();

			this.StructDefinitionTable = this.ReadArray<DataForgeStructDefinition>(structDefinitionCount);
			this.PropertyDefinitionTable = this.ReadArray<DataForgePropertyDefinition>(propertyDefinitionCount);
			this.EnumDefinitionTable = this.ReadArray<DataForgeEnumDefinition>(enumDefinitionCount);
                        this.DataMappingTable = new BinaryDataMappingArray(this, dataMappingCount);
			this.RecordDefinitionTable = this.ReadArray<DataForgeRecord>(recordDefinitionCount);

            this.Array_Int8Values = new BinaryArray<DataForgeInt8>(this, int8ValueCount);
            this.Array_Int16Values = new BinaryArray<DataForgeInt16>(this, int16ValueCount);
            this.Array_Int32Values = new BinaryArray<DataForgeInt32>(this, int32ValueCount);
            this.Array_Int64Values = new BinaryArray<DataForgeInt64>(this, int64ValueCount);
            this.Array_UInt8Values = new BinaryArray<DataForgeUInt8>(this, uint8ValueCount);
            this.Array_UInt16Values = new BinaryArray<DataForgeUInt16>(this, uint16ValueCount);
            this.Array_UInt32Values = new BinaryArray<DataForgeUInt32>(this, uint32ValueCount);
            this.Array_UInt64Values = new BinaryArray<DataForgeUInt64>(this, uint64ValueCount);
            this.Array_BooleanValues = new BinaryArray<DataForgeBoolean>(this, booleanValueCount);
            this.Array_SingleValues = new BinaryArray<DataForgeSingle>(this, singleValueCount);
            this.Array_DoubleValues = new BinaryArray<DataForgeDouble>(this, doubleValueCount);
            this.Array_GuidValues = new BinaryArray<DataForgeGuid>(this, guidValueCount);
            this.Array_StringValues = new BinaryArray<DataForgeStringLookup>(this, stringValueCount);
            this.Array_LocaleValues = new BinaryArray<DataForgeLocale>(this, localeValueCount);
            this.Array_EnumValues = new BinaryArray<DataForgeEnum>(this, enumValueCount);
            this.Array_StrongValues = new BinaryArray<DataForgePointer>(this, strongValueCount);
            this.Array_WeakValues = new BinaryArray<DataForgePointer>(this, weakValueCount);

            this.Array_ReferenceValues = new BinaryArray<DataForgeReference>(this, referenceValueCount);
            this.EnumOptionTable = this.ReadArray<DataForgeStringLookup>(enumOptionCount);

            this.TextMap = new BinaryStringMap(this._br, textLength);
            this.BlobMap = new BinaryStringMap(this._br, blobLength);
            if (blobLength == 0) this.BlobMap = this.TextMap;

            this.DataMap = new Dictionary<UInt32, BinaryStructArray> { };

            for (int i = 0; i < this.DataMappingTable.Count; i++)
            {
                var dataMapping = this.DataMappingTable[i];
                var dataStruct = this.StructDefinitionTable[dataMapping.StructIndex];
                this.DataMap[dataMapping.StructIndex] = new BinaryStructArray(this, dataStruct, dataMapping.Name, (Int32)dataMapping.StructCount);
            }
        }

        private XmlDocument _xmlDocument = new XmlDocument();

		internal XmlElement CreateElement(String name) { return this._xmlDocument.CreateElement(name); }
        internal XmlAttribute CreateAttribute(String name) { return this._xmlDocument.CreateAttribute(name); }

        public String OuterXML
		{
			get
			{
				if (String.IsNullOrWhiteSpace(this._xmlDocument?.InnerXml)) this.Compile();
				return this._xmlDocument.OuterXml;
			}
		}

        public void Save(String filename)
        {
			if (String.IsNullOrWhiteSpace(this._xmlDocument?.InnerXml)) this.Compile();
			
			var i = 0;
			foreach (var record in this.RecordDefinitionTable)
			{
				var fileReference = record.FileName;

				if (fileReference.Split('/').Length == 2) fileReference = fileReference.Split('/')[1];

				if (String.IsNullOrWhiteSpace(fileReference)) fileReference = String.Format(@"Dump\{0}_{1}.xml", record.Name, i++);

				var newPath = Path.Combine(Path.GetDirectoryName(filename), fileReference);

				if (!Directory.Exists(Path.GetDirectoryName(newPath))) Directory.CreateDirectory(Path.GetDirectoryName(newPath));

				XmlDocument doc = new XmlDocument { };
				doc.LoadXml(this.DataMap[record.StructIndex][record.VariantIndex].OuterXml);
				doc.Save(newPath);
			}

			this._xmlDocument.Save(filename);
        }

		internal void Compile()
		{
                        var root = this._xmlDocument.CreateElement("DataForge");
                        this._xmlDocument.AppendChild(root);

                        var i = 0;
                        foreach (var record in this.RecordDefinitionTable)
                        {
                                var fileReference = record.FileName;

                                if (fileReference.Split('/').Length == 2)
                                {
                                        fileReference = fileReference.Split('/')[1];
                                }

                                if (!record.FileName.ToLowerInvariant().Contains(record.Name.ToLowerInvariant()) &&
                                        !record.FileName.ToLowerInvariant().Contains(record.Name.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Last().ToLowerInvariant()))
                                {
                                        Console.WriteLine("Warning {0} doesn't match {1}", record.Name, record.FileName);
                                }

                                if (String.IsNullOrWhiteSpace(fileReference))
                                {
                                        fileReference = String.Format(@"Dump\{0}_{1}.xml", record.Name, i++);
                                }

                                if (record.Hash.HasValue && record.Hash != Guid.Empty)
                                {
                                        var hash = this.CreateAttribute("__ref");
                                        hash.Value = $"{record.Hash}";
                                        this.DataMap[record.StructIndex][record.VariantIndex].Attributes.Append(hash);
                                }

                                if (!String.IsNullOrWhiteSpace(record.FileName))
                                {
                                        var path = this.CreateAttribute("__path");
                                        path.Value = $"{record.FileName}";
                                        this.DataMap[record.StructIndex][record.VariantIndex].Attributes.Append(path);
                                }

                                this.DataMap[record.StructIndex][record.VariantIndex] = this.DataMap[record.StructIndex][record.VariantIndex].Rename(record.Name);
                                root.AppendChild(this.DataMap[record.StructIndex][record.VariantIndex]);
                        }

                        foreach (var dataMapping in this.Require_ClassMapping)
                        {
                                if (dataMapping.StructIndex == 0xFFFF)
                                {
#if NONULL
                                        dataMapping.Node.ParentNode.RemoveChild(dataMapping.Node);
#else
                    dataMapping.Item1.ParentNode.ReplaceChild(
                        this._xmlDocument.CreateElement("null"),
                        dataMapping.Item1);
#endif
                                }
                                else if (this.DataMap.ContainsKey(dataMapping.StructIndex) && this.DataMap[dataMapping.StructIndex].Count > dataMapping.RecordIndex)
                                {
                                        dataMapping.Node.ParentNode.ReplaceChild(
                                                (XmlElement)this.DataMap[dataMapping.StructIndex][dataMapping.RecordIndex].CloneNode(true),
                                                dataMapping.Node);
                                }
                                else
                                {
                                        var bugged = this._xmlDocument.CreateElement("bugged");
                                        var __class = this._xmlDocument.CreateAttribute("__class");
                                        var __index = this._xmlDocument.CreateAttribute("__index");
                                        __class.Value = $"{dataMapping.StructIndex:X8}";
                                        __index.Value = $"{dataMapping.RecordIndex:X8}";
                                        bugged.Attributes.Append(__class);
                                        bugged.Attributes.Append(__index);
                                        dataMapping.Node.ParentNode.ReplaceChild(
                                                bugged,
                                                dataMapping.Node);
                                }
                        }

                        foreach (var dataMapping in this.Require_StrongMapping)
                        {
                                var strong = this.Array_StrongValues[dataMapping.RecordIndex];

                                if (strong.Index == 0xFFFFFFFF)
                                {
#if NONULL
                                        dataMapping.Node.ParentNode.RemoveChild(dataMapping.Node);
#else
                    dataMapping.Item1.ParentNode.ReplaceChild(
                        this._xmlDocument.CreateElement("null"),
                        dataMapping.Item1);
#endif
                                }
                                else
                                {
                                        dataMapping.Node.ParentNode.ReplaceChild(
                                                (XmlElement)this.DataMap[strong.StructType][(Int32)strong.Index].CloneNode(true),
                                                dataMapping.Node);
                                }
                        }

                        foreach (var dataMapping in this.Require_WeakMapping1)
                        {
                                var weak = this.Array_WeakValues[dataMapping.RecordIndex];

                                var weakAttribute = dataMapping.Node;

                                if (weak.Index == 0xFFFFFFFF)
                                {
                                        weakAttribute.Value = String.Format("0");
                                }
                                else
                                {
                                        var targetElement = this.DataMap[weak.StructType][(Int32)weak.Index];

                                        weakAttribute.Value = targetElement.GetPath();
                                }
                        }

                        foreach (var dataMapping in this.Require_WeakMapping2)
                        {
                                var weakAttribute = dataMapping.Node;

                                if (dataMapping.StructIndex == 0xFFFF)
                                {
                                        weakAttribute.Value = "null";
                                }
                                else if (dataMapping.RecordIndex == -1)
                                {
                                        var targetElement = this.DataMap[dataMapping.StructIndex];

                                        weakAttribute.Value = targetElement.FirstOrDefault()?.GetPath();
                                }
                                else
                                {
                                        var targetElement = this.DataMap[dataMapping.StructIndex][dataMapping.RecordIndex];

                                        weakAttribute.Value = targetElement.GetPath();
                                }
                        }
		}

		public Stream GetStream()
		{
			if (String.IsNullOrWhiteSpace(this._xmlDocument?.InnerXml)) this.Compile();

			var outStream = new MemoryStream();

			this._xmlDocument.Save(outStream);

			return outStream;
		}

		public void GenerateSerializationClasses(String path = "AutoGen", String assemblyName = "HoloXPLOR.Data.DataForge")
        {
            path = new DirectoryInfo(path).FullName;

            if (Directory.Exists(path) && path != new DirectoryInfo(".").FullName)
            {
                Directory.Delete(path, true);
                while (Directory.Exists(path))
                {
                    Thread.Sleep(100);
                }
            }

            Directory.CreateDirectory(path);
            while (!Directory.Exists(path))
            {
                Thread.Sleep(100);
            }

            var sb = new StringBuilder();

            sb.AppendLine(@"using System.Xml.Serialization;");
            sb.AppendLine();
            sb.AppendFormat(@"namespace {0}", assemblyName);
            sb.AppendLine();
            sb.AppendLine(@"{");
            foreach (var enumDefinition in this.EnumDefinitionTable)
            {
                sb.Append(enumDefinition.Export());
            }
            sb.AppendLine(@"}");

            File.WriteAllText(Path.Combine(path, "Enums.cs"), sb.ToString());

            sb = new StringBuilder();

            sb.AppendLine(@"using System;");
            sb.AppendLine(@"using System.Xml.Serialization;");
            sb.AppendLine();
            sb.AppendFormat(@"namespace {0}", assemblyName);
            sb.AppendLine();
            sb.AppendLine(@"{");
            foreach (EDataType typeDefinition in Enum.GetValues(typeof(EDataType)))
            {
                var typeName = typeDefinition.ToString().Replace("var", "");
                switch (typeDefinition)
                {
                    case EDataType.varStrongPointer:
                    case EDataType.varClass: break;
                    case EDataType.varLocale:
                    case EDataType.varWeakPointer:
                        sb.AppendFormat(@"    public class _{0}", typeName);
                        sb.AppendLine();
                        sb.AppendLine(@"    {");
                        sb.AppendLine(@"        public String Value { get; set; }");
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

            foreach (var structDefinition in this.StructDefinitionTable)
            {
                var code = structDefinition.Export(assemblyName);
                File.WriteAllText(Path.Combine(path, String.Format("{0}.cs", structDefinition.Name)), code);
            }
        }

		public IEnumerator GetEnumerator()
		{
			if (String.IsNullOrWhiteSpace(this._xmlDocument?.InnerXml)) this.Compile();

			var i = 0;

			foreach (var record in this.RecordDefinitionTable)
			{
				var fileReference = record.FileName;

				if (fileReference.Split('/').Length == 2) fileReference = fileReference.Split('/')[1];

				if (String.IsNullOrWhiteSpace(fileReference)) fileReference = String.Format(@"Dump\{0}_{1}.xml", record.Name, i++);

				var newPath = fileReference;

				if (!Directory.Exists(Path.GetDirectoryName(newPath))) Directory.CreateDirectory(Path.GetDirectoryName(newPath));

				XmlDocument doc = new XmlDocument { };
				doc.LoadXml(this.DataMap[record.StructIndex][record.VariantIndex].OuterXml);

				yield return (FileName: newPath, XmlDocument: doc);
			}
		}

		public Int32 Length => this.RecordDefinitionTable.Length;

#if NET20 || NET35 || NET40 || NET45 || NET451 || NET452 || NET46 || NET461 || NET462 || NET47 || NET471 || NET472
		public void CompileSerializationAssembly(String assemblyName = "HoloXPLOR.Data.DataForge")
        {
            CSharpCodeProvider provider = new CSharpCodeProvider();
            CompilerParameters parameters = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = false,
                OutputAssembly = String.Format("{0}.dll", assemblyName),
            };

            parameters.ReferencedAssemblies.Add("System.dll");
            parameters.ReferencedAssemblies.Add("System.Xml.dll");

            List<String> source = new List<String> { };

            var sb = new StringBuilder();

            sb.AppendLine(@"using System.Xml.Serialization;");
            sb.AppendLine();
            sb.AppendFormat(@"namespace {0}", assemblyName);
            sb.AppendLine();
            sb.AppendLine(@"{");
            foreach (var enumDefinition in this.EnumDefinitionTable)
            {
                sb.Append(enumDefinition.Export());
            }
            sb.AppendLine(@"}");

            source.Add(sb.ToString());

            sb = new StringBuilder();

            sb.AppendLine(@"using System;");
            sb.AppendLine(@"using System.Xml.Serialization;");
            sb.AppendLine();
            sb.AppendFormat(@"namespace {0}", assemblyName);
            sb.AppendLine();
            sb.AppendLine(@"{");
            foreach (EDataType typeDefinition in Enum.GetValues(typeof(EDataType)))
            {
                var typeName = typeDefinition.ToString().Replace("var", "");
                switch (typeDefinition)
                {
                    case EDataType.varStrongPointer:
                    case EDataType.varClass: break;
                    case EDataType.varByte:
                        typeName = "UInt8";
                        sb.AppendFormat(@"    public class _{0}", typeName);
                        sb.AppendLine();
                        sb.AppendLine(@"    {");
                        sb.AppendLine(@"        public Byte Value { get; set; }");
                        sb.AppendLine(@"    }");
                        break;
                    case EDataType.varSByte:
                        typeName = "Int8";
                        sb.AppendFormat(@"    public class _{0}", typeName);
                        sb.AppendLine();
                        sb.AppendLine(@"    {");
                        sb.AppendLine(@"        public SByte Value { get; set; }");
                        sb.AppendLine(@"    }");
                        break;
                    case EDataType.varLocale:
                    case EDataType.varWeakPointer:
                        sb.AppendFormat(@"    public class _{0}", typeName);
                        sb.AppendLine();
                        sb.AppendLine(@"    {");
                        sb.AppendLine(@"        public String Value { get; set; }");
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

            source.Add(sb.ToString());

            foreach (var structDefinition in this.StructDefinitionTable)
            {
                var code = structDefinition.Export(assemblyName);
                source.Add(code);
            }

            var result = provider.CompileAssemblyFromSource(parameters, source.ToArray());
        }
#endif
	}
}
