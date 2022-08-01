using MyFTP.Utils;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MyFTP.Services
{
	public class FileLogger : ILogger
	{
		private FileLogger(string fileName)
		{
			_fileName = fileName;
			_queue = new BlockingCollection<string>();
			_cancellationTokenSource = new CancellationTokenSource();
			CreateTask(_cancellationTokenSource.Token);
			AppendFile($" Logger started ".PadBoth(140, '*'));
		}

		~FileLogger() => Dispose(disposing: false);

		private string _fileName;
		private BlockingCollection<string> _queue;
		private Task _writerTask;
		private CancellationTokenSource _cancellationTokenSource;
		private bool disposedValue;

		public bool ShowOnConsole { get; set; } = true;

		public void WriteLine(string message) => WriteLine(message, DateTime.Now);

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void WriteLine(string message, DateTime timestamp) => _queue.TryAdd($"[{timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff")}] {message}");

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					_cancellationTokenSource.Cancel();
				}
				disposedValue = true;
			}
		}

		private void CreateTask(CancellationToken token)
		{
			_writerTask = Task.Run(() =>
			{
				try
				{
					while (true)
					{
						var message = _queue.Take(token);
						AppendFile(message);
					}
				}
				catch (OperationCanceledException)
				{
					while (_queue.TryTake(out var message))
					{
						AppendFile(message);
					}
				}
			}).ContinueWith((task) =>
			{
				AppendFile(" Logger Dispose ".PadBoth(140, '*'));
			});
		}

		private void AppendFile(string line)
		{
			try
			{
				using (var writer = new StreamWriter(_fileName, true))
				{
					writer.WriteLine(line);
					Debug.WriteLineIf(ShowOnConsole, line);
				}
			}
			catch (Exception ex)
			{
				WriteLine(ex.ToString());
			}
		}

		public static FileLogger Create(string fileName)
		{
			try
			{
				EnsureHasPermission(fileName);
				return new FileLogger(fileName);
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		private static void EnsureHasPermission(string fileName)
		{
			var dir = Path.GetDirectoryName(fileName);
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}
			if (File.Exists(fileName))
			{
				using (File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) { }
			}
			else
			{
				using (File.Create(fileName)) { }
			}
		}
	}
}