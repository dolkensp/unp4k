using System.IO;

namespace unforge
{
	public abstract class _DataForgeSerializable
    {
        internal DataForge DocumentRoot { get; private set; }
        internal BinaryReader _br;
        
        public _DataForgeSerializable(DataForge documentRoot)
        {
            this.DocumentRoot = documentRoot;
            this._br = documentRoot._br;
        }
    }
}
