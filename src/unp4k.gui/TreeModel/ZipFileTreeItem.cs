using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Media;
using System.Diagnostics;
using unp4k.gui.Extensions;
using System.Windows.Controls;
using System.Threading.Tasks;

namespace unp4k.gui.TreeModel
{
	public class ZipFileTreeItem : TreeItem, IBranchItem
	{
		public virtual Boolean Expanded { get; set; } = false;

		public override String RelativePath => String.Empty;

		public override DateTime LastModifiedUtc => this.AllChildren
			.OfType<IStreamTreeItem>()
			.Max(t => t.LastModifiedUtc);

		public override Int64 StreamLength => this.AllChildren
			.OfType<IStreamTreeItem>()
			.Sum(t => t.StreamLength);

		private ImageSource _icon;
		public override Object Icon =>
			this._icon = this._icon ??
			IconManager.GetCachedFileIcon(
				path: this.Title,
				iconSize: IconManager.IconSize.Large);

		public ZipFileTreeItem(ZipFile zipFile, String name = null)
			: base(zipFile.GetArchiveName(name))
		{
			var sw = new Stopwatch { };

			sw.Start();

			var maxIndex = zipFile.Count - 1;
			var lastIndex = 0L;
			var timeTaken = 0L;

			var oldProgress = ArchiveExplorer.RegisterProgress(async (ProgressBar barProgress) =>
			{
				barProgress.Maximum = maxIndex;
				barProgress.Value = lastIndex;

				await ArchiveExplorer.UpdateStatus($"Loading file {lastIndex:#,##0}/{maxIndex:#,##0} from archive");

				await Task.CompletedTask;
			});

			var entryList = new List<ZipEntry> { };

			foreach (ZipEntry entry in zipFile)
			{
				entryList.Add(entry);	
			}

			foreach (var entry in entryList)
			{
				this.AddStream(entry.Name, () => zipFile.GetInputStream(entry), entry.DateTime.ToUniversalTime(), entry.Size);

				lastIndex = entry.ZipFileIndex;
			}

			sw.Stop();

			timeTaken = sw.ElapsedMilliseconds;

			ArchiveExplorer.RegisterProgress(oldProgress);

			ArchiveExplorer.UpdateStatus($"Loaded {this.Text} in {timeTaken:#,000}ms").Wait();
		}
	}
}
