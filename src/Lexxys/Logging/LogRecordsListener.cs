//#define TraceFlushing
// Lexxys Infrastructural library.
// file: LogRecordsListener.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

#nullable enable

namespace Lexxys.Logging
{
	internal class LogRecordsListener
	{
		private static readonly LogRecordsListener Empty = new LogRecordsListener(0, Array.Empty<int>());
		private static readonly LogRecordsListener[] NoListeners = { Empty, Empty, Empty, Empty, Empty, Empty };

		private static volatile Listener?[] __global = Array.Empty<Listener>();
		private static volatile int __version;

		private readonly int[] _listener;
		private readonly int _version;

		public const int ListenerTurnSleep = 0;
		public const int ListenerPulseInterval = 5000;

		private LogRecordsListener(int varsion, int[] index)
		{
			_version = varsion;
			_listener = index;
		}

		public void Write(LogRecord? record)
		{
			if (record == null)
				return;
			Listener?[] global = __global;
			if (_version != __version)
				return;

			for (int i = 0; i < _listener.Length; ++i)
			{
				Listener? temp = global[_listener[i]];
				if (temp != null)
					temp.Write(record);
			}
		}

		// Should be synced with Collect
		internal static void Initialize(IEnumerable<ILogWriter> writers)
		{
			if (writers == null)
				throw new ArgumentNullException(nameof(writers));
			var next = writers.Where(o => o != null).Select(o => new Listener(o)).ToArray();
			var prev = __global;
			++__version;
			__global = next;
			if (prev.Length > 0)
				DisposeListeners(prev);
		}

		private static void DisposeListeners(IEnumerable<Listener?> listeners)
		{
			foreach (Listener? listener in listeners)
			{
				listener?.Dispose();
			}
		}

		// Should be synced with Initialize
		public static LogRecordsListener[] Collect(string? source)
		{
			var global = __global;
			var version = __version;
			var listeners = new LogRecordsListener[LoggingContext.LogTypeCount];
			for (LogType logType = 0; logType <= LogType.MaxValue; ++logType)
			{
				var indices = new List<int>();
				for (int i = 0; i < global.Length; ++i)
				{
					var item = global[i];
					if (item != null && item.Writer.WillWrite(source, logType))
						indices.Add(i);
				}
				listeners[(int)logType] = indices.Count == 0 ? Empty : new LogRecordsListener(version, indices.ToArray());
			}
			return listeners;
		}

		public static LogRecordsListener[] SelectEmpty()
		{
			return NoListeners;
		}

		public static void ClearBuffers()
		{
			Listener?[] global = __global;
			for (int i = 0; i < global.Length; ++i)
			{
				Listener? temp = global[i];
				if (temp != null)
					temp.ClearBuffers();
			}
		}

		public static void FlushBuffers()
		{
			Listener?[] global = __global;
			for (int i = 0; i < global.Length; ++i)
			{
				global[i]?.FLush();
			}
		}

		public static void StopAll(bool force = false)
		{
			Listener?[] global = __global;
#if TraceFlushing
			long x = WatchTimer.Start();
			Console.WriteLine("Start flushing.");
#endif
			var pool = new List<Thread>(global.Length);
			lock (SyncRoot)
			{
				for (int i = 0; i < global.Length; ++i)
				{
					Listener? listener = global[i];
					if (listener != null)
					{
						global[i] = null;
						if (force || listener.QueueIsEmpty)
						{
							listener.ClearBuffers();
							listener.Stop(force);
						}
						else
						{
							var stopper = new Thread(Stopper);
							pool.Add(stopper);
							stopper.Start(listener);
						}
					}
				}
			}

			foreach (var item in pool)
			{
				item.Join();
			}

#if TraceFlushing
			Console.WriteLine("Stop flushing: {0} sec.", WatchTimer.ToSeconds(WatchTimer.Stop(x)));
#endif
		}

		private static void Stopper(object? obj)
		{
			if (obj is Listener listener)
			{
#if TraceFlushing
				long y = WatchTimer.Start();
				Console.WriteLine($"{listener.Writer.Name} ({listener.RecordCount} {Lingua.Plural("record", listener.RecordCount)})");
#endif
				listener.Stop(false);
#if TraceFlushing
				Console.WriteLine($"{listener.Writer.Name} finish: {WatchTimer.ToSeconds(WatchTimer.Stop(y))} sec.");
#endif
			}
		}

		private readonly static object SyncRoot = new object();

		class Listener: IDisposable
		{
			private const string Source = "Lexxys.Logging.LogRecordsListenner";
			private readonly ILogWriter _writer;
			private LogRecordQueue _queue;
			private volatile EventWaitHandle? _data;
			private readonly Thread _thread;

			public Listener(ILogWriter writer)
			{
				_writer = writer ?? throw new ArgumentNullException(nameof(writer));
				_queue = new LogRecordQueue();
				_data = new AutoResetEvent(false);

				_writer.Open();
				_thread = new Thread(Listen)
				{
					Name = "LOG:" + _writer.Name,
					IsBackground = true,
					Priority = ThreadPriority.Lowest
				};
				_thread.Start();
			}

			public ILogWriter Writer => _writer;

			public bool QueueIsEmpty => _queue.IsEmpty;

#if TraceFlushing
			public int RecordCount => _queue.Count;
#endif

			private bool IsStopping => _data == null;

			public int MaxQueueSize => LoggingContext.MaxQueueSize;

			public void Dispose()
			{
				Stop(false);
			}

			public void ClearBuffers()
			{
				if (!IsStopping)
					_queue = new LogRecordQueue();
			}

			public void FLush()
			{
				if (!_queue.IsEmpty)
				{
					ConcurrentQueue<LogRecord> queue = Interlocked.Exchange(ref _queue, new LogRecordQueue());
					_writer.Write(queue);
				}
			}

			public void Stop(bool force)
			{
				using EventWaitHandle? handle = Interlocked.Exchange(ref _data, null);
				if (handle != null)
				{
					if (!handle.SafeWaitHandle.IsClosed)
						handle.Set();
					Terminate(handle, force);
				}
			}

			private void Terminate(EventWaitHandle data, bool force)
			{
				Thread.Sleep(0);

				if (force)
				{
					_queue = new LogRecordQueue();
					_writer.Write(new [] { new LogRecord(LogType.Warning, Source, "Terminating...", null) });
					_writer.Close();
#if !NETCOREAPP
					LogWriter.WriteEventLogMessage(Source, "Terminating...", LogRecord.Args("ThreadName", _thread.Name));
					if ((_thread.ThreadState & (ThreadState.Stopped | ThreadState.Unstarted | ThreadState.Aborted)) == 0)
						_thread.Abort();
#endif
				}
				else
				{
					try
					{
						if (!_queue.IsEmpty)
							_writer.Write(_queue);

						_writer.Write(new[] { new LogRecord(LogType.Trace, Source, "Exiting...", null) });
						_writer.Close();
						if ((_thread.ThreadState & (ThreadState.Stopped | ThreadState.Unstarted | ThreadState.Aborted)) != 0)
							return;
						if (_thread.Join(0))
							return;
						LogWriter.WriteEventLogMessage(Source, "Waiting for working thread", LogRecord.Args("ThreadName", _thread.Name));
						if (_thread.Join(LoggingContext.LogoffTimeout))
							return;
						LogWriter.WriteErrorMessage(Source, "Thread join operation has been timed out", LogRecord.Args("time-out", LoggingContext.LogoffTimeout));
#if !NETCOREAPP
						_thread.Abort();
#endif
					}
					catch (Exception ex)
					{
						if (ex.IsCriticalException())
							throw;
					}
				}
				data.Dispose();
			}

			private int _count = 0;

			public void Write(LogRecord record)
			{
				if (IsStopping)
					return;
				_queue.Enqueue(record);
				EventWaitHandle? handle = _data;
				if (handle != null)
				{
					handle.Set();
					if (MaxQueueSize > 0 && Interlocked.Increment(ref _count) > MaxQueueSize)
					{
						while (_queue.Count > MaxQueueSize)
							Thread.Sleep(5);
						_count = 0;
					}
				}
			}


			private void Listen()
			{
				while (!IsStopping)
				{
					_data?.WaitOne(ListenerPulseInterval);
					while (!_queue.IsEmpty)
					{
						if (IsStopping)
							break;

						try
						{
							_writer.Write(_queue);
						}
						catch (Exception e)
						{
							LogWriter.WriteErrorMessage(String.Format("Internal error in Log Writer '{0}'", Writer.Name), e);
						}
						Thread.Sleep(ListenerTurnSleep);
					}
				}
			}

			class LogRecordQueue: ConcurrentQueue<LogRecord>, IEnumerable<LogRecord>
			{
				IEnumerator<LogRecord> IEnumerable<LogRecord>.GetEnumerator()
				{
					while (TryDequeue(out LogRecord? rec))
					{
						yield return rec;
					}
				}
			}
		}
	}
}
