using AdvancedDLSupport;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("DLSupportDynamicAssembly")]

namespace Zstd.Net
{
	public class ZStdException : Exception
	{
		public ZStdException(String message) : base(message) { }
		public ZStdException(Exception ex) : base("Error processing file", ex) { }
	}

	public class InputStream : Stream
	{
		private readonly Stream _inputStream;
		private readonly bool _leaveOpen;
		private readonly IntPtr _zst;
		private readonly byte[] _inputBufferArray;
		private bool _closed;
		private int _inputArrayPosition;
		private int _inputArraySize;
		private bool _depleted;

		public static bool IsZstdStream(byte[] buffBytes, Int64 buffLen)
		{
			//0xFD2FB528 LE
			return buffLen > 3
				&& buffBytes[0] == 0x28
				&& buffBytes[1] == 0xB5
				&& buffBytes[2] == 0x2F
				&& buffBytes[3] == 0xFD;
		}

		public InputStream(Stream inputStream, bool leaveOpen = false)
		{
			_inputStream = inputStream;
			_leaveOpen = leaveOpen;
			_zst = Zstd.Library.ZSTD_createDStream();
			Zstd.CheckError(Zstd.Library.ZSTD_initDStream(_zst));
			_inputBuffer.Size = Zstd.Library.ZSTD_DStreamInSize();
			_inputBufferArray = new byte[(int)_inputBuffer.Size.ToUInt32()];
			_outputBuffer.Size = Zstd.Library.ZSTD_DStreamOutSize();
		}

		protected override void Dispose(bool disposing)
		{
			if (_closed) return;
			Zstd.CheckError(Zstd.Library.ZSTD_freeDStream(_zst));
			if (!_leaveOpen) _inputStream.Dispose();
			_closed = true;
			base.Dispose(disposing);
		}

		public override void Flush() { }

		public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }

		public override void SetLength(long value) { throw new NotSupportedException(); }

		public override int Read(byte[] buffer, int offset, int count)
		{
			if (count == 0) return 0;
			var retVal = 0;
			var alloc1 = GCHandle.Alloc(_inputBufferArray, GCHandleType.Pinned);
			var alloc2 = GCHandle.Alloc(buffer, GCHandleType.Pinned);
			try
			{
				while (count > 0)
				{
					var left = _inputArraySize - _inputArrayPosition;
					if (left <= 0 && !_depleted)
					{
						_inputArrayPosition = 0;
						_inputArraySize = left = _inputStream.Read(_inputBufferArray, 0, _inputBufferArray.Length);
						// no more data at all
						if (left <= 0)
						{
							left = 0;
							_depleted = true;
						}
					}
					_inputBuffer.Position = UIntPtr.Zero;
					if (_depleted)
					{
						_inputBuffer.Size = UIntPtr.Zero;
						_inputBuffer.Data = IntPtr.Zero;
					}
					else
					{
						_inputBuffer.Size = new UIntPtr((uint)left);
						_inputBuffer.Data = Marshal.UnsafeAddrOfPinnedArrayElement(_inputBufferArray, _inputArrayPosition);
					}

					_outputBuffer.Position = UIntPtr.Zero;
					_outputBuffer.Size = new UIntPtr((uint)count);
					_outputBuffer.Data = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset);
					Zstd.CheckError(Zstd.Library.ZSTD_decompressStream(_zst, _outputBuffer, _inputBuffer));
					var bytesProduced = (int)_outputBuffer.Position.ToUInt32();
					if (bytesProduced == 0 && _depleted) break;
					retVal += bytesProduced;
					count -= bytesProduced;
					offset += bytesProduced;
					if (_depleted) continue;
					var bytesConsumed = (int)_inputBuffer.Position.ToUInt32();
					_inputArrayPosition += bytesConsumed;
				}
				return retVal;
			}
			catch (Exception ex)
			{
				throw new ZStdException(ex);
			}
			finally
			{
				alloc1.Free();
				alloc2.Free();
			}
		}

		private readonly Zstd.Buffer _inputBuffer = new Zstd.Buffer();
		private readonly Zstd.Buffer _outputBuffer = new Zstd.Buffer();

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		public override bool CanRead
		{
			get { return _inputStream.CanRead; }
		}

		public override bool CanSeek
		{
			get { return false; }
		}

		public override bool CanWrite
		{
			get { return false; }
		}

		public override long Length
		{
			get { return 0; }
		}

		public override long Position
		{
			get { return 0; }
			set { }
		}
	}

	internal static class Zstd
	{
		internal static IImportZstd Library { get; }

		static Zstd()
		{
			try
			{
				var activator = new NativeLibraryBuilder { };

				// Detect which DLL version we need for Windows
				if (Environment.OSVersion.Platform == PlatformID.Win32NT)
				{
					if (Environment.Is64BitProcess)
					{
						Zstd.Library = activator.ActivateInterface<IImportZstd>("x64/libzstd");
					}
					else
					{
						Zstd.Library = activator.ActivateInterface<IImportZstd>("x86/libzstd");
					}
				}
				else
				{
					Zstd.Library = activator.ActivateInterface<IImportZstd>("libzstd");
				}
			} catch (Exception ex) {
				do
				{
					Console.WriteLine(ex.Message);
					Console.WriteLine(ex.Source);
					ex = ex.InnerException;
				} while (ex != null);

				Console.WriteLine();
				Console.WriteLine("Try placing libzstd compiled for your distribution in the bin directory alongside unp4k");
				throw;
			}
		}

		internal static void CheckError(UIntPtr x)
		{
			var code = Zstd.Library.ZSTD_isError(x);
			if (code == 0) return;

			throw new ZStdException($"Error {x}:{code}");
			// Debug.WriteLine(Zstd.Library.GetErrorName(x));
			// throw new ZStdException(Zstd.Library.GetErrorName(x));
		}

		[StructLayout(LayoutKind.Sequential)]
		internal sealed class Buffer
		{
			public IntPtr Data;
			public UIntPtr Size;
			public UIntPtr Position;
		}

		internal interface IImportZstd
		{
			Int32 ZSTD_maxCLevel();
			Int32 ZSTD_versionNumber();
			String ZSTD_versionString();

			Int32 ZSTD_isError(UIntPtr code);
			String ZSTD_getErrorName(UIntPtr code);

			IntPtr ZSTD_createCStream();
			UIntPtr ZSTD_freeCStream(IntPtr zcs);
			UIntPtr ZSTD_initCStream(IntPtr zcs, int compressionLevel);
			UIntPtr ZSTD_compressStream(IntPtr zcs, Buffer outputBuffer, Buffer inputBuffer);
			UIntPtr ZSTD_CStreamInSize();
			UIntPtr ZSTD_CStreamOutSize();

			IntPtr ZSTD_createDStream();
			UIntPtr ZSTD_freeDStream(IntPtr zcs);
			UIntPtr ZSTD_initDStream(IntPtr zcs);
			UIntPtr ZSTD_decompressStream(IntPtr zcs, Buffer outputBuffer, Buffer inputBuffer);
			UIntPtr ZSTD_DStreamInSize();
			UIntPtr ZSTD_DStreamOutSize();

			UIntPtr ZSTD_flushStream(IntPtr zcs, Buffer outputBuffer);
			UIntPtr ZSTD_endStream(IntPtr zcs, Buffer outputBuffer);
		}
	}
}
