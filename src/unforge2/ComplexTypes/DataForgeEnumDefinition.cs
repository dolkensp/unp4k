using System;
using System.Text;

namespace unforge
{
	public class DataForgeEnumDefinition : _DataForgeSerializable
    {
        public UInt32 NameOffset { get; set; }
        public String Name { get { return this.DocumentRoot.BlobMap[this.NameOffset]; } }
        public UInt16 ValueCount { get; set; }
        public UInt16 FirstValueIndex { get; set; }

        public DataForgeEnumDefinition(DataForge documentRoot)
            : base(documentRoot)
        {
            this.NameOffset = this._br.ReadUInt32();
            this.ValueCount = this._br.ReadUInt16();
            this.FirstValueIndex = this._br.ReadUInt16();
        }

        public override String ToString()
        {
            return String.Format("<{0} />", this.Name);
        }

        public String Export()
        {
            var sb = new StringBuilder();
            sb.AppendFormat(@"    public enum {0}", this.Name);
            sb.AppendLine();
            sb.AppendLine(@"    {");
            for (UInt32 i = this.FirstValueIndex, j = (UInt32)(this.FirstValueIndex + this.ValueCount); i < j;  i++)
            {
                sb.AppendFormat(@"        [XmlEnum(Name = ""{0}"")]", this.DocumentRoot.EnumOptionTable[i].Value);
                sb.AppendLine();
                sb.AppendFormat(@"        _{0},", this.DocumentRoot.EnumOptionTable[i].Value);
                sb.AppendLine();
            }
            sb.AppendLine(@"    }");
            sb.AppendLine();
            sb.AppendFormat(@"    public class _{0}", this.Name);
            sb.AppendLine();
            sb.AppendLine(@"    {");
            sb.AppendFormat(@"        public {0} Value {{ get; set; }}", this.Name);
            sb.AppendLine();
            sb.AppendLine(@"    }");
            return sb.ToString();
        }
    }
}
