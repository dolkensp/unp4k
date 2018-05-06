using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using unp4k.gui.Plugins;
using unp4k.gui.TreeModel;

namespace unp4k.gui.TreeModel
{
	public interface IStreamTreeItem : ITreeItem
	{
		Stream Stream { get; }
		DateTime LastWriteTimeUtc { get; }
	}

	public class StreamTreeItem : TreeItem, IStreamTreeItem
	{
		public Stream Stream => this._streamDelegate();
		public virtual DateTime LastWriteTimeUtc { get; }

		public override ImageSource Icon => IconManager.GetCachedFileIcon(
			path: this.Title,
			iconSize: IconManager.IconSize.Large);

		private Func<Stream> _streamDelegate;

		//private Func<Stream> GetSeekableDelegate(Stream stream)
		//{
		//	return () =>
		//	{
		//		var targetStream = this._stream ?? stream;

		//		if (targetStream?.CanSeek == false)
		//		{
		//			this._stream = new MemoryStream { };
		//			targetStream.CopyTo(this._stream);
		//			targetStream.Dispose();
		//			targetStream = this._stream;
		//		}

		//		this._stream = targetStream;

		//		this._stream.Seek(0, SeekOrigin.Begin);

		//		return this._stream;
		//	};
		//}

		// public StreamTreeItem(String title, ITreeItem parent, Stream stream)
		// 	: base(title, parent)
		// {
		// 	this._streamDelegate = this.GetSeekableDelegate(stream);
		// }

		public StreamTreeItem(String title, ITreeItem parent, DateTime lastWriteTimeUtc, Func<Stream> @delegate)
			: base(title, parent)
		{
			this.LastWriteTimeUtc = lastWriteTimeUtc;

			this._streamDelegate = () =>
			{
				var targetStream = @delegate();

				if (targetStream?.CanSeek == true) return targetStream;

				var buffer = new MemoryStream { };

				targetStream.CopyTo(buffer);
				targetStream.Dispose();

				return buffer;
			};
		}
	}
}
