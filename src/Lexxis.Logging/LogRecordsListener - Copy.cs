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
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Printing;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

using Lexxys;

namespace Lexxys.Logging;

//public static class LogRecordsService
//{
//	private static volatile LogRecordService Instance = new LogRecordService();
//	private static readonly Object SyncRoot = new Object();
//	private static volatile int _lockDepth;

//	public static void Start(IEnumerable<ILogWriter> writers)
//	{
//		if (writers == null)
//			throw new ArgumentNullException(nameof(writers));

//		lock (SyncRoot)
//		{
//			Instance.Stop();
//			Instance.SetWriters(writers);
//			Instance.Start();
//		}
//	}

//	public static bool IsStarted => !Instance.IsEmpty;
//	//internal static int MaxQueueSize => LoggingContext.MaxQueueSize;
//	//public static void Flush() => Instance.Flush();

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

//	public static int LockLogging() => Interlocked.Increment(ref _lockDepth);

//	public static int UnlockLogging() => Interlocked.Decrement(ref _lockDepth);

//	internal static ILogRecordWriter GetLogRecordWriter(string source) => Instance.GetLogWriter(source);

//	//public static bool Initialize() => LoggingContext.Initialize();

//	public static void RegisterFactory()
//	{
//		if (!_registered)
//		{
//			_registered = true;
//			StaticServices.AddFactory(LoggerFactory.Instance);
//		}
//	}
//	private static bool _registered;
//}


//class LoggerFactory: StaticServices.IFactory
//{
//	public static readonly StaticServices.IFactory Instance = new LoggerFactory();

//	public IReadOnlyCollection<Type> SupportedTypes => _supportedTypes;
//	private readonly Type[] _supportedTypes = new Type[] { typeof(ILogger), typeof(ILogger<>), typeof(ILogging), typeof(ILogging<>) };

//	public bool TryCreate(Type type, object?[]? arguments, [MaybeNullWhen(false)] out object result)
//	{
//		result = null!;
//		if (type.IsGenericType)
//		{
//			var generic = type.GetGenericTypeDefinition();
//			if (generic == typeof(ILogging<>) || generic == typeof(ILogger<>))
//			{
//				var loggerType = typeof(Logger<>).MakeGenericType(type.GetGenericArguments());
//				result = Activator.CreateInstance(loggerType);
//			}
//		}
//		else
//		{
//			if (type == typeof(ILogging) || type == typeof(ILogger))
//			{
//				var arg = arguments?.Length > 0 && arguments[0] != null ? arguments[0]!.ToString(): null;
//				result = new Logger(arg ?? "*");
//			}
//		}
//		return result != null;
//	}
//}

class LogRecordService: ILoggingService
{
	internal const int LogTypeCount = (int)LogType.MaxValue + 1;

	internal const int DefaultMaxQueueSize = 256 * 1024;
	internal static readonly TimeSpan DefaultFlushTimeout = TimeSpan.FromSeconds(5);
	internal const int LogWatcherSleep = 500;

	private volatile ILogRecordQueueListener[] _listeners;
	private volatile int _lockDepth;
	private volatile int _version;
	public event Action? Changed;

	public LogRecordService(ILoggingParameters parameters)
	{
		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		var lstnrs = new List<ILogRecordQueueListener>();
		foreach (var item in parameters)
		{
			if (item == null)
				continue;
			var wr = item.CreateWriter();
			lstnrs.Add(new LogRecordQueueListener(wr, item.MaxQueueSize, item.FlushTimeout));
		}
		_version = 1;
		_listeners = lstnrs.ToArray();
		SetTerminationHandlers();
	}

	private void SetTerminationHandlers()
	{
		bool registered = false;
		try
		{
			AppDomain.CurrentDomain.ProcessExit += OnExit;
			registered = true;
		}
		catch
		{
			// ignored
		}
		Lxx.UnhandledException += OnError;
		if (!registered)
		{
			var watcher = new Thread(LogWatcher) { Name = "LOG Watcher", Priority = ThreadPriority.Lowest };
			watcher.Start(Thread.CurrentThread);
		}

		//Lxx.ConfigurationChanged += Lxx_ConfigurationChanged;
	}

	private void OnExit(object? sender, EventArgs e) => Stop();
	private void OnError(object? sender, ThreadExceptionEventArgs e) => Stop();
	private void LogWatcher(object? caller)
	{
		if (caller is not Thread main)
			return;
		while (main.IsAlive)
		{
			Thread.Sleep(LogWatcherSleep);
		}
		Stop();
	}

	//public LogRecordService(IEnumerable<ILogWriter?>? writers = null)
	//{
	//	_version = 1;
	//	_listeners = writers is null ?
	//		Array.Empty<ILogRecordQueueListener>() :
	//		writers.Where(o => o is not null).Select(o => new LogRecordQueueListener(o!)).ToArray();
	//}

	public bool IsEmpty => _listeners.Length == 0;

	public void AddWriters(IEnumerable<ILogWriter?> writers)
	{
		if (writers is null)
			throw new ArgumentNullException(nameof(writers));

		var append = writers.Where(o => o is not null).Select(o => new LogRecordQueueListener(o!)).ToArray();
		if (append.Length == 0)
			return;

		lock (this)
		{
			var copy = _listeners;
			var array = new ILogRecordQueueListener[copy.Length + append.Length];
			Array.Copy(copy, array, copy.Length);
			Array.Copy(append, 0, array, copy.Length, append.Length);
			++_version;
			Interlocked.Exchange(ref _listeners, array);
		}
		OnChanged();
	}

	public void SetWriters(IEnumerable<ILogWriter?> writers)
	{
		if (writers is null)
			throw new ArgumentNullException(nameof(writers));

		lock (this)
		{
			var array = writers.Where(o => o is not null).Select(o => new LogRecordQueueListener(o!)).ToArray();
			++_version;
			Interlocked.Exchange(ref _listeners, array);
		}
		OnChanged();
	}

	public ILogRecordWriter GetLogWriter(string source)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));
		return new LogRecordWriter(this, source);
	}

	private static IEnumerable<ILogWriter> GetLogWriters(ILoggingParameters parameters)
	{
		var wrtrs = parameters.Select(o => o.CreateWriter()).ToList();
		if (System.Diagnostics.Debugger.IsLogging())
		{
			foreach (var itm in wrtrs)
			{
				SystemLog.WriteDebugMessage("Lexxys.LogRecordService", "Writer: " + itm.Name + " -> " + itm.Target);
			}
		}
		return wrtrs;
	}


	//public void Start()
	//{
	//	var listeners = _listeners;
	//	for (int i = 0; i < listeners.Length; i++)
	//		listeners[i]?.Start();
	//}

	//public void Stop(bool force = false)
	//{
	//	for (int i = 0; i < _listeners.Length; i++)
	//		_listeners[i]?.Stop(force);
	//}

	public void Stop(bool force = false)
	{
		var listeners = Interlocked.Exchange(ref _listeners, Array.Empty<ILogRecordQueueListener>());
		var pool = new List<Task>();
		for (int i = 0; i < listeners.Length; ++i)
		{
			var listener = listeners[i];
			if (listener != null && listener.IsStarted)
			{
				var stopper = new Task(() => listener.Stop(force));
				pool.Add(stopper);
				stopper.Start();
			}
		}
		Task.WaitAll(pool.ToArray());
	}

	public int LockLogging() => Interlocked.Increment(ref _lockDepth);

	public int UnlockLogging() => Interlocked.Decrement(ref _lockDepth);

	private bool Write(LogRecord? record, int version, int[]? indexes)
	{
		if (record is null || indexes == null || _lockDepth > 0)
			return true;

		var listeners = _listeners;
		if (version != _version)
			return false;

		for (int i = 0; i < indexes.Length; ++i)
		{
			listeners[indexes[i]]?.Write(record);
		}
		return true;
	}

	//	private static void Stopper(object? obj)
	//	{
	//		if (obj is LogRecordQueueListener listener)
	//		{
	//#if TraceFlushing
	//			long y = WatchTimer.Start();
	//			Console.WriteLine($"{listener.Writer.Name} ({listener.RecordCount} {Lingua.Plural("record", listener.RecordCount)})");
	//#endif
	//			listener.Stop(false);
	//#if TraceFlushing
	//			Console.WriteLine($"{listener.Writer.Name} finish: {WatchTimer.ToSeconds(WatchTimer.Stop(y))} sec.");
	//#endif
	//		}
	//	}

	private (int Version, int[]?[] Indexes) CollectIndexes(string? source)
	{
		var result = new int[]?[LogTypeCount];
		var indices = new List<int>();
		lock (this)
		{
			var listeners = _listeners;
			var version = _version;
			for (int type = 0; type < LogTypeCount; ++type)
			{
				indices.Clear();
				for (int i = 0; i < listeners.Length; ++i)
				{
					if (listeners[i].Writer.Accepts(source, (LogType)type))
						indices.Add(i);
				}
				result[type] = indices.Count == 0 ? null : indices.ToArray();
			}
			return (version, result);
		}
	}

	private void OnChanged() => Changed?.Invoke();

	class LogRecordWriter: ILogRecordWriter
	{
		//public static readonly ILogRecordWriter Empty = new LogRecordWriter(LogRecordService.Empty, new int[]?[(int)LogType.MaxValue]);

		private readonly LogRecordService _service;
		private readonly string _source;
		private int _version;
		private int[]?[]? _indexes;

		public LogRecordWriter(LogRecordService service, string source)
		{
			if (service is null)
				throw new ArgumentNullException(nameof(service));
			if (source is null)
				throw new ArgumentNullException(nameof(source));

			_service = service;
			service.Changed += Service_Changed;
			_source = source;
			_indexes = default;
		}

		public bool IsEnabled(LogType logType) => Indexes[(int)logType] != null;

		public void Write(LogRecord? record)
		{
			if (record == null)
				return;
			while (_indexes == null || !_service.Write(record, _version, _indexes[(int)record.LogType]))
				(_version, _indexes) = _service.CollectIndexes(_source);
		}

		private int[]?[] Indexes
		{
			get
			{
				if (_indexes == null)
					(_version, _indexes) = _service.CollectIndexes(_source);
				return _indexes;
			}
		}

		private void Service_Changed() => _indexes = default;
	}
}

interface ILogRecordQueueListener
{
	bool IsStarted { get; }
	bool QueueIsEmpty { get; }
	ILogWriter Writer { get; }
	void ClearBuffers();
	bool Start();
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
	private int _queueSize;


	public LogRecordQueueListener(ILogWriter writer, int? maxQuereSize = default, TimeSpan? flushTimeout = default)
	{
		_writer = writer ?? throw new ArgumentNullException(nameof(writer));
		_queue = new LogRecordQueue();
		_data = new AutoResetEvent(false);
		FlushTimeout = flushTimeout ?? LogRecordService.DefaultFlushTimeout;
		MaxQueueSize = maxQuereSize ?? LogRecordService.DefaultMaxQueueSize;
	}

	private int MaxQueueSize { get; }

	public TimeSpan FlushTimeout { get; }

	public ILogWriter Writer => _writer;

	public bool QueueIsEmpty => _queue.IsEmpty;

	public bool IsStarted => _thread != null;

	private bool IsStopping => _data == null;

#if TraceFlushing
	public int RecordCount => _queue.Count;
#endif

	public bool Start()
	{
		if (IsStarted || IsStopping)
			return false;

		_writer.Open();
		_thread = new Thread(Listen)
		{
			Name = "LOG:" + _writer.Name,
			IsBackground = true,
			Priority = ThreadPriority.Lowest
		};
		_thread.Start();
		return true;
	}

	public void ClearBuffers()
	{
		if (!IsStopping)
#if NETCOREAPP
			_queue.Clear();
#else
			_queue = new LogRecordQueue();
#endif
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
#if NETCOREAPP
			_queue.Clear();
#else
			Interlocked.Exchange(ref _queue, new LogRecordQueue());
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

				var stopped = (_thread!.ThreadState & (ThreadState.Stopped | ThreadState.Unstarted | ThreadState.Aborted)) != 0 || _thread!.Join(FlushTimeout);
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

	public void Write(LogRecord? record)
	{
		if (record == null || IsStopping)
			return;
		if (!IsStarted && !Start())
			return;

		_queue.Enqueue(record);
		EventWaitHandle? handle = _data;
		if (handle != null)
		{
			handle.Set();
			if (MaxQueueSize > 0 && Interlocked.Increment(ref _queueSize) > MaxQueueSize)
			{
				while (_queue.Count > MaxQueueSize)
					Thread.Sleep(5);
				_queueSize = 0;
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

