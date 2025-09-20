using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace CrystalFrost.Logging
{
	/// <summary>
	/// A basic file logger.
	/// </summary>
	public class BasicFileLogger : ILogger
	{
		/// <summary>
		/// Holds the desired level for this category.
		/// </summary>
		private readonly LogLevel _logLevel;
		private readonly ILogFileWriter _writer;

		/// <summary>
		/// Initializes a new instance of the <see cref="BasicFileLogger"/> class.
		/// </summary>
		/// <param name="categoryName">The name of the category.</param>
		/// <param name="configuration">The configuration.</param>
		/// <param name="writer">The log file writer.</param>
		public BasicFileLogger(string categoryName, IConfiguration configuration,
			ILogFileWriter writer)
		{
			// Look in the configuration for a log level section,
			// and in there look for value with out category name.
			// if that value exists use it for our level.
			// if a value with a name matching our category was not found.
			// use the default category.
			// if there is no default category use 'Information' as the level.
			var logLevelSection = configuration.GetSection("LogLevel");
			var level = logLevelSection[categoryName];
			level ??= logLevelSection["Default"];
			level ??= "Information";

			// convert the string from the configuration to the enum.
			// defaulting to information if there are problems.
			_logLevel = Enum.TryParse<LogLevel>(level, out var parsed)
				? parsed
				: LogLevel.Information;

			_writer = writer;
		}

		/// <inheritdoc/>
		public IDisposable BeginScope<TState>(TState state) where TState : notnull
		{
			// no need for logging scopes just yet.
			return default;
		}

		/// <inheritdoc/>
		public bool IsEnabled(LogLevel logLevel)
		{
			return logLevel >= _logLevel;
		}

		/// <inheritdoc/>
		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			_writer.Enqeue(eventId.Id, logLevel, formatter(state, exception));
		}
	}
}
