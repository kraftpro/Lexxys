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
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Lexxys.Logging.Legacy.Next2;

public static class LogRecordsService
{
	private static LogRecordServiceInstance Instance = new LogRecordServiceInstance(Array.Empty<ILogWriter>());
	private static readonly object SyncRoot = new Object();
	private static volatile int _lockDepth;

	//public static void Start(IEnumerable<ILogWriter> writers)
	//{
	//	if (writers == null)
	//		throw new ArgumentNullException(nameof(writers));

	//	lock (SyncRoot)
	//	{
	//		Instance.Stop();
	//		Instance.SetWriters(writers);
	//		Instance.Start();
	//	}
	//}

	//public static bool IsStarted => !Instance.IsEmpty;
	internal static int MaxQueueSize => LoggingContext.MaxQueueSize;

//	public static void Flush() => Instance.Flush();

//	public static void Stop(bool force = false)
//	{
//#if TraceFlushing
//		long x = WatchTimer.Start();
//		Console.WriteLine("Start flushing.");
//#endif
//		lock (SyncRoot)
//		{
//			Instance.Stop(force);
//			Instance.SetWriters(Array.Empty<ILogWriter>());
//		}

//#if TraceFlushing
//		Console.WriteLine("Stop flushing: {0} sec.", WatchTimer.ToSeconds(WatchTimer.Stop(x)));
//#endif
//	}

	public static int LockLogging() => Interlocked.Increment(ref _lockDepth);

	public static int UnlockLogging() => Interlocked.Decrement(ref _lockDepth);

	internal static ILogRecordWriter GetLogRecordWriter(string source) => Instance.GetLogWriter(source);

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
}


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
				result = Statics.GetLogger(arg ?? "*");
				//result = new Logger(arg ?? "*");
			}
		}
		return result != null;
	}
}

class LogRecordServiceInstance
{
	internal const int LogTypeCount = (int)LogType.MaxValue + 1;

	private static volatile int __servicesVerion;

	private readonly ILogRecordQueueListener[] _listeners;
	private readonly int _version;
	private volatile int _lockDepth;
	private int _stopped;

	public LogRecordServiceInstance(IEnumerable<ILogWriter> writers)
	{
		if (writers is null)
			throw new ArgumentNullException(nameof(writers));
		_listeners = writers.Where(o => o is not null).Select(o => new LogRecordQueueListener(o!)).ToArray();
		_version = Interlocked.Increment(ref __servicesVerion);

		for (int i = 0; i < _listeners.Length; i++)
			_listeners[i].Start();
	}

	public int Version => _version;

	//public bool IsEmpty => _listeners.Length == 0;

	public ILogRecordWriter GetLogWriter(string source)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		return new LogRecordWriter(this, source);
	}

	//private void Start()
	//{
	//	var listeners = _listeners;
	//	for (int i = 0; i < listeners.Length; i++)
	//		listeners[i]?.Start();
	//}

	public void Stop(bool force = false)
	{
		if (Interlocked.CompareExchange(ref _stopped, 1, 0) == 1)
			return;
		var pool = new List<Task>();
		var listeners = _listeners;
		for (int i = 0; i < listeners.Length; ++i)
		{
			var listener = listeners[i];
			var forceListener = force;
			var stopper = new Task(() => listener.Stop(forceListener));
			pool.Add(stopper);
			stopper.Start();
		}
		Task.WaitAll(pool.ToArray());
	}

	public int LockLogging() => Interlocked.Increment(ref _lockDepth);

	public int UnlockLogging() => Interlocked.Decrement(ref _lockDepth);

	public void Write(LogRecord? record, int[]? indexes)
	{
		if (record is null || indexes is null || _lockDepth > 0 || _stopped != 0)
			return;

		var listeners = _listeners;
		for (int i = 0; i < indexes.Length; ++i)
		{
			listeners[indexes[i]]?.Write(record);
		}
	}

	private int[]?[] CollectIndexes(string? source)
	{
		var result = new int[]?[LogTypeCount];
		var indices = new List<int>();
		var listeners = _listeners;
		for (int type = 0; type < LogTypeCount; ++type)
		{
			indices.Clear();
			for (int i = 0; i < listeners.Length; ++i)
			{
				if (listeners[i].Writer.Accepts(source, (LogType)type))
					indices.Add(i);
			}
			result[type] = indices.Count == 0 ? null: indices.ToArray();
		}
		return result;
	}

	class LogRecordWriter: ILogRecordWriter
	{
		//public static readonly ILogRecordWriter Empty = new LogRecordWriter(LogRecordService.Empty, new int[]?[(int)LogType.MaxValue]);

		private LogRecordServiceInstance _service;
		private int[]?[] _indexes;

		public LogRecordWriter(LogRecordServiceInstance service, string source)
		{
			if (service is null)
				throw new ArgumentNullException(nameof(service));
			if (source is null)
				throw new ArgumentNullException(nameof(source));

			_service = service;
			_indexes = _service.CollectIndexes(source);
			Version = service.Version;
			Source = source;
		}

		public void Reset(LogRecordServiceInstance service)
		{
			if (service is null)
				throw new ArgumentNullException(nameof(service));

			_indexes = service.CollectIndexes(Source);
			_service = service;
			Version = service.Version;
		}

		public string Source { get; }

		public int Version { get; private set; }

		public bool IsEnabled(LogType logType) => _indexes[(int)logType] != null;

		public void Write(LogRecord? record)
		{
			if (record == null)
				return;
			_service.Write(record, _indexes[(int)record.LogType]);
		}
	}
}

interface ILogRecordQueueListener
{
	bool QueueIsEmpty { get; }
	ILogWriter Writer { get; }
	void ClearBuffers();
	void Start();
	void Stop(bool force);
	void Write(LogRecord? record);
}

class LogRecordQueueListener: ILogRecordQueueListener
{
	internal const int ListenerTurnSleep = 0;
	internal const int ListenerPulseInterval = 5000;
	private const string Source = "Lexxys.Logging.LogRecordsListenner";
	private readonly ILogWriter _writer;
	private LogRecordQueue _queue;
	private volatile EventWaitHandle? _data;
	private Thread? _thread;

	public LogRecordQueueListener(ILogWriter writer)
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

	private int MaxQueueSize => LogRecordsService.MaxQueueSize;

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
			SystemLog.WriteEventLogMessage(Source, "Terminating '{_writer.Name}'...", LogRecord.Args("ThreadName", _thread!.Name));
			_writer.Write(new[] { new LogRecord(LogType.Warning, Source, "Terminating '{_writer.Name}'...", null) });
#if !NETCOREAPP
			if ((_thread.ThreadState & (ThreadState.Stopped | ThreadState.Unstarted | ThreadState.Aborted)) == 0)
				_thread.Abort();
#endif
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
					SystemLog.WriteErrorMessage(Source, "Thread join operation for '{_writer.Name}' has been timed out", LogRecord.Args("time-out"));
#if !NETCOREAPP
					Interlocked.Exchange(ref _queue, new LogRecordQueue());
#else
					_queue.Clear();
#endif
					if (!data.SafeWaitHandle.IsClosed)
						data.Set();
					_thread!.Join(10);
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

	class LogRecordQueue: ConcurrentQueue<LogRecord>, IEnumerable<LogRecord>
	{
		public LogRecordQueue()
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

