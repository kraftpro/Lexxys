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
using System.Threading.Tasks;

namespace Lexxys.Logging;

class LogRecordService: ILoggingService
{
	private const int LogTypeCount = (int)LogType.MaxValue + 1;

	internal const int DefaultMaxQueueSize = 256 * 1024;
	internal static readonly TimeSpan DefaultFlushTimeout = TimeSpan.FromSeconds(5);
	private const int LogWatcherSleep = 500;

	//private const string LogSource = "Lexxys.Logging.LogRecordService";

	private volatile ILogRecordQueueListener[] _listeners;
	private volatile int _lockDepth;
	private volatile int _version;
	public event Action? Changed;

	public LogRecordService(ILoggingParameters parameters)
	{
		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		var listeners = new List<ILogRecordQueueListener>();
		foreach (var item in parameters)
		{
			if (item == null)
				continue;
			var wr = item.CreateWriter();
			listeners.Add(new LogRecordQueueListener(wr, item.MaxQueueSize, item.FlushTimeout));
		}
		_version = 1;
		_listeners = listeners.ToArray();
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
			Interlocked.Increment(ref _version);
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
			var array = writers.Where(o => o is not null).Select(o => (ILogRecordQueueListener)new LogRecordQueueListener(o!)).ToArray();
			Interlocked.Increment(ref _version);
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

	// private static IEnumerable<ILogWriter> GetLogWriters(ILoggingParameters parameters)
	// {
	// 	var wrtrs = parameters.Select(o => o.CreateWriter()).ToList();
	// 	if (System.Diagnostics.Debugger.IsLogging())
	// 	{
	// 		foreach (var itm in wrtrs)
	// 		{
	// 			SystemLog.WriteDebugMessage(LogSource, "Writer: " + itm.Name + " -> " + itm.Target);
	// 		}
	// 	}
	// 	return wrtrs;
	// }


	public void Stop(bool force = false)
	{
		var listeners = Interlocked.Exchange(ref _listeners, Array.Empty<ILogRecordQueueListener>());
		var pool = new List<Task>();
		for (int i = 0; i < listeners.Length; ++i)
		{
			var listener = listeners[i];
			if (listener is { IsStarted: true })
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
			listeners[indexes[i]].Write(record);
		}
		return true;
	}

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
	private const string LogSource = "Lexxys.Logging.LogRecordsListener";

	private readonly ILogWriter _writer;
	private LogRecordQueue _queue;
	private volatile EventWaitHandle? _data;
	private Thread? _thread;

	public LogRecordQueueListener(ILogWriter writer, int? maxQueueSize = default, TimeSpan? flushTimeout = default)
	{
		_writer = writer ?? throw new ArgumentNullException(nameof(writer));
		_queue = new LogRecordQueue();
		_data = new AutoResetEvent(false);
		FlushTimeout = flushTimeout ?? LogRecordService.DefaultFlushTimeout;
		MaxQueueSize = maxQueueSize ?? LogRecordService.DefaultMaxQueueSize;
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
			SystemLog.WriteEventLogMessage(LogSource, "Terminating '{_writer.Name}'...", LogRecord.Args("ThreadName", _thread!.Name));
			_writer.Write(new[] { new LogRecord(LogType.Warning, LogSource, "Terminating '{_writer.Name}'...", null) });
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
					SystemLog.WriteErrorMessage(LogSource, "Thread join operation for '{_writer.Name}' has been timed out", LogRecord.Args("time-out"));
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
				_writer.Write(new[] { new LogRecord(LogType.Trace, LogSource, "Exiting...", null) });
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
			if (MaxQueueSize > 0 && _queue.Count > MaxQueueSize)
			{
				do
				{
					Thread.Sleep(5);
				} while (_queue.Count > MaxQueueSize);
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
					SystemLog.WriteErrorMessage($"{LogSource}`{Writer.Name}", e);
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

