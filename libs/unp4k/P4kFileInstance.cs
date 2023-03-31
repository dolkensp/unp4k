using ICSharpCode.SharpZipLib.Zip;

namespace unp4k;

public class P4kFileInstance
{
    internal ZipFile? P4kFile { get; private set; }
    private List<ZipEntry> Entries { get; set; } = new();
    public int EntryCount => Entries.Count;

    public P4kFileInstance(FileInfo p4kFile)
    {
        P4kFile = new(p4kFile.Open(FileMode.Open, FileAccess.Read, FileShare.None)); // The Data.p4k must be locked while it is being read to avoid corruption.
        P4kFile.UseZip64 = UseZip64.On;
        P4kFile.KeysRequired += (object sender, KeysRequiredEventArgs e) => e.Key = new byte[] { 0x5E, 0x7A, 0x20, 0x02, 0x30, 0x2E, 0xEB, 0x1A, 0x3B, 0xB6, 0x17, 0xC3, 0x0F, 0xDE, 0x1E, 0x47 };
        foreach (ZipEntry entry in P4kFile) Entries.Add(entry);
    }

    public void FilterEntries(Func<ZipEntry, bool> where) => Entries = new(Entries.Where(where));
    public void OrderBy<U>(Func<ZipEntry, U> order) => Entries = new(Entries.OrderBy(order));
    public ParallelQuery<ZipEntry> GetParallelEnumerator(int threads, ParallelMergeOptions merge, Func<ZipEntry, int, ZipEntry> func) => Entries.AsParallel().AsOrdered().WithDegreeOfParallelism(threads).WithMergeOptions(merge).Select(func);
}