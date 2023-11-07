using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace unp4k;

public class P4KFile
{
    internal ZipFile? P4K { get; private set; }
    public List<ZipEntry> Entries { get; internal set; } = new();

    /// <summary>
    /// The amount of entries within the P4K.
    /// </summary>
    public int EntryCount => Entries.Count;

    /// <summary>
    /// A P4KFile object contains all the relevant data and utilities to exploit a P4K file.
    /// </summary>
    /// <param name="p4kFile"></param>
    public P4KFile(FileInfo p4kFile)
    {
        P4K = new(p4kFile.Open(FileMode.Open, FileAccess.Read, FileShare.None))
        {
            UseZip64 = UseZip64.On
        }; // The Data.p4k must be locked while it is being read to avoid corruption.
        P4K.KeysRequired += (object sender, KeysRequiredEventArgs e) => e.Key = new byte[] { 0x5E, 0x7A, 0x20, 0x02, 0x30, 0x2E, 0xEB, 0x1A, 0x3B, 0xB6, 0x17, 0xC3, 0x0F, 0xDE, 0x1E, 0x47 };
        foreach (ZipEntry entry in P4K) Entries.Add(entry);
    }

    /// <summary>
    /// Filter the P4K's entries using LINQ Where.
    /// </summary>
    /// <param name="where"></param>
    public void FilterEntries(Func<ZipEntry, bool> where) => Entries = new(Entries.Where(where));

    /// <summary>
    /// Order the P4K's entries using LINQ OrderBy.
    /// </summary>
    /// <typeparam name="U"></typeparam>
    /// <param name="order"></param>
    public void OrderBy<U>(Func<ZipEntry, U> order) => Entries = new(Entries.OrderBy(order));

    /// <summary>
    /// Creates an enumorator which can be accessed and modified by multiple threads at once.
    /// </summary>
    /// <param name="threads"></param>
    /// <param name="merge"></param>
    /// <param name="func"></param>
    /// <returns></returns>
    public ParallelQuery<ZipEntry> GetParallelEnumerator(int threads, ParallelMergeOptions merge, Func<ZipEntry, int, ZipEntry> func) => Entries.AsParallel().AsOrdered().WithDegreeOfParallelism(threads).WithMergeOptions(merge).Select(func);

    /// <summary>
    /// Extract a file entry from the P4K.
    /// </summary>
    /// <param name="entry"></param>
    /// <param name="extractionFile"></param>
    public void Extract(ZipEntry entry, FileInfo extractionFile)
    {
        byte[] decomBuffer = new byte[4096];
        if (!extractionFile.Directory.Exists) extractionFile.Directory.Create();
        else if (extractionFile.Exists) extractionFile.Delete();
        FileStream fs = extractionFile.Open(FileMode.Create, FileAccess.Write, FileShare.ReadWrite); // Dont want people accessing incomplete files.
        Stream decompStream = P4K.GetInputStream(entry);
        StreamUtils.Copy(decompStream, fs, decomBuffer);
        decompStream.Close();
        fs.Close();
    }
}