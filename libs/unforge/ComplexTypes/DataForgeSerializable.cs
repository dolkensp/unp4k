using System.IO;

namespace unforge
{
    public abstract class DataForgeSerializable
    {
        internal DataForge DocumentRoot { get; private set; }
        internal BinaryReader br;
        
        public DataForgeSerializable(DataForge documentRoot)
        {
            DocumentRoot = documentRoot;
            br = documentRoot.br;
        }
    }
}
