// Lexxys Infrastructural library.
// file: FileWatcher.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.IO;
#if !NETCOREAPP
using System.Security.Permissions;
#endif
using Microsoft.Extensions.Logging;

namespace Lexxys
{
#if !NETCOREAPP
	[SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
#endif
	public sealed class FileWatcher: IDisposable
	{
		private const string LogSource = "Lexxys.FileWatcher";
		private static ILogger? __log;
		private static readonly Dictionary<string, FileWatcher> __fileWatchersMap = new Dictionary<string, FileWatcher>();

		private readonly FileSystemWatcher _watcher;
		private WatchingFileInfo _watching;

		readonly struct WatchingFileInfo
		{
			private readonly string _fullName;
			private readonly int _length;
			private readonly DateTime _lastWriteTime;

			public WatchingFileInfo(FileInfo fileInfo)
            {
				_fullName = fileInfo.FullName;
				_length = (int)fileInfo.Length;
				_lastWriteTime = fileInfo.LastWriteTimeUtc;
            }

			public bool IsWatched(string? fullName) => fullName == _fullName;

			public bool Ignore(FileInfo fi) => fi.Length == 0 || fi.Length == _length && fi.LastWriteTimeUtc == _lastWriteTime;
		}

		private FileWatcher(FileInfo fileInfo)
		{
			if (fileInfo is null)
				throw new ArgumentNullException(nameof(fileInfo));
			fileInfo.Refresh();
			if (fileInfo.DirectoryName is null)
				throw new ArgumentOutOfRangeException(nameof(fileInfo));
			_watching = new WatchingFileInfo(fileInfo);
			_watcher = new FileSystemWatcher(fileInfo.DirectoryName, fileInfo.Name)
			{
				IncludeSubdirectories = false,
				NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
				EnableRaisingEvents = true
			};

			_watcher.Changed += OnFileChanged;
			_watcher.Created += OnFileChanged;
			_watcher.Renamed += OnFileChanged;
		}

		public event FileSystemEventHandler? FileChanged;

		/// <summary>
		/// Add new file watcher.
		/// </summary>
		/// <param name="fileName">Path to file to monitor</param>
		/// <param name="watcher">Method that will handle event of changing file</param>
#if !NETCOREAPP
		[SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
#endif
		public static void AddFileWatcher(string fileName, FileSystemEventHandler watcher)
		{
			var fi = new FileInfo(fileName);
			lock (__fileWatchersMap)
			{
				if (!__fileWatchersMap.TryGetValue(fi.FullName, out var fw))
				{
					fw = new FileWatcher(fi);
					__fileWatchersMap[fi.FullName] = fw;
				}
				fw.FileChanged += watcher;
			}
		}

		public static void RemoveFileWatcher(string fileName, FileSystemEventHandler? watcher = null)
		{
			lock (__fileWatchersMap)
			{
				var fi = new FileInfo(fileName);
				if (!__fileWatchersMap.TryGetValue(fi.FullName, out var fw)) return;

				if (watcher == null)
					fw.FileChanged = null;
				else
					fw.FileChanged -= watcher;
				if (fw.FileChanged is null) return;

				__fileWatchersMap.Remove(fi.FullName);
				fw.Dispose();
			}
		}

		private void OnFileChanged(object sender, FileSystemEventArgs e)
		{
			if (!_watching.IsWatched(e.FullPath))
				return;
			
			var fn = new FileInfo(e.FullPath);
			if (_watching.Ignore(fn)) return;

			lock (__fileWatchersMap)
			{
				fn.Refresh();
				if (_watching.Ignore(fn)) return;
				
				_watching = new WatchingFileInfo(fn);
				Log?.Info(SR.FileChanged(e.FullPath));
				FileChanged?.Invoke(sender, e);
			}
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;
				_watcher.Dispose();
			}
		}
		private bool _disposed;

		private static ILogger? Log => __log ??= Statics.TryGetLogger(LogSource);
	}
}
