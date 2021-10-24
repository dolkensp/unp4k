using System.IO;

namespace unforge
{
    public abstract class DataForgeSerializable
    {
        internal DataForgeInstancePackage DocumentRoot { get; private set; }
        internal BinaryReader Br { get { return DocumentRoot.Br; } }
        
        public DataForgeSerializable(DataForgeInstancePackage documentRoot)
        {
            DocumentRoot = documentRoot;
        }
    }
}
