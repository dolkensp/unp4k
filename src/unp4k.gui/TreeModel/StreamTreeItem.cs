using System;
using System.IO;

namespace unp4k.gui.TreeModel
{
	public interface IStreamTreeItem : ITreeItem
	{
		Stream Stream { get; }
	}

	public class StreamTreeItem : TreeItem, IStreamTreeItem
	{
		public Stream Stream => this._streamDelegate();

		public override Object Icon => IconManager.GetCachedFileIcon(
			path: this.Title,
			iconSize: IconManager.IconSize.Large);

		private Func<Stream> _streamDelegate;

		public override DateTime LastModifiedUtc { get; }
		public override Int64 StreamLength { get; }

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

		public StreamTreeItem(String title, Func<Stream> @delegate, DateTime lastModifiedUtc, Int64 streamLength)
			: base(title)
		{
			this.StreamLength = streamLength;
			this.LastModifiedUtc = lastModifiedUtc;

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
