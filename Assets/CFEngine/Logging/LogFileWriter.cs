using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CrystalFrost.Logging
{
	/// <summary>
	/// Defines an interface for a log file writer.
	/// </summary>
	public interface ILogFileWriter
	{
		/// <summary>
		/// Enqueues a log message to be written to the file.
		/// </summary>
		/// <param name="eventId">The event ID associated with the log entry.</param>
		/// <param name="level">The level of the log entry.</param>
		/// <param name="message">The log message.</param>
		void Enqeue(int eventId, LogLevel level, string message);
	}

	/// <summary>
	/// Implements a log file writer that asynchronously writes log messages to a file.
	/// </summary>
	public class LogFileWriter : ILogFileWriter
	{
		private readonly string _filename;
		private int _isProcessing = 0;
		private Exception? _lastException;

		private class Entry
		{
			public LogLevel Level;
			public string Message;
			public DateTime Occurred;
			public int EventId;
		}

		private readonly ConcurrentQueue<Entry> _entries = new();

		/// <summary>
		/// Initializes a new instance of the <see cref="LogFileWriter"/> class.
		/// </summary>
		public LogFileWriter()
		{
			var logDir = Path.Combine(UnityEngine.Application.persistentDataPath, "logs");
			if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);

			_filename = Path.Combine(logDir, DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log");
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LogFileWriter"/> class with a specified filename.
		/// </summary>
		/// <param name="filename">The custom filename or path for the log file.</param>
		public LogFileWriter(string filename)
		{
			_filename = filename;
			var logDir = Path.GetDirectoryName(_filename);
			if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
			{
				Directory.CreateDirectory(logDir);
			}
		}

		private void ProcessQueue()
		{
			try
			{
				using var filestream = new FileStream(_filename, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
				using var streamwriter = new StreamWriter(filestream, Encoding.UTF8);

				bool wroteAny = false;
				while (_entries.TryDequeue(out Entry entry))
				{
					if (entry is null) continue;
					streamwriter.Write(entry.Occurred.ToString("s"));
					streamwriter.Write(" ");
					streamwriter.Write(LevelString(entry.Level));
					streamwriter.Write(" ");
					streamwriter.Write(entry.EventId.ToString());
					streamwriter.Write(" ");
					streamwriter.WriteLine(entry.Message);
					wroteAny = true;
				}

				if (wroteAny)
				{
					streamwriter.Flush();
				}
			}
			catch (Exception ex)
			{
				_lastException = ex;
			}
			finally
			{
				Interlocked.Exchange(ref _isProcessing, 0);

				if (!_entries.IsEmpty)
				{
					if (Interlocked.CompareExchange(ref _isProcessing, 1, 0) == 0)
					{
						Task.Run(ProcessQueue);
					}
				}
			}
		}

		private static string LevelString(LogLevel level)
		{
			return level switch
			{
				LogLevel.Trace => "TRCE",
				LogLevel.Debug => "DBUG",
				LogLevel.Information => "INFO",
				LogLevel.Warning => "WARN",
				LogLevel.Error => "FAIL",
				LogLevel.Critical => "CRIT",
				_ => "????",
			};
		}

		/// <summary>
		/// Enqueues a log message to be written to the file and starts the writer task if not running.
		/// </summary>
		/// <param name="eventId">The event ID associated with the log entry.</param>
		/// <param name="level">The level of the log entry.</param>
		/// <param name="message">The log message.</param>
		public void Enqeue(int eventId, LogLevel level, string message)
		{
			if (_lastException != null)
			{
				var ex = _lastException;
				_lastException = null;
				throw ex;
			}

			_entries.Enqueue(new Entry
			{
				Occurred = DateTime.Now,
				Message = message,
				Level = level,
				EventId = eventId,
			});

			if (Interlocked.CompareExchange(ref _isProcessing, 1, 0) == 0)
			{
				Task.Run(ProcessQueue);
			}
		}
	}
}
