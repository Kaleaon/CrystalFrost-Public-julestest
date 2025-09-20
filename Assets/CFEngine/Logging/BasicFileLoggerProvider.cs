using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

namespace CrystalFrost.Logging
{
	/// <summary>
	/// A provider for the basic file logger.
	/// </summary>
	public class BasicFileLoggerProvider : ILoggerProvider
	{
		private readonly IConfigurationSection _configuration;
		private readonly LogFileWriter _writer;
		private readonly ConcurrentDictionary<string, BasicFileLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Initializes a new instance of the <see cref="BasicFileLoggerProvider"/> class.
		/// </summary>
		/// <param name="configuration">The configuration.</param>
		public BasicFileLoggerProvider(IConfiguration configuration) 
		{
			_configuration = configuration.GetSection("BasicFileLogger");
			_writer = new LogFileWriter();
		}

		/// <inheritdoc/>
		public ILogger CreateLogger(string categoryName)
		{
			return _loggers.GetOrAdd(categoryName, name => new BasicFileLogger(name, _configuration, _writer));
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			_loggers.Clear();
			GC.SuppressFinalize(this);
		}
	}
}
