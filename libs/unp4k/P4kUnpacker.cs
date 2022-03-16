using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

using unforge;

namespace unp4k;

public static class P4kUnpacker
{
    public static void ExtractP4kEntry(P4kFileInstance instance, ZipEntry entry, FileInfo extractionFile, FileInfo forgeFile = null)
    {
        byte[] decomBuffer = new byte[4096];
        if (!extractionFile.Directory.Exists) extractionFile.Directory.Create();
        FileStream fs = extractionFile.Open(FileMode.Create, FileAccess.Write, FileShare.ReadWrite); // Dont want people accessing incomplete files.
        Stream decompStream = instance.P4kFile.GetInputStream(entry);
        StreamUtils.Copy(decompStream, fs, decomBuffer);
        decompStream.Close();
        fs.Close();
        if (forgeFile is not null)
        {
            try { if (extractionFile.Extension is ".dcb") DataForge.Forge(extractionFile, forgeFile); else DataForge.DeserialiseCryXml(extractionFile, forgeFile); }
            catch { throw; }
        }
    }
}