// Lexxys Infrastructural library.
// file: ReadWriteSyncFat.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Lexxys
{
	public sealed class ReadWriteSyncFat: IReadWriteSync
	{
		private static ILogging Log => _log ??= new Logger("Lexxys.ReadWriteSyncFat");
		private static ILogging _log;
		private readonly ReaderWriterLock _locker;
		public const int DefaultTimingThreshold = 100;
		public const int DefaultLockTimeout = 5 * 60 * 1000;

		public static readonly ReadWriteSyncFat Null = new ReadWriteSyncFat(null);

		public ReadWriteSyncFat()
		{
			_locker = new ReaderWriterLock();
		}

		private ReadWriteSyncFat(ReaderWriterLock locker)
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
			if (_locker == null || _locker.IsReaderLockHeld || _locker.IsWriterLockHeld)
				return null;
			if (timingThreshold < 0)
				throw EX.ArgumentOutOfRange("logTiming", timingThreshold);

			if (timingThreshold == 0)
				return new NewReader(_locker, timeout);

			if (source == null)
				source = "ReadWriteSync.LockRead";
			else
				source += ".LockRead";

			using (Log.DebugTiming(source, new TimeSpan(timingThreshold * TimeSpan.TicksPerMillisecond)))
			{
				return new NewReader(_locker, timeout);
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
			if (_locker == null || _locker.IsWriterLockHeld)
				return null;
			if (timingThreshold < 0)
				throw EX.ArgumentOutOfRange("logTiming", timingThreshold);

			if (timingThreshold == 0)
				return _locker.IsReaderLockHeld ?
					(IDisposable)new UpgradeToWriter(_locker, timeout) :
					(IDisposable)new NewWriter(_locker, timeout);

			if (source == null)
				source = "ReadWriteSync.LockWrite";
			else
				source += ".LockWrite";

			using (Log.DebugTiming(source, new TimeSpan(timingThreshold * TimeSpan.TicksPerMillisecond)))
			{
				return _locker.IsReaderLockHeld ?
					(IDisposable)new UpgradeToWriter(_locker, timeout) :
					(IDisposable)new NewWriter(_locker, timeout);
			}
		}

		public bool IsReaderLockHeld
		{
			get { return _locker != null && _locker.IsReaderLockHeld; }
		}
		public bool IsWriterLockHeld
		{
			get { return _locker != null && _locker.IsWriterLockHeld; }
		}
		#endregion

		#region IDisposable Members

		public void Dispose()
		{
		}
		#endregion

		private class NewReader: IDisposable
		{
			ReaderWriterLock _locker;

			public NewReader(ReaderWriterLock locker, int timeout)
			{
				_locker = locker;
				_locker.AcquireReaderLock(timeout);
			}

			public void Dispose()
			{
				ReaderWriterLock tmp = Interlocked.Exchange(ref _locker, null);
				if (tmp != null)
					tmp.ReleaseReaderLock();
			}
		}

		private class NewWriter: IDisposable
		{
			ReaderWriterLock _locker;

			public NewWriter(ReaderWriterLock locker, int timeout)
			{
				_locker = locker;
				_locker.AcquireWriterLock(timeout);
			}

			public void Dispose()
			{
				ReaderWriterLock tmp = Interlocked.Exchange(ref _locker, null);
				if (tmp != null)
					tmp.ReleaseWriterLock();
			}
		}

		private class UpgradeToWriter: IDisposable
		{
			ReaderWriterLock _locker;
			LockCookie _cookie;

			public UpgradeToWriter(ReaderWriterLock locker, int timeout)
			{
				_locker = locker;
				_cookie = _locker.UpgradeToWriterLock(timeout);
			}

			public void Dispose()
			{
				ReaderWriterLock tmp = Interlocked.Exchange(ref _locker, null);
				if (tmp != null)
					tmp.DowngradeFromWriterLock(ref _cookie);
			}
		}
	}
}
