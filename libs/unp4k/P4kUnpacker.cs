using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using System.Xml;
using unforge;

namespace unp4k;

public static class P4kUnpacker
{
    public static void ExtractP4kEntry(P4kFileInstance instance, ZipEntry entry, FileInfo extractionFile)
    {
        byte[] decomBuffer = new byte[4096];
        if (!extractionFile.Directory.Exists) extractionFile.Directory.Create();
        else if (extractionFile.Exists) extractionFile.Delete();
        FileStream fs = extractionFile.Open(FileMode.Create, FileAccess.Write, FileShare.ReadWrite); // Dont want people accessing incomplete files.
        Stream decompStream = instance.P4kFile.GetInputStream(entry);
        StreamUtils.Copy(decompStream, fs, decomBuffer);
        decompStream.Close();
        fs.Close();
    }

    public static void UnForgeFile(FileInfo extractionFile, FileInfo forgeFile, bool convertToJson)
    {
        try
        {
            if (extractionFile.Extension is ".dcb") DataForge.UnForge(extractionFile, forgeFile);
            else if (extractionFile.Extension is ".xml") DataForge.DeserialiseCryXml(extractionFile, forgeFile);
            else throw new FormatException(extractionFile.Name);

            if (convertToJson)
            {
                FileInfo jsonFile = new(forgeFile.FullName.Replace(".dcb", ".json").Replace(".xml", ".json"));
                if (jsonFile.Exists) jsonFile.Delete();
                StreamWriter writer = new(jsonFile.Open(FileMode.CreateNew, FileAccess.Write, FileShare.None));
                XmlDocument xmlDoc = new();
                xmlDoc.LoadXml(forgeFile.OpenText().ReadToEnd());
                writer.Write(JsonConvert.SerializeXmlNode(xmlDoc));
            }
        }
        catch
        {
            throw new InvalidOperationException(extractionFile.Name);
        }
    }
}