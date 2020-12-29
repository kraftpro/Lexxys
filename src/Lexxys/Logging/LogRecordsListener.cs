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
#pragma warning disable 0420

namespace Lexxys.Logging
{
	internal class LogRecordsListener
	{
		private static readonly LogRecordsListener Empty = new LogRecordsListener(0, Array.Empty<int>());
		private static readonly LogRecordsListener[] NoListeners = { Empty, Empty, Empty, Empty, Empty, Empty };

		private static volatile Listener[] _global = Array.Empty<Listener>();

		private readonly int[] _listener;
		private readonly int _hash;

		private LogRecordsListener(int hash, int[] index)
		{
			_hash = hash;
			_listener = index;
		}

		public void Write(LogRecord record)
		{
			Listener[] global = _global;
			if (_hash != global.GetHashCode())
				return;

			for (int i = 0; i < _listener.Length; ++i)
			{
				Listener temp = global[_listener[i]];
				if (temp != null)
					temp.Write(record);
			}
		}

		public static void Initialize(IEnumerable<LogWriter> writers)
		{
			if (writers == null)
				throw new ArgumentNullException(nameof(writers));
			Listener[] next = writers.Where(o => o != null && !o.Rule.IsEmpty).Select(o => new Listener(o)).ToArray();
			Listener[] prev = Interlocked.Exchange(ref _global, next);
			if (prev.Length > 0)
				DisposeListeners(prev);
		}

		private static void DisposeListeners(IEnumerable<Listener> listeners)
		{
			foreach (Listener listener in listeners)
			{
				listener.Dispose();
			}
		}

		public static LogRecordsListener[] Collect(string source)
		{
			Listener[] global = _global;
			var listeners = new LogRecordsListener[LoggingContext.LogTypeCount];
			for (LogType logType = 0; logType <= LogType.MaxValue; ++logType)
			{
				var indices = new List<int>();
				for (int i = 0; i < global.Length; ++i)
				{
					if (global[i].Writer.Rule.Contains(source, logType))
						indices.Add(i);
				}
				listeners[(int)logType] = indices.Count == 0 ? Empty : new LogRecordsListener(global.GetHashCode(), indices.ToArray());
			}
			return listeners;
		}

		public static LogRecordsListener[] SelectEmpty()
		{
			return NoListeners;
		}

		public static void ClearBuffers()
		{
			Listener[] global = _global;
			for (int i = 0; i < global.Length; ++i)
			{
				Listener temp = global[i];
				if (temp != null)
					temp.ClearBuffers();
			}
		}

		public static void FlushBuffers()
		{
			Listener[] global = _global;
			for (int i = 0; i < global.Length; ++i)
			{
				Listener temp = global[i];
				if (temp != null)
					temp.FLush();
			}
		}

		public static void StopAll(bool force = false)
		{
			Listener[] global = _global;
#if TraceFlushing
			long x = WatchTimer.Start();
			Console.WriteLine("Start flushing.");
#endif
			var pool = new List<Thread>(global.Length);
			lock (SyncRoot)
			{
				for (int i = 0; i < global.Length; ++i)
				{
					Listener temp = global[i];
					if (temp != null)
					{
						global[i] = null;
						if (force || temp.RecordCount == 0)
						{
							temp.ClearBuffers();
							temp.Stop(force);
						}
						else
						{
							var stopper = new Thread(Stopper);
							pool.Add(stopper);
							stopper.Start(temp);
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

		private static void Stopper(object obj)
		{
			var listener = (Listener)obj;
			if (listener != null)
			{
#if TraceFlushing
				long y = WatchTimer.Start();
				Console.WriteLine("{0} ({1} {2})", listener.Writer.Name, listener.RecordCount, Lingua.Plural("record", listener.RecordCount));
#endif
				listener.Stop(false);
#if TraceFlushing
				Console.WriteLine("{0} finish: {1} sec.", listener.Writer.Name, WatchTimer.ToSeconds(WatchTimer.Stop(y)));
#endif
			}
		}

		private readonly static object SyncRoot = new object();

		class Listener: IDisposable
		{
			//const int MinBatchSize = 1;
			//const int MaxBatchSize = 1024;
			//const int DefaultBatchSize = 4;
			private const string Source = "Lexxys.Logging.LogRecordsListenner";
			private readonly LogWriter _writer;
			private readonly int _batchSize;
			private readonly int _flushBound;
			private LogRecordQueue _queue;
			private volatile EventWaitHandle _data;
			private readonly Thread _thread;

			public Listener(LogWriter writer)
			{
				_writer = writer ?? throw new ArgumentNullException(nameof(writer));
				_batchSize = writer.BatchSize == 0 ? LoggingContext.DefaultBatchSize:
					writer.BatchSize <= LoggingContext.BatchSizeMin ? LoggingContext.BatchSizeMin:
					writer.BatchSize >= LoggingContext.BatchSizeMax ? LoggingContext.BatchSizeMax: writer.BatchSize;
				_queue = new LogRecordQueue();
				_flushBound = LoggingContext.FlushBoundMultiplier * (
					writer.FlushBound <= 0 ? LoggingContext.DefaultFlushBound:
					writer.FlushBound <= LoggingContext.FlushBoundMin ? LoggingContext.FlushBoundMin:
					writer.FlushBound >= LoggingContext.FlushBoundMax ? LoggingContext.FlushBoundMax: writer.FlushBound);
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

			public LogWriter Writer
			{
				get { return _writer; }
			}

			public int RecordCount
			{
				get { return _queue.Count; }
			}

			public void Dispose()
			{
				Stop(false);
			}

			private bool IsStopping
			{
				get { return _data == null; }
			}

			public void ClearBuffers()
			{
				if (IsStopping)
					return;
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
				using EventWaitHandle tmp = Interlocked.Exchange(ref _data, null);
				if (tmp != null)
				{
					if (!tmp.SafeWaitHandle.IsClosed)
						tmp.Set();
					Terminate(tmp, force);
				}
			}

			private void Terminate(EventWaitHandle data, bool force)
			{
				Thread.Sleep(0);

				if (force)
				{
					_queue = new LogRecordQueue();
					LogWriter.WriteEventLogMessage(Source, "Terminating...", LogRecord.Args("ThreadName", _thread.Name));
					if ((_thread.ThreadState & (ThreadState.Stopped | ThreadState.Unstarted | ThreadState.Aborted)) == 0)
						_thread.Abort();
				}
				else
				{
					try
					{
						if (!_queue.IsEmpty)
							_writer.Write(_queue);

						_writer.Write(new LogRecord(Source, "Exiting...", LogType.Trace, null));
						_writer.Close();
						if ((_thread.ThreadState & (ThreadState.Stopped | ThreadState.Unstarted | ThreadState.Aborted)) != 0)
							return;
						if (_thread.Join(LoggingContext.ListenerTurnSleep))
							return;
						LogWriter.WriteEventLogMessage(Source, "Waiting for working thread", LogRecord.Args("ThreadName", _thread.Name));
						if (_thread.Join(LoggingContext.LogoffTimeout))
							return;
						LogWriter.WriteErrorMessage(Source, "Thread join operation has been timed out", LogRecord.Args("time-out", LoggingContext.LogoffTimeout));
						_thread.Abort();
					}
					catch (Exception ex)
					{
						if (ex.IsCriticalException())
							throw;
					}
				}
				data.Dispose();
			}

			public void Write(LogRecord record)
			{
				if (IsStopping)
					return;
				_queue.Enqueue(record);
				EventWaitHandle tmp = _data;
				if (tmp != null && _queue.Count >= _batchSize)
				{
					tmp.Set();
					while (_queue.Count >= _flushBound)
						Thread.Sleep(5);
				}
			}


			private void Listen()
			{
				while (!IsStopping)
				{
					{
						EventWaitHandle tmp = _data;
						if (tmp == null)
							break;
						tmp.WaitOne(LoggingContext.ListenerPulseInterval);
					}
					if (IsStopping)
						return;

					while (!_queue.IsEmpty)
					{
						if (IsStopping)
							break;

						if (!_writer.IsReady)
						{
							Thread.Sleep(100);
							continue;
						}
						try
						{
							_writer.Write(_queue);
						}
						catch (Exception e)
						{
							LogWriter.WriteErrorMessage(String.Format("Internal error in Log Writer '{0}'", Writer.Name), e);
						}
						Thread.Sleep(LoggingContext.ListenerTurnSleep);
					}
				}
			}

			class LogRecordQueue: ConcurrentQueue<LogRecord>, IEnumerable<LogRecord>
			{
				IEnumerator<LogRecord> IEnumerable<LogRecord>.GetEnumerator()
				{
					while (TryDequeue(out LogRecord rec))
					{
						yield return rec;
					}
				}
			}
		}
	}
}


