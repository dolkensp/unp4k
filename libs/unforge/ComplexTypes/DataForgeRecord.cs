using System;
using System.IO;

namespace unforge
{
    public class DataForgeRecord : DataForgeSerializable
    {
        public uint NameOffset { get; set; }
        public string Name { get { return DocumentRoot.ValueMap[NameOffset]; } }

        public string FileName { get { return DocumentRoot.ValueMap[FileNameOffset]; } }
        public uint FileNameOffset { get; set; }

        public string __structIndex { get { return string.Format("{0:X4}", StructIndex); } }
        public uint StructIndex { get; set; }

        public Guid? Hash { get; set; }

        public string __variantIndex { get { return string.Format("{0:X4}", VariantIndex); } }
        public ushort VariantIndex { get; set; }

        public string __otherIndex { get { return string.Format("{0:X4}", OtherIndex); } }
        public ushort OtherIndex { get; set; }

        public DataForgeRecord(DataForge documentRoot) : base(documentRoot)
        {
            NameOffset = _br.ReadUInt32();
            if (!DocumentRoot.IsLegacy) FileNameOffset = _br.ReadUInt32();
            StructIndex = _br.ReadUInt32();
            Hash = _br.ReadGuid(false);
            VariantIndex = _br.ReadUInt16();
            OtherIndex = _br.ReadUInt16();
        }

        public override string ToString() => string.Format("<{0} {1:X4} />", Name, StructIndex);
    }
}
