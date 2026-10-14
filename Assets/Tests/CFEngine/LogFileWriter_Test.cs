using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using CrystalFrost.Logging;

namespace CrystalFrostEngine.Tests
{
	[TestFixture]
	public class LogFileWriter_Test
	{
		private string _tempDirPath = null!;

		[SetUp]
		public void Setup()
		{
			_tempDirPath = Path.Combine(Path.GetTempPath(), "CFLogTests_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_tempDirPath);
		}

		[TearDown]
		public void TearDown()
		{
			if (Directory.Exists(_tempDirPath))
			{
				try
				{
					Directory.Delete(_tempDirPath, true);
				}
				catch
				{
					// Ignore cleanup exceptions
				}
			}
		}

		[Test]
		public async Task LogFileWriter_Enqueue_WritesToFileWithExactFormat()
		{
			var logFilePath = Path.Combine(_tempDirPath, "test_format.log");
			var writer = new LogFileWriter(logFilePath);

			writer.Enqeue(101, LogLevel.Information, "Test message 1");
			writer.Enqeue(102, LogLevel.Warning, "Test message 2");
			writer.Enqeue(103, LogLevel.Error, "Test message 3");

			// Allow background task to drain
			await Task.Delay(300);

			Assert.IsTrue(File.Exists(logFilePath), "Log file should exist.");
			var lines = File.ReadAllLines(logFilePath);
			Assert.AreEqual(3, lines.Length);

			Assert.IsTrue(lines[0].Contains("INFO 101 Test message 1"));
			Assert.IsTrue(lines[1].Contains("WARN 102 Test message 2"));
			Assert.IsTrue(lines[2].Contains("FAIL 103 Test message 3"));
		}

		[Test]
		public async Task LogFileWriter_ConcurrentEnqueue_NoDataLoss()
		{
			var logFilePath = Path.Combine(_tempDirPath, "test_concurrent.log");
			var writer = new LogFileWriter(logFilePath);

			int threadCount = 10;
			int itemsPerThread = 1000;
			int totalExpected = threadCount * itemsPerThread;

			Task[] tasks = new Task[threadCount];
			for (int t = 0; t < threadCount; t++)
			{
				int threadId = t;
				tasks[t] = Task.Run(() =>
				{
					for (int i = 0; i < itemsPerThread; i++)
					{
						writer.Enqeue(threadId, LogLevel.Information, $"Thread {threadId} Item {i}");
					}
				});
			}

			await Task.WhenAll(tasks);

			// Allow background processing to finish
			await Task.Delay(800);

			Assert.IsTrue(File.Exists(logFilePath));
			var lines = File.ReadAllLines(logFilePath);
			Assert.AreEqual(totalExpected, lines.Length, "All enqueued log entries from all threads should be written.");
		}

		[Test]
		public async Task LogFileWriter_MultipleInstances_ExecuteIndependently()
		{
			var logFile1 = Path.Combine(_tempDirPath, "instance1.log");
			var logFile2 = Path.Combine(_tempDirPath, "instance2.log");

			var writer1 = new LogFileWriter(logFile1);
			var writer2 = new LogFileWriter(logFile2);

			var t1 = Task.Run(() =>
			{
				for (int i = 0; i < 500; i++)
				{
					writer1.Enqeue(1, LogLevel.Information, $"Writer1 Msg {i}");
				}
			});

			var t2 = Task.Run(() =>
			{
				for (int i = 0; i < 500; i++)
				{
					writer2.Enqeue(2, LogLevel.Debug, $"Writer2 Msg {i}");
				}
			});

			await Task.WhenAll(t1, t2);
			await Task.Delay(500);

			var lines1 = File.ReadAllLines(logFile1);
			var lines2 = File.ReadAllLines(logFile2);

			Assert.AreEqual(500, lines1.Length);
			Assert.AreEqual(500, lines2.Length);
			Assert.IsTrue(lines1.All(l => l.Contains("INFO 1 Writer1 Msg")));
			Assert.IsTrue(lines2.All(l => l.Contains("DBUG 2 Writer2 Msg")));
		}

		[Test]
		public async Task LogFileWriter_HighVolumeEnqueue_ExecutesRapidlyWithoutStalls()
		{
			var logFilePath = Path.Combine(_tempDirPath, "test_perf.log");
			var writer = new LogFileWriter(logFilePath);

			int totalEntries = 20000;
			var sw = Stopwatch.StartNew();

			for (int i = 0; i < totalEntries; i++)
			{
				writer.Enqeue(i, LogLevel.Trace, $"High throughput message {i}");
			}

			sw.Stop();

			// Enqueue phase must finish almost instantaneously (O(1) non-blocking)
			Assert.IsTrue(sw.ElapsedMilliseconds < 1000, $"Enqueueing {totalEntries} messages took {sw.ElapsedMilliseconds}ms, which indicates blocking.");

			await Task.Delay(1000);

			Assert.IsTrue(File.Exists(logFilePath));
			var lines = File.ReadAllLines(logFilePath);
			Assert.AreEqual(totalEntries, lines.Length);
		}
	}
}
