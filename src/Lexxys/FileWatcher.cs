// Lexxys Infrastructural library.
// file: FileWatcher.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
#if NETFRAMEWORK
using System.Security.Permissions;
#endif
using System.Threading;

namespace Lexxys
{
#if NETFRAMEWORK
	[SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
#endif
	public sealed class FileWatcher: IDisposable
	{
		private static Logger __log;
		private static readonly Dictionary<string, FileWatcher> __fileWatcher = new Dictionary<string, FileWatcher>(8);

		private readonly FileSystemWatcher _watcher;
		private FileInfo _fi;

		private FileWatcher(FileInfo fileInfo)
		{
			_fi = fileInfo ?? throw EX.ArgumentNull(nameof(fileInfo));
			_fi.Refresh();
			_watcher = new FileSystemWatcher(_fi.DirectoryName, _fi.Name)
			{
				IncludeSubdirectories = false,
				NotifyFilter = NotifyFilters.LastWrite,
				EnableRaisingEvents = true
			};

			_watcher.Changed += OnFileChanged;
			//_watcher.Created += OnFileChanged;
			//_watcher.Deleted += OnFileChanged;
			//_watcher.Renamed += OnFileChanged;
		}

		public event FileSystemEventHandler FileChanged;

		/// <summary>
		/// Add new file watcher.
		/// </summary>
		/// <param name="fileName">Path to file to monitor</param>
		/// <param name="watcher">Method that will hadle event of changing file</param>
#if NETFRAMEWORK
		[SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
#endif
		public static void AddFileWatcher(string fileName, FileSystemEventHandler watcher)
		{
			var fi = new FileInfo(fileName);
			lock (__fileWatcher)
			{
				if (!__fileWatcher.TryGetValue(fi.FullName, out var fw))
				{
					fw = new FileWatcher(fi);
					__fileWatcher[fi.FullName] = fw;
				}
				fw.FileChanged += watcher;
			}
		}

		public static void RemoveFileWatcher(string fileName, FileSystemEventHandler watcher)
		{
			var fi = new FileInfo(fileName);
			lock (__fileWatcher)
			{
				if (__fileWatcher.TryGetValue(fi.FullName, out var fw))
				{
					if (watcher == null)
						fw.FileChanged = null;
					else
						fw.FileChanged -= watcher;
					if (fw.FileChanged == null)
					{
						__fileWatcher.Remove(fi.FullName);
						fw.Dispose();
					}
				}
			}
		}

		private void OnFileChanged(object sender, FileSystemEventArgs e)
		{
			if (_fi.FullName != e.FullPath)
				return;
			var fn = new FileInfo(_fi.FullName);
			if (fn.Length == 0 || _fi.Length == fn.Length && _fi.LastWriteTime == fn.LastWriteTime)
				return;
			lock (__fileWatcher)
			{
				if (_fi.Length == fn.Length && _fi.LastWriteTime == fn.LastWriteTime)
					return;
				_fi = fn;

				Log.Info(SR.FileChanged(_fi.FullName));
				FileChanged?.Invoke(sender, e);
			}
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				_disposed = true;
				if (_watcher != null)
					_watcher.Dispose();
			}
		}
		private bool _disposed;

		private static Logger Log
		{
			get { return __log ??= new Logger("Lexxys.FileWatcher"); }
		}
	}
}
