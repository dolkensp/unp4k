using System.IO;
using System.Xml;
using System.Threading.Tasks;

namespace unforge
{
    public static class DataForge
    {
        public static async Task Forge(DataForgeInstancePackage pckg)
        {
            XmlWriter writer = null;
            string currentSection = null;
            foreach (DataForgeDataMapping dm in pckg.DataMappingTable)
            {
                if (writer is null || currentSection != dm.Name)
                {
                    currentSection = dm.Name;
                    writer?.Close();
                    writer?.Dispose();
                    writer = XmlWriter.Create(new FileInfo(Path.Join(pckg.OutFile.FullName[..pckg.OutFile.FullName.LastIndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar })],
                            $"{pckg.OutFile.Name.Replace(pckg.OutFile.Extension, string.Empty)}_{dm.Name}.xml")).Open(FileMode.Create, FileAccess.Write, FileShare.None), new XmlWriterSettings
                            {
                                Indent = true,
                                Async = true
                            });
                }
                await pckg.StructDefinitionTable[dm.StructIndex].Read(writer);
            }
            writer?.Close();
            writer?.Dispose();
        }
    }
}
