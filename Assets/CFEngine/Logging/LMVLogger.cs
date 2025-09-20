using Microsoft.Extensions.Logging;
using System;

namespace CrystalFrost.Logging
{
	/// <summary>
	/// Defines the interface for a logger that captures log messages from the LibMetaverse library.
	/// </summary>
	public interface ILMVLogger : IDisposable { }

	/// <summary>
	/// A logger that captures log messages from the LibMetaverse library.
	/// </summary>
	public class LMVLogger : ILMVLogger
	{
		private readonly ILogger<LMVLogger> _log;

		/// <summary>
		/// Initializes a new instance of the <see cref="LMVLogger"/> class.
		/// </summary>
		/// <param name="log">A logger for logging messages.</param>
		public LMVLogger(ILogger<LMVLogger> log)
		{
			_log = log;
			OpenMetaverse.Logger.OnLogMessage += OpenMetaverseLogger_OnLogMessage;
		}

		private void OpenMetaverseLogger_OnLogMessage(object message, OpenMetaverse.Helpers.LogLevel level)
		{
			switch (level)
			{
				case OpenMetaverse.Helpers.LogLevel.Debug:
					_log.LMV_Debug((string)message);
					break;
				case OpenMetaverse.Helpers.LogLevel.Info:
					_log.LMV_Information((string)message);
					break;
				case OpenMetaverse.Helpers.LogLevel.Warning:
					_log.LMV_Warning((string)message);
					break;
				case OpenMetaverse.Helpers.LogLevel.Error:
					_log.LMV_Error((string)message);
					break;
				default:
					break;
			}
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			OpenMetaverse.Logger.OnLogMessage -= OpenMetaverseLogger_OnLogMessage;
			GC.SuppressFinalize(this);
		}
	}
}
