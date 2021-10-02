using ICSharpCode.SharpZipLib.Zip;
using Ookii.Dialogs.Wpf;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using unp4k.gui.Extensions;
using unp4k.gui.Plugins;
using unp4k.gui.TreeModel;
using Zstd.Net;
using Path = System.IO.Path;

namespace unp4k.gui
{
	public enum ExtractModeEnum
	{
		New,
		NewOrLatest,
		Overwrite,
	}

	public class TreeExtractor
	{
		private ZipFile _pak;
		public Predicate<Object> Filter { get; }

		public TreeExtractor(ZipFile pak, Predicate<Object> filter)
		{
			this._pak = pak;
			this.Filter = filter;
		}

		private Int32 _filesSelected;
		private Int32 _filesExtracted;

		public async Task<Boolean> ExtractNodeAsync(ITreeItem selectedItem, Boolean useTemp = false)
		{
			if (selectedItem == null) return false;

			Boolean? result = false;
			String path = String.Empty;

			if (useTemp)
			{
				path = Path.Combine(Path.GetTempPath(), "unp4k", selectedItem.Title);
				result = true;
			}

			var extractMode = ExtractModeEnum.NewOrLatest;

			if (String.IsNullOrWhiteSpace(path))
			{
				if (selectedItem is IStreamTreeItem)
				{
					var dlg = new VistaSaveFileDialog
					{
						FileName = selectedItem.Title,
						OverwritePrompt = true,
						Title = $"Export {selectedItem.Text} File",
						Filter = $"Selected File|{Path.GetExtension(selectedItem.Title)}",
					};

					result = dlg.ShowDialog();
					path = dlg.FileName;

					extractMode = ExtractModeEnum.Overwrite;
				}

				else if (selectedItem is IBranchItem)
				{
					var dlg = new VistaFolderBrowserDialog
					{
						Description = $"Export {selectedItem.Text} Directory",
						UseDescriptionForTitle = true,
						SelectedPath = selectedItem.Title,
					};

					result = dlg.ShowDialog();
					path = dlg.SelectedPath;

					extractMode = ExtractModeEnum.NewOrLatest;
				}
			}

			if (result == true)
			{
				this._filesSelected = await this.CountNodesAsync(selectedItem);
				this._filesExtracted = 0;

				var oldProgress = ArchiveExplorer.RegisterProgress(async (ProgressBar barProgress) =>
				{
					barProgress.Maximum = this._filesSelected + 1; // Add 1 as we increment early
					barProgress.Value = this._filesExtracted;

					await ArchiveExplorer.UpdateStatus($"Extracting file {this._filesExtracted:#,##0}/{this._filesSelected:#,##0} from archive");

					await Task.CompletedTask;
				});

				var sw = new Stopwatch();

				sw.Start();

				result &= await this.ExtractNodeAsync(selectedItem, path, extractMode);

				sw.Stop();

				await ArchiveExplorer.UpdateStatus($"Extracted {this._filesExtracted:#,##0} files in {sw.ElapsedMilliseconds:#,000}ms");

				ArchiveExplorer.RegisterProgress(oldProgress);

				if (useTemp && (File.Exists(path) || Directory.Exists(path))) System.Diagnostics.Process.Start(path);
			}

			return result ?? false;
		}

		private async Task<Int32> CountNodesAsync(ITreeItem node)
		{
			// Early exit if we don't match the filter
			if (!this.Filter(node)) return 0;

			await Task.CompletedTask;

			return node.AllChildren
				.OfType<IStreamTreeItem>()
				.Where(n => this.Filter(n))
				.Count();
		}

		private async Task<Boolean> ExtractNodeAsync(ITreeItem node, String outputRoot, ExtractModeEnum extractMode, String rootPath = null)
		{
			// Early exit if we don't match the filter
			if (!this.Filter(node)) return true;

			if (node is IStreamTreeItem leaf)
			{
				return await this.ExtractNodeAsync(leaf, outputRoot, extractMode, rootPath);
			}

			if (node is IBranchItem branch)
			{
				return await this.ExtractNodeAsync(branch, outputRoot, extractMode, rootPath);
			}

			return false;

			// else
			// {
			// 	throw new NotSupportedException($"Node type not supported. Node type: {node.GetType().Name}");
			// }
		}

		private async Task<Boolean> ExtractNodeAsync(IStreamTreeItem node, String outputRoot, ExtractModeEnum extractMode, String rootPath)
		{
			this._filesExtracted += 1;

			var forgeFactory = new DataForgeFormatFactory { };
			var cryxmlFactory = new CryXmlFormatFactory { };

			node = forgeFactory.Extract(node);
			node = cryxmlFactory.Extract(node);

			if (rootPath == null)
			{
				rootPath = Path.GetDirectoryName(node.RelativePath);
				outputRoot = Path.GetDirectoryName(outputRoot);
			}

			// Get file path relative to the passed root
			var relativePath = node.RelativePath.RelativeTo(rootPath);
			var absolutePath = Path.Combine(outputRoot, relativePath);

			if (!String.IsNullOrWhiteSpace(absolutePath))
			{
				var target = new FileInfo(absolutePath);

				if (!target.Directory.Exists) target.Directory.Create();

				if (target.Exists)
				{
					switch (extractMode)
					{
						case ExtractModeEnum.New: return false;
						case ExtractModeEnum.NewOrLatest: if (target.LastWriteTimeUtc >= node.LastModifiedUtc) return false; break;
						case ExtractModeEnum.Overwrite: break;
					}
				}

				#region Dump Raw File

				try
				{

					using (var dataStream = node.Stream)
					{
						dataStream.Seek(0, SeekOrigin.Begin);

						using (FileStream fs = File.Create(absolutePath))
						{
							await dataStream.CopyToAsync(fs, 4096);
						}

						target.LastWriteTimeUtc = node.LastModifiedUtc;
					}
				}
				catch (ZStdException ex)
				{
					return false;
				}

				#endregion
			}

			return true;
		}

		private async Task<Boolean> ExtractNodeAsync(IBranchItem node, String outputRoot, ExtractModeEnum extractMode, String rootPath)
		{
			var result = true;

			if (rootPath == null)
			{
				rootPath = String.Empty;

				if (!String.IsNullOrWhiteSpace(node.RelativePath))
				{
					rootPath = Path.GetDirectoryName(node.RelativePath);
				}
			}
			
			foreach (var child in node.Children.OfType<ITreeItem>())
			{
				result &= await this.ExtractNodeAsync(child, outputRoot, extractMode, rootPath);
			}

			return result;
		}
	}
}
