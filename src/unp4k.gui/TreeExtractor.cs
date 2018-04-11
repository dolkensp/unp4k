using ICSharpCode.SharpZipLib.Zip;
using Ookii.Dialogs.Wpf;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Path = System.IO.Path;
using unp4k.gui.Extensions;
using unp4k.gui.TreeModel;

namespace unp4k.gui
{
	public class TreeExtractor
	{
		private ZipFile _pak;
		public Predicate<Object> Filter { get; }

		public TreeExtractor(ZipFile pak, Predicate<Object> filter)
		{
			this._pak = pak;
			this.Filter = filter;
		}

		public async Task ExtractNodeAsync(TreeItem selectedItem, Boolean useTemp = false)
		{
			if (selectedItem == null) return;

			Boolean? result = false;
			String path = String.Empty;

			if (useTemp)
			{
				path = Path.Combine(Path.GetTempPath(), "unp4k", selectedItem.Title);
				result = true;
			}

			if (String.IsNullOrWhiteSpace(path))
			{
				if (selectedItem is ZipEntryTreeItem zipEntry)
				{
					var dlg = new VistaSaveFileDialog
					{
						FileName = selectedItem.Title,
						OverwritePrompt = true,
						Title = $"Export {selectedItem.Title} File",
						Filter = $"Selected File|{Path.GetExtension(selectedItem.Title)}",
					};

					result = dlg.ShowDialog();
					path = dlg.FileName;
				}

				if (selectedItem is DirectoryTreeItem directory)
				{
					var dlg = new VistaFolderBrowserDialog
					{
						Description = $"Export {selectedItem.Title} Directory",
						UseDescriptionForTitle = true,
						SelectedPath = selectedItem.Title,
					};

					result = dlg.ShowDialog();
					path = dlg.SelectedPath;
				}

				if (selectedItem is ZipFileTreeItem zipFile)
				{
					var dlg = new VistaFolderBrowserDialog
					{
						Description = $"Export {selectedItem.Title} Archive",
						UseDescriptionForTitle = true,
						SelectedPath = selectedItem.Title,
					};

					result = dlg.ShowDialog();
					path = dlg.SelectedPath;
				}
			}

			if (result == true)
			{
				await this.ExtractNodeAsync(selectedItem, path);

				if (useTemp) System.Diagnostics.Process.Start(path);
			}

			await Task.CompletedTask;
		}

		private async Task ExtractNodeAsync(TreeItem node, String outputRoot, String rootPath = null)
		{
			// Early exit
			if (!this.Filter(node)) return;

			if (node is DirectoryTreeItem directory)
			{
				await this.ExtractNodeAsync(directory, outputRoot, rootPath);
			}

			else if (node is ZipEntryTreeItem zipEntry)
			{
				await this.ExtractNodeAsync(zipEntry, outputRoot, rootPath);
			}

			else if (node is ZipFileTreeItem zipFile)
			{
				await this.ExtractNodeAsync(zipFile, outputRoot, rootPath);
			}

			else
			{
				throw new NotSupportedException($"Node type not supported. Node type: {node.GetType().Name}");
			}
		}

		private async Task ExtractNodeAsync(ZipEntryTreeItem node, String outputRoot, String rootPath)
		{
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
				var entry = node.Entry;

				var target = new FileInfo(absolutePath);

				if (!target.Directory.Exists) target.Directory.Create();

				using (Stream zs = this._pak.GetInputStream(entry))
				{
					using (Stream s = new MemoryStream())
					{
						await zs.CopyToAsync(s, 4096);
						
						using (BinaryReader br = new BinaryReader(s))
						{
							#region Check for CryXmlB

							s.Seek(0, SeekOrigin.Begin);

							var peek = br.PeekChar();

							if (peek == 'C')
							{
								String header = br.ReadFString(7);

								if (header == "CryXml" || header == "CryXmlB" || header == "CRY3SDK")
								{
									s.Seek(0, SeekOrigin.Begin);

									var xml = unforge.CryXmlSerializer.ReadStream(s, unforge.ByteOrderEnum.AutoDetect, false);

									xml.Save(absolutePath);

									return;
								}
							}

							#endregion

							// TODO: Check for DataForge

							#region Dump Raw File

							s.Seek(0, SeekOrigin.Begin);

							using (FileStream fs = File.Create(absolutePath))
							{
								await s.CopyToAsync(fs, 4096);
							}

							#endregion
						}
					}
				}
			}
		}

		private async Task ExtractNodeAsync(ZipFileTreeItem node, String outputRoot, String rootPath)
		{
			if (rootPath == null)
			{
				rootPath = String.Empty;

				if (!String.IsNullOrWhiteSpace(node.RelativePath))
				{
					rootPath = Path.GetDirectoryName(node.RelativePath);
				}
			}

			foreach (var child in node.Children)
			{
				await this.ExtractNodeAsync(child, outputRoot, rootPath);
			}
		}

		private async Task ExtractNodeAsync(DirectoryTreeItem node, String outputRoot, String rootPath)
		{
			// Determine the root of the extraction
			if (rootPath == null)
			{
				rootPath = node.RelativePath;
			}

			foreach (var child in node.Children)
			{
				await this.ExtractNodeAsync(child, outputRoot, rootPath);
			}
		}
	}
}
