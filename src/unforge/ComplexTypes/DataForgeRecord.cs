using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace unforge
{
    public class DataForgeRecord : _DataForgeSerializable
    {
        public UInt32 NameOffset { get; set; }
        public String Name { get { return this.DocumentRoot.ValueMap[this.NameOffset]; } }

        public String FileName { get { return this.DocumentRoot.ValueMap[this.FileNameOffset]; } }
        public UInt32 FileNameOffset { get; set; }

        public String __structIndex { get { return String.Format("{0:X4}", this.StructIndex); } }
        public UInt32 StructIndex { get; set; }

        public Guid? Hash { get; set; }

        public String __variantIndex { get { return String.Format("{0:X4}", this.VariantIndex); } }
        public UInt16 VariantIndex { get; set; }

        public String __otherIndex { get { return String.Format("{0:X4}", this.OtherIndex); } }
        public UInt16 OtherIndex { get; set; }

        public DataForgeRecord(DataForge documentRoot)
            : base(documentRoot)
        {
            this.NameOffset = this._br.ReadUInt32();
            
            if (!this.DocumentRoot.IsLegacy)
            {
                this.FileNameOffset = this._br.ReadUInt32();
            }

            this.StructIndex = this._br.ReadUInt32();
            this.Hash = this._br.ReadGuid(false);

            this.VariantIndex = this._br.ReadUInt16();
            this.OtherIndex = this._br.ReadUInt16();
        }

        public override String ToString()
        {
            return String.Format("<{0} {1:X4} />", this.Name, this.StructIndex);
        }
    }
}
