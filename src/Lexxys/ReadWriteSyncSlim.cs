// Lexxys Infrastructural library.
// file: ReadWriteSyncSlim.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Threading;

namespace Lexxys
{
	public sealed class ReadWriteSyncSlim: IReadWriteSync
	{
		private static ILogging Log => _log ??= StaticServices.Create<ILogging>("Lexxys.ReadWriteSyncSlim");
		private static ILogging _log;
		private ReaderWriterLockSlim _locker;
		public const int DefaultTimingThreshold = 100;
		public const int DefaultLockTimeout = 5 * 60 * 1000;

		public static readonly ReadWriteSyncSlim Null = new ReadWriteSyncSlim(null);


		public ReadWriteSyncSlim()
		{
			_locker = new ReaderWriterLockSlim();
		}

		private ReadWriteSyncSlim(ReaderWriterLockSlim locker)
		{
			_locker = locker;
		}

		#region IReadWriteSync Members
		public IDisposable Read()
		{
			return Read(DefaultLockTimeout, null, DefaultTimingThreshold);
		}
		public IDisposable Read(int timeout)
		{
			return Read(timeout, null, DefaultTimingThreshold);
		}
		public IDisposable Read(string source)
		{
			return Read(DefaultLockTimeout, source, DefaultTimingThreshold);
		}
		public IDisposable Read(int timeout, string source)
		{
			return Read(timeout, source, DefaultTimingThreshold);
		}
		public IDisposable Read(int timeout, string source, int timingThreshold)
		{
			if (timingThreshold < 0)
				throw EX.ArgumentOutOfRange("logTiming", timingThreshold);

			ReaderWriterLockSlim locker = _locker;
			if (locker == null || locker.IsReadLockHeld || locker.IsWriteLockHeld)
				return null;
			if (timingThreshold == 0)
				return new NewReader(locker, timeout);

			if (source == null)
				source = "ReadWriteSync.LockRead";
			else
				source += ".LockRead";

			using (Log.DebugTiming(source, new TimeSpan(timingThreshold * TimeSpan.TicksPerMillisecond)))
			{
				return new NewReader(locker, timeout);
			}
		}

		public IDisposable Write()
		{
			return Write(DefaultLockTimeout, null, DefaultTimingThreshold);
		}
		public IDisposable Write(int timeout)
		{
			return Write(timeout, null, DefaultTimingThreshold);
		}
		public IDisposable Write(string source)
		{
			return Write(DefaultLockTimeout, source, DefaultTimingThreshold);
		}
		public IDisposable Write(int timeout, string source)
		{
			return Write(timeout, source, DefaultTimingThreshold);
		}
		public IDisposable Write(int timeout, string source, int timingThreshold)
		{
			if (timingThreshold < 0)
				throw EX.ArgumentOutOfRange("logTiming", timingThreshold);

			ReaderWriterLockSlim locker = _locker;
			if (locker == null || locker.IsWriteLockHeld)
				return null;
			if (timingThreshold == 0)
				return new NewWriter(locker, timeout);

			if (source == null)
				source = "ReadWriteSync.LockWrite";
			else
				source += ".LockWrite";

			using (Log.DebugTiming(source, new TimeSpan(timingThreshold * TimeSpan.TicksPerMillisecond)))
			{
				return new NewWriter(locker, timeout);
			}
		}

		public bool IsReaderLockHeld
		{
			get { return _locker != null && _locker.IsReadLockHeld; }
		}
		public bool IsWriterLockHeld
		{
			get { return _locker != null && _locker.IsWriteLockHeld; }
		}
		#endregion

		#region IDisposable Members
		public void Dispose()
		{
			ReaderWriterLockSlim temp = Interlocked.Exchange(ref _locker, null);
			if (temp != null)
			{
				temp.Dispose();
			}
		}
		#endregion

		private class NewReader: IDisposable
		{
			ReaderWriterLockSlim _locker;

			public NewReader(ReaderWriterLockSlim locker, int timeout)
			{
				_locker = locker;
				if (!_locker.TryEnterReadLock(timeout))
					throw EX.Unexpected(SR.LockTimeout(timeout));
			}

			public void Dispose()
			{
				ReaderWriterLockSlim tmp = Interlocked.Exchange(ref _locker, null);
				if (tmp != null)
					tmp.ExitReadLock();
			}
		}

		private class NewWriter: IDisposable
		{
			ReaderWriterLockSlim _locker;
			readonly bool _upgrade;

			public NewWriter(ReaderWriterLockSlim locker, int timeout)
			{
				_locker = locker;
				_upgrade = _locker.IsReadLockHeld;
				if (_upgrade)
					_locker.ExitReadLock();
				if (!_locker.TryEnterWriteLock(timeout))
					throw EX.Unexpected(SR.LockTimeout(timeout));
			}

			public void Dispose()
			{
				ReaderWriterLockSlim tmp = Interlocked.Exchange(ref _locker, null);
				if (tmp != null)
				{
					tmp.ExitWriteLock();
					if (_upgrade)
						tmp.EnterReadLock();
				}
			}
		}
	}
}


