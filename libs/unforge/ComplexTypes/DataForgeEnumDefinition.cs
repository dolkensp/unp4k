using System.Text;

namespace unforge
{
    public class DataForgeEnumDefinition : DataForgeSerializable
    {
        public uint NameOffset { get; set; }
        public string Name { get { return DocumentRoot.ValueMap[NameOffset]; } }
        public ushort ValueCount { get; set; }
        public ushort FirstValueIndex { get; set; }

        public DataForgeEnumDefinition(DataForge documentRoot) : base(documentRoot)
        {
            NameOffset = _br.ReadUInt32();
            ValueCount = _br.ReadUInt16();
            FirstValueIndex = _br.ReadUInt16();
        }

        public override string ToString() => string.Format("<{0} />", Name);

        public string Export()
        {
            var sb = new StringBuilder();
            sb.AppendFormat(@"    public enum {0}", Name);
            sb.AppendLine();
            sb.AppendLine(@"    {");
            for (uint i = FirstValueIndex, j = (uint)(FirstValueIndex + ValueCount); i < j;  i++)
            {
                sb.AppendFormat(@"        [XmlEnum(Name = ""{0}"")]", DocumentRoot.EnumOptionTable[i].Value);
                sb.AppendLine();
                sb.AppendFormat(@"        _{0},", DocumentRoot.EnumOptionTable[i].Value);
                sb.AppendLine();
            }
            sb.AppendLine(@"    }");
            sb.AppendLine();
            sb.AppendFormat(@"    public class _{0}", Name);
            sb.AppendLine();
            sb.AppendLine(@"    {");
            sb.AppendFormat(@"        public {0} Value {{ get; set; }}", Name);
            sb.AppendLine();
            sb.AppendLine(@"    }");
            return sb.ToString();
        }
    }
}
