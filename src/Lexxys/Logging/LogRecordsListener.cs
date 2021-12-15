//#define TraceFlushing
// Lexxys Infrastructural library.
// file: LogRecordsListener.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


#nullable enable

namespace Lexxys.Logging
{
	public static class LogRecordsService
	{
		private const int ListenerTurnSleep = 0;
		private const int ListenerPulseInterval = 5000;

		private static volatile InstanceCollection Instance = new InstanceCollection(Array.Empty<Listener>(), 1);
		private static readonly Object SyncRoot = new Object();
		private static volatile int _lockDepth;

		public static void Start(IEnumerable<ILogWriter> writers)
		{
			if (writers == null)
				throw new ArgumentNullException(nameof(writers));

			lock (SyncRoot)
			{
				var prev = Instance;
				var next = new InstanceCollection(writers.Where(o => o != null).Select(o => new Listener(o)).ToArray(), prev.Version + 1);
				next.Start();
				Interlocked.Exchange(ref Instance, next);
				prev.Stop();
			}
		}

		public static bool IsStarted => !Instance.IsEmpty;

		public static void Flush() => Instance.Flush();

		public static void Stop(bool force = false)
		{
#if TraceFlushing
			long x = WatchTimer.Start();
			Console.WriteLine("Start flushing.");
#endif
			IEnumerable<Thread> pool;
			lock (SyncRoot)
			{
				var inst = Instance;
				Instance = InstanceCollection.Empty;
				pool = inst.StopParallel(force);
			}

			foreach (var item in pool)
			{
				item.Join();
			}

#if TraceFlushing
			Console.WriteLine("Stop flushing: {0} sec.", WatchTimer.ToSeconds(WatchTimer.Stop(x)));
#endif
		}

		public static int LockLogging() => Interlocked.Increment(ref _lockDepth);

		public static int UnlockLogging() => Interlocked.Decrement(ref _lockDepth);

		public static ILogRecordWriter GetLogRecordWriter(string source) => new LogRecordWriter(source, LogRecordWritersMap.Empty);

		public static bool Initialize() => LoggingContext.Initialize();

		class InstanceCollection
		{
			public static readonly InstanceCollection Empty = new InstanceCollection(Array.Empty<Listener>(), 0);

			private readonly Listener?[] _listeners;

			public InstanceCollection(Listener?[] listeners, int version)
			{
				_listeners = listeners ?? throw new ArgumentNullException(nameof(listeners));
				Version = version;
			}

			public int Version { get; }

			public bool IsEmpty => _listeners.Length == 0;

			public void Start()
			{
				for (int i = 0; i < _listeners.Length; i++)
					_listeners[i]?.Start();
			}

			public void Stop(bool force = false)
			{
				for (int i = 0; i < _listeners.Length; i++)
					_listeners[i]?.Stop(force);
			}

			public void Write(LogRecord record, int[]? index)
			{
				if (index == null)
					return;
				for (int i = 0; i < index.Length; ++i)
				{
					_listeners[index[i]]?.Write(record);
				}
			}

			public void Flush()
			{
				for (int i = 0; i < _listeners.Length; i++)
					_listeners[i]?.FLush();
			}

			public IEnumerable<Thread> StopParallel(bool force = false)
			{
				var pool = new List<Thread>();
				var listeners = _listeners;
				for (int i = 0; i < listeners.Length; ++i)
				{
					Listener? listener = listeners[i];
					if (listener != null)
					{
						listeners[i] = null;
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

				return pool;
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

			internal int[]?[] CollectWritersMap(string? source)
			{
				var listeners = _listeners;
				var version = Version;
				var result = new int[]?[LoggingContext.LogTypeCount];
				for (int type = 0; type < LoggingContext.LogTypeCount; ++type)
				{
					var indices = new List<int>();
					for (int i = 0; i < listeners.Length; ++i)
					{
						if (listeners[i]?.Writer.Accepts(source, (LogType)type) ?? false)
							indices.Add(i);
					}
					if (indices.Count > 0)
						result[type] = indices.ToArray();
				}
				return result;
			}
		}

		class LogRecordWriter: ILogRecordWriter
		{
			private volatile LogRecordWritersMap _map;
			private readonly string _source;

			public LogRecordWriter(string source, LogRecordWritersMap map)
			{
				_source = source ?? throw new ArgumentNullException(nameof(source));
				_map = map ?? throw new ArgumentNullException(nameof(map));
			}

			public bool IsEnabled(LogType logType)
			{
				return Actual().Map.Supports(logType);
			}

			public void Write(LogRecord? record)
			{
				if (record == null || _lockDepth > 0)
					return;
				var (inst, map) = Actual();
				map.Write(record, inst);
			}

			private (InstanceCollection Inst, LogRecordWritersMap Map) Actual()
			{
				InstanceCollection inst;
				LogRecordWritersMap map;
				do
				{
					inst = Instance;
					map = _map;
					if (inst.Version == map.Version)
						break;
					if (Initialize())
						inst = Instance;
					map = new LogRecordWritersMap(inst.Version, inst.CollectWritersMap(_source));
					Interlocked.Exchange(ref _map, map);
				} while (inst.Version == Instance.Version);
				return (inst, map);
			}
		}

		class LogRecordWritersMap
		{
			public static readonly LogRecordWritersMap Empty = new LogRecordWritersMap(0, new int[]?[LoggingContext.LogTypeCount]);

			private readonly int[]?[] _indexes;

			public LogRecordWritersMap(int version, int[]?[] indexes)
			{
				Version = version;
				_indexes = indexes ?? throw new ArgumentNullException(nameof(indexes));
			}

			public int Version { get; }

			public bool Supports(LogType logType)
				=> _indexes[(int)logType] != null;

			public void Write(LogRecord record, InstanceCollection instance)
				=> instance.Write(record, _indexes[(int)record.LogType]);
		}

		class Listener
		{
			private const string Source = "Lexxys.Logging.LogRecordsListenner";
			private readonly ILogWriter _writer;
			private LogRecordQueue _queue;
			private volatile EventWaitHandle? _data;
			private Thread? _thread;

			public Listener(ILogWriter writer)
			{
				_writer = writer ?? throw new ArgumentNullException(nameof(writer));
				_queue = new LogRecordQueue();
				_data = new AutoResetEvent(false);
			}

			public ILogWriter Writer => _writer;

			public bool QueueIsEmpty => _queue.IsEmpty;

#if TraceFlushing
			public int RecordCount => _queue.Count;
#endif

			public void Start()
			{
				if (_thread == null)
				{
					_writer.Open();
					_thread = new Thread(Listen)
					{
						Name = "LOG:" + _writer.Name,
						IsBackground = true,
						Priority = ThreadPriority.Lowest
					};
					_thread.Start();
				}
			}

			private bool IsStopping => _data == null;

			private bool IsStarted => _thread != null;

			public int MaxQueueSize => LoggingContext.MaxQueueSize;

			public void ClearBuffers()
			{
				if (!IsStopping)
					_queue = new LogRecordQueue();
			}

			public void FLush()
			{
				if (!IsStarted || _queue.IsEmpty)
					return;
				ConcurrentQueue<LogRecord> queue = Interlocked.Exchange(ref _queue, new LogRecordQueue());
				_writer.Write(queue);
			}

			public void Stop(bool force)
			{
				if (!IsStarted)
					return;

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
				if (!IsStarted)
					return;

				Thread.Sleep(0);

				if (force)
				{
					_queue = new LogRecordQueue();
					_writer.Write(new[] { new LogRecord(LogType.Warning, Source, "Terminating...", null) });
					_writer.Close();
#if !NETCOREAPP
					LogWriter.WriteEventLogMessage(Source, "Terminating...", LogRecord.Args("ThreadName", _thread!.Name));
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
						if ((_thread!.ThreadState & (ThreadState.Stopped | ThreadState.Unstarted | ThreadState.Aborted)) != 0)
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

			public void Write(LogRecord? record)
			{
				if (record == null || IsStopping || !IsStarted)
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

			class LogRecordQueue : ConcurrentQueue<LogRecord>, IEnumerable<LogRecord>
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
