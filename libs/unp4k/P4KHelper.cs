using System.Xml;

using Newtonsoft.Json;

using unforge;

namespace unp4k;

public static class P4KHelper
{
    /// <summary>
    /// UnForge a file extracted from a P4K from CryXML to standard XML as well as JSON.
    /// </summary>
    /// <param name="extractionFile"></param>
    /// <param name="forgeFile"></param>
    /// <param name="convertToJson"></param>
    /// <exception cref="InvalidOperationException"></exception>
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