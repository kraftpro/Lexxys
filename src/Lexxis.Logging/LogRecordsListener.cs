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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

using Microsoft.Extensions.Logging;

namespace Lexxys.Logging;

public static class LogRecordsService
{
	private const int ListenerTurnSleep = 0;
	private const int ListenerPulseInterval = 5000;

	private static volatile InstanceCollection Instance = new InstanceCollection(Array.Empty<Listener>(), 1);
	private static readonly Object SyncRoot = new Object();
	private static volatile int _lockDepth;
	private static readonly ConcurrentDictionary<string, ILogRecordWriter> _writers = new ConcurrentDictionary<string, ILogRecordWriter>(StringComparer.OrdinalIgnoreCase);

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

	//public static void Flush() => Instance.Flush();

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

	internal static ILogRecordWriter GetLogRecordWriter(string source) => new LogRecordWriter(source, LogRecordWritersMap.Empty);

	public static bool Initialize() => LoggingContext.Initialize();

	public static void RegisterFactory()
	{
		if (!_registered)
		{
			_registered = true;
			StaticServices.AddFactory(LoggerFactory.Instance);
		}
	}
	private static bool _registered;

	class LoggerFactory: StaticServices.IFactory
	{
		public static readonly StaticServices.IFactory Instance = new LoggerFactory();

		public IReadOnlyCollection<Type> SupportedTypes => _supportedTypes;
		private readonly Type[] _supportedTypes = new Type[] { typeof(ILogger), typeof(ILogger<>), typeof(ILogging), typeof(ILogging<>) };

		public bool TryCreate(Type type, object?[]? arguments, [MaybeNullWhen(false)] out object result)
		{
			result = null!;
			if (type.IsGenericType)
			{
				var generic = type.GetGenericTypeDefinition();
				if (generic == typeof(ILogging<>) || generic == typeof(ILogger<>))
				{
					var loggerType = typeof(Logger<>).MakeGenericType(type.GetGenericArguments());
					result = Activator.CreateInstance(loggerType);
				}

			}
			else
			{
				if (type == typeof(ILogging) || type == typeof(ILogger))
				{
					var arg = arguments?.Length > 0 && arguments[0] != null ? arguments[0]!.ToString(): null;
					result = new Logger(arg ?? "*");
					return true;
				}
			}
			return result != null;
		}
	}

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
			} while (map.Version != Instance.Version);
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

		//public void FLush()
		//{
		//	if (!IsStarted || _queue.IsEmpty)
		//		return;
		//	ConcurrentQueue<LogRecord> queue = Interlocked.Exchange(ref _queue, new LogRecordQueue());
		//	_writer.Write(queue);
		//}

		public void Stop(bool force)
		{
			if (!IsStarted)
				return;

			using EventWaitHandle? handle = Interlocked.Exchange(ref _data, null);
			if (handle != null)
				Terminate(handle, force);
		}

		private void Terminate(EventWaitHandle data, bool force)
		{
			Thread.Sleep(0);

			if (force)
			{
#if !NETCOREAPP
				Interlocked.Exchange(ref _queue, new LogRecordQueue());
#else
				_queue.Clear();
#endif
				if (!data.SafeWaitHandle.IsClosed)
					data.Set();
				Thread.Sleep(0);
				if ((_thread!.ThreadState & (ThreadState.Stopped | ThreadState.Unstarted | ThreadState.Aborted)) != 0)
					_thread!.Join(100);
				SystemLog.WriteEventLogMessage(Source, "Terminating...", LogRecord.Args("ThreadName", _thread!.Name));
#if !NETCOREAPP
				if ((_thread.ThreadState & (ThreadState.Stopped | ThreadState.Unstarted | ThreadState.Aborted)) == 0)
					_thread.Abort();
#endif
				_writer.Write(new[] { new LogRecord(LogType.Warning, Source, "Terminating...", null) });
			}
			else
			{
				try
				{
					if (!data.SafeWaitHandle.IsClosed)
						data.Set();
					Thread.Sleep(0);

					var stopped = (_thread!.ThreadState & (ThreadState.Stopped | ThreadState.Unstarted | ThreadState.Aborted)) != 0 || _thread!.Join(LoggingContext.FlushTimeout);
					if (!stopped)
					{
						SystemLog.WriteEventLogMessage(Source, "Waiting for working thread", LogRecord.Args("ThreadName", _thread.Name));
						stopped = (_thread!.ThreadState & (ThreadState.Stopped | ThreadState.Unstarted | ThreadState.Aborted)) != 0 || _thread!.Join(LoggingContext.LogoffTimeout);
						if (!stopped)
						{
							SystemLog.WriteErrorMessage(Source, "Thread join operation has been timed out", LogRecord.Args("time-out", LoggingContext.LogoffTimeout));
#if !NETCOREAPP
							Interlocked.Exchange(ref _queue, new LogRecordQueue());
#else
							_queue.Clear();
#endif
							if (!data.SafeWaitHandle.IsClosed)
								data.Set();
							_thread!.Join(10);
						}
					}
#if !NETCOREAPP
					if ((_thread!.ThreadState & (ThreadState.Stopped | ThreadState.Unstarted | ThreadState.Aborted)) != 0)
						_thread.Abort();
#endif
					_writer.Write(new[] { new LogRecord(LogType.Trace, Source, "Exiting...", null) });
				}
				catch (Exception flaw)
				{
					if (flaw.IsCriticalException())
						throw;
				}
				try
				{
					_writer.Close();
				}
				catch (Exception flaw)
				{
					if (flaw.IsCriticalException())
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
						SystemLog.WriteErrorMessage(String.Format("Internal error in Log Writer '{0}'", Writer.Name), e);
					}
					Thread.Sleep(ListenerTurnSleep);
				}
			}
		}

		class LogRecordQueue : ConcurrentQueue<LogRecord>, IEnumerable<LogRecord>
		{
			public LogRecordQueue()
			{
			}

			public LogRecordQueue(IEnumerable<LogRecord> records): base(records)
			{
			}

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
