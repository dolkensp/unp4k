using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace unforge
{
	/// <summary>
	/// Logger for capturing DataForge extraction errors and diagnostics.
	/// </summary>
	public class DataForgeLogger
	{
		public static DataForgeLogger Instance { get; } = new DataForgeLogger();

		public Boolean IsEnabled { get; set; } = false;
		public Boolean VerboseMode { get; set; } = false;

		private readonly List<LogEntry> _entries = new List<LogEntry>();
		private readonly Object _lock = new Object();

		public Int32 ErrorCount { get; private set; } = 0;
		public Int32 WarningCount { get; private set; } = 0;

		public void Clear()
		{
			lock (_lock)
			{
				_entries.Clear();
				ErrorCount = 0;
				WarningCount = 0;
			}
		}

		public void LogError(String message, String recordPath = null, String propertyName = null,
			String dataType = null, Int64? streamPosition = null, Exception exception = null)
		{
			if (!IsEnabled) return;

			lock (_lock)
			{
				ErrorCount++;
				_entries.Add(new LogEntry
				{
					Level = LogLevel.Error,
					Timestamp = DateTime.UtcNow,
					Message = message,
					RecordPath = recordPath,
					PropertyName = propertyName,
					DataType = dataType,
					StreamPosition = streamPosition,
					Exception = exception
				});
			}

			if (VerboseMode)
			{
				Console.Error.WriteLine($"[ERROR] {FormatEntry(_entries[_entries.Count - 1])}");
			}
		}

		public void LogWarning(String message, String recordPath = null, String propertyName = null,
			String dataType = null, Int64? streamPosition = null)
		{
			if (!IsEnabled) return;

			lock (_lock)
			{
				WarningCount++;
				_entries.Add(new LogEntry
				{
					Level = LogLevel.Warning,
					Timestamp = DateTime.UtcNow,
					Message = message,
					RecordPath = recordPath,
					PropertyName = propertyName,
					DataType = dataType,
					StreamPosition = streamPosition
				});
			}

			if (VerboseMode)
			{
				Console.Error.WriteLine($"[WARN] {FormatEntry(_entries[_entries.Count - 1])}");
			}
		}

		public void LogInfo(String message)
		{
			if (!IsEnabled || !VerboseMode) return;

			lock (_lock)
			{
				_entries.Add(new LogEntry
				{
					Level = LogLevel.Info,
					Timestamp = DateTime.UtcNow,
					Message = message
				});
			}

			Console.WriteLine($"[INFO] {message}");
		}

		public void LogDebug(String message, Int64? streamPosition = null)
		{
			if (!IsEnabled || !VerboseMode) return;

			lock (_lock)
			{
				_entries.Add(new LogEntry
				{
					Level = LogLevel.Debug,
					Timestamp = DateTime.UtcNow,
					Message = message,
					StreamPosition = streamPosition
				});
			}

			Console.WriteLine($"[DEBUG] {message}");
		}

		public void SaveToFile(String filePath)
		{
			lock (_lock)
			{
				var sb = new StringBuilder();
				sb.AppendLine("=== DataForge Extraction Log ===");
				sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
				sb.AppendLine($"Total Entries: {_entries.Count}");
				sb.AppendLine($"Errors: {ErrorCount}, Warnings: {WarningCount}");
				sb.AppendLine(new String('=', 50));
				sb.AppendLine();

				foreach (var entry in _entries)
				{
					sb.AppendLine(FormatEntry(entry));
					if (entry.Exception != null)
					{
						sb.AppendLine($"  Stack Trace: {entry.Exception.StackTrace}");
					}
					sb.AppendLine();
				}

				File.WriteAllText(filePath, sb.ToString());
			}
		}

		public String GetSummary()
		{
			lock (_lock)
			{
				if (ErrorCount == 0 && WarningCount == 0)
					return "Extraction completed with no errors.";

				return $"Extraction completed with {ErrorCount} error(s) and {WarningCount} warning(s).";
			}
		}

		public IReadOnlyList<LogEntry> GetEntries()
		{
			lock (_lock)
			{
				return _entries.ToArray();
			}
		}

		private String FormatEntry(LogEntry entry)
		{
			var sb = new StringBuilder();
			sb.Append($"[{entry.Level}] {entry.Message}");

			if (!String.IsNullOrEmpty(entry.RecordPath))
				sb.Append($" | Record: {entry.RecordPath}");

			if (!String.IsNullOrEmpty(entry.PropertyName))
				sb.Append($" | Property: {entry.PropertyName}");

			if (!String.IsNullOrEmpty(entry.DataType))
				sb.Append($" | Type: {entry.DataType}");

			if (entry.StreamPosition.HasValue)
				sb.Append($" | Position: 0x{entry.StreamPosition.Value:X8}");

			if (entry.Exception != null)
				sb.Append($" | Exception: {entry.Exception.GetType().Name}: {entry.Exception.Message}");

			return sb.ToString();
		}

		public enum LogLevel
		{
			Debug,
			Info,
			Warning,
			Error
		}

		public class LogEntry
		{
			public LogLevel Level { get; set; }
			public DateTime Timestamp { get; set; }
			public String Message { get; set; }
			public String RecordPath { get; set; }
			public String PropertyName { get; set; }
			public String DataType { get; set; }
			public Int64? StreamPosition { get; set; }
			public Exception Exception { get; set; }
		}
	}
}
