using System.IO;

namespace unforge
{
    public abstract class DataForgeSerializable
    {
        internal DataForge DocumentRoot { get; private set; }
        internal BinaryReader _br;
        
        public DataForgeSerializable(DataForge documentRoot)
        {
            DocumentRoot = documentRoot;
            _br = documentRoot._br;
        }
    }
}
