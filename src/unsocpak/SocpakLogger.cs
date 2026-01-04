using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace unsocpak
{
	/// <summary>
	/// Logger for capturing socpak extraction errors and diagnostics.
	/// </summary>
	public class SocpakLogger
	{
		public static SocpakLogger Instance { get; } = new SocpakLogger();

		public Boolean IsEnabled { get; set; } = false;
		public Boolean VerboseMode { get; set; } = false;

		private readonly List<LogEntry> _entries = new List<LogEntry>();
		private readonly Object _lock = new Object();

		public Int32 ErrorCount { get; private set; } = 0;
		public Int32 WarningCount { get; private set; } = 0;
		public Int32 SuccessCount { get; private set; } = 0;
		public Int32 SkippedCount { get; private set; } = 0;

		public void Clear()
		{
			lock (_lock)
			{
				_entries.Clear();
				ErrorCount = 0;
				WarningCount = 0;
				SuccessCount = 0;
				SkippedCount = 0;
			}
		}

		public void LogError(String message, String socpakFile = null, String entryName = null, Exception exception = null)
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
					SocpakFile = socpakFile,
					EntryName = entryName,
					Exception = exception
				});
			}

			if (VerboseMode)
			{
				Console.Error.WriteLine($"[ERROR] {FormatEntry(_entries[_entries.Count - 1])}");
			}
		}

		public void LogWarning(String message, String socpakFile = null, String entryName = null)
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
					SocpakFile = socpakFile,
					EntryName = entryName
				});
			}

			if (VerboseMode)
			{
				Console.Error.WriteLine($"[WARN] {FormatEntry(_entries[_entries.Count - 1])}");
			}
		}

		public void LogSuccess(String socpakFile, Int32 entriesExtracted)
		{
			if (!IsEnabled) return;

			lock (_lock)
			{
				SuccessCount++;

				if (VerboseMode)
				{
					_entries.Add(new LogEntry
					{
						Level = LogLevel.Info,
						Timestamp = DateTime.UtcNow,
						Message = $"Extracted {entriesExtracted} entries",
						SocpakFile = socpakFile
					});
				}
			}
		}

		public void LogSkipped(String socpakFile, String reason)
		{
			if (!IsEnabled) return;

			lock (_lock)
			{
				SkippedCount++;

				if (VerboseMode)
				{
					_entries.Add(new LogEntry
					{
						Level = LogLevel.Debug,
						Timestamp = DateTime.UtcNow,
						Message = $"Skipped: {reason}",
						SocpakFile = socpakFile
					});
				}
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

		public void LogDebug(String message)
		{
			if (!IsEnabled || !VerboseMode) return;

			lock (_lock)
			{
				_entries.Add(new LogEntry
				{
					Level = LogLevel.Debug,
					Timestamp = DateTime.UtcNow,
					Message = message
				});
			}

			Console.WriteLine($"[DEBUG] {message}");
		}

		public void SaveToFile(String filePath)
		{
			lock (_lock)
			{
				var sb = new StringBuilder();
				sb.AppendLine("=== Socpak Extraction Log ===");
				sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
				sb.AppendLine($"Total Entries: {_entries.Count}");
				sb.AppendLine($"Extracted: {SuccessCount}, Errors: {ErrorCount}, Warnings: {WarningCount}, Skipped: {SkippedCount}");
				sb.AppendLine(new String('=', 50));
				sb.AppendLine();

				// Group errors and warnings at the top for easy review
				var errors = _entries.FindAll(e => e.Level == LogLevel.Error);
				var warnings = _entries.FindAll(e => e.Level == LogLevel.Warning);

				if (errors.Count > 0)
				{
					sb.AppendLine("=== ERRORS ===");
					foreach (var entry in errors)
					{
						sb.AppendLine(FormatEntry(entry));
						if (entry.Exception != null)
						{
							sb.AppendLine($"  Stack Trace: {entry.Exception.StackTrace}");
						}
						sb.AppendLine();
					}
				}

				if (warnings.Count > 0)
				{
					sb.AppendLine("=== WARNINGS ===");
					foreach (var entry in warnings)
					{
						sb.AppendLine(FormatEntry(entry));
						sb.AppendLine();
					}
				}

				File.WriteAllText(filePath, sb.ToString());
			}
		}

		public String GetSummary()
		{
			lock (_lock)
			{
				if (ErrorCount == 0 && WarningCount == 0)
					return $"Extraction completed: {SuccessCount} socpak files extracted, {SkippedCount} skipped, no errors.";

				return $"Extraction completed: {SuccessCount} socpak files extracted, {SkippedCount} skipped, {ErrorCount} error(s), {WarningCount} warning(s).";
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

			if (!String.IsNullOrEmpty(entry.SocpakFile))
				sb.Append($" | Socpak: {entry.SocpakFile}");

			if (!String.IsNullOrEmpty(entry.EntryName))
				sb.Append($" | Entry: {entry.EntryName}");

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
			public String SocpakFile { get; set; }
			public String EntryName { get; set; }
			public Exception Exception { get; set; }
		}
	}
}
