// Lexxys Infrastructural library.
// file: BlobStorage.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using Lexxys;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// ReSharper disable ConditionIsAlwaysTrueOrFalse

#nullable enable

namespace Lexxys
{
	/// <summary>
	/// Directory structure generation mode
	/// </summary>
	public enum DirectoryGenerationMode
	{
		/// <summary>
		/// 1234567 -> "/34/12"
		/// </summary>
		Compatible,
		/// <summary>
		/// 1234567 -> "/12/34"
		/// </summary>
		BigEndian,
		/// <summary>
		/// 1234567 -> "/67/45"
		/// </summary>
		LittleEndian
	}

	/// <summary>
	/// Directory and file name generation configuration
	/// </summary>
	public class DirectoryStorageConfig
	{
		/// <summary>
		/// Default configuration
		/// </summary>
		public static readonly DirectoryStorageConfig Default = new DirectoryStorageConfig();

		/// <summary>
		/// Number of directories in subdirectories (default 100).
		/// </summary>
		public int DirectoryCount { get; }
		/// <summary>
		/// Directory string format (default "00").
		/// </summary>
		public string DirectoryFormat { get; }
		/// <summary>
		/// Number of files in directory (default 1000)
		/// </summary>
		public int FileCount { get; }
		/// <summary>
		/// File format string (default "{index:0000000}-{salt}{ext}")
		/// </summary>
		public string FileFormat { get; }
		/// <summary>
		/// Path separator (default Path.DirectorySeparatorChar)
		/// </summary>
		public char PathSeparator { get; }
		/// <summary>
		/// Temporary folder for files without index value (default ".t")
		/// </summary>
		public string TemporaryFolder { get; }
		/// <summary>
		/// Directory generation mode (see <see cref="DirectoryGenerationMode"/>)
		/// </summary>
		public DirectoryGenerationMode Mode { get; }

		/// <summary>
		/// Creates a new <see cref="DirectoryStorageConfig"/>
		/// </summary>
		/// <param name="directoryCount">Number of directories in subdirectories (default 100).</param>
		/// <param name="fileCount">Number of files in directory (default 1000)</param>
		/// <param name="directoryFormat">Directory string format (default "00").</param>
		/// <param name="fileFormat">File format string (default "{index:0000000}-{salt}{ext}")</param>
		/// <param name="pathSeparator">Path separator (default Path.DirectorySeparatorChar)</param>
		/// <param name="temporaryFolder">Temporary folder for files without index value (default ".t")</param>
		/// <param name="mode">Directory generation mode (see <see cref="DirectoryGenerationMode"/>)</param>
		public DirectoryStorageConfig(int directoryCount = default, int fileCount = default, string? directoryFormat = default, string? fileFormat = default, char pathSeparator = default, string? temporaryFolder = default, DirectoryGenerationMode mode = default)
		{
			DirectoryCount = directoryCount == 0 ? 100: directoryCount;
			DirectoryFormat = directoryFormat ?? new String('0', (directoryCount - 1).ToString().Length);
			FileCount = fileCount == 0 ? 1000: fileCount;
			FileFormat = fileFormat ?? "{index:0000000}-{salt}{ext}";
			PathSeparator = pathSeparator  == default ? Path.DirectorySeparatorChar: pathSeparator;
			TemporaryFolder = temporaryFolder?.Replace("\\", "").Replace("/", "").TrimToNull() ?? ".t";
			Mode = mode;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public class BlobStorage
	{
		private readonly Dictionary<string, List<IBlobStorageProvider>> _schemes = new Dictionary<string, List<IBlobStorageProvider>>();
		private readonly List<IBlobStorageProvider> _providers = new List<IBlobStorageProvider>();

		/// <summary>
		/// Get <see cref="IBlobStorageProvider"/> for the specified <paramref name="uri"/>.
		/// </summary>
		/// <param name="uri">Blob location</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public IBlobStorageProvider? GetProvider(string uri)
		{
			if (uri is not { Length: > 0 })
				throw new ArgumentNullException(nameof(uri));
		
			var (scheme, _) = SplitSchemeAndPath(uri);
			if (!_schemes.TryGetValue(scheme, out var providers))
				providers = _providers;

			return providers.FirstOrDefault(o => o.CanOpen(uri));
		}

		/// <summary>
		/// Registers a blob storage provider.
		/// </summary>
		/// <param name="provider">Blob storage provider</param>
		/// <exception cref="ArgumentNullException"><paramref name="provider"/> is null</exception>
		public void Register(IBlobStorageProvider provider)
		{
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));
			//_providers.Add(provider);

			if (provider.SupportedSchemes == null)
				return;

			foreach (var scheme in provider.SupportedSchemes)
			{
				if (scheme == null)
					continue;
				int i = scheme.IndexOf(':');
				var s = i < 0 ? scheme.Trim() : scheme.Substring(0, i).Trim();
				if (!_schemes.TryGetValue(s, out var list))
				{
					_schemes.Add(s, list = new List<IBlobStorageProvider>());
				}
				list.Add(provider);
			}
		}

		/// <summary>Returns "random" salt value based on system timer.</summary>
		public static long InitSalt(bool ordinal = default)
		{
			return ordinal ?
				DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond - 63520000000000:
				(long)(Rev64((ulong)DateTime.UtcNow.Ticks >> 5) >> 24 | 0x0100_0000_0000);

			static ulong Rev64(ulong value) => ((ulong)Rev32((uint)value) << 32) | Rev32((uint)(value >> 32));
			static uint Rev32(uint value) => (Rev16(value) << 16) | Rev16(value >> 16);
			//static uint Rev16(uint value) => (value & 0xFF) << 8 | ((value & 0xFF00) >> 8);
			static uint Rev16(uint value) => (Rev8(value) << 8) | Rev8(value >> 8);
			static uint Rev8(uint value) => ((value & 0x0F) << 4) | ((value & 0xF0) >> 4);
		}

		/// <summary>Increases the <paramref name="salt"/> value</summary>
		/// <param name="salt">The value to increase</param>
		public static long NextSalt(long salt) => salt + 1 + ((DateTime.UtcNow.Ticks >> 5) & 7);

		/// <summary>
		/// Creates a file path to be stored to a file system. Returns a file path in the form /dir/.../index-salt.ext, ie /12/34/1234567-a3dre.doc
		/// </summary>
		/// <param name="index">File index</param>
		/// <param name="salt">Random file salt</param>
		/// <param name="extension">file extension</param>
		/// <param name="config">FileStorage configuration or null</param>
		/// <returns></returns>
		public static string MakePath(int? index, long salt, string? extension, DirectoryStorageConfig? config = default)
		{
			config ??= DirectoryStorageConfig.Default;
			if (!String.IsNullOrEmpty(extension) && extension![0] != '.')
				extension = "." + extension;
			if (index.GetValueOrDefault() == 0)
				return $"{config.TemporaryFolder}{config.PathSeparator}{Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)}{extension}";
			string dir = GetDirectory(index.GetValueOrDefault(), config.DirectoryCount, config.DirectoryFormat, config.FileCount, config.PathSeparator, config.Mode); // /01/02/
			string format = config.FileFormat.Replace("{index", "{0").Replace("{salt", "{1").Replace("{ext", "{2");
			return dir + String.Format(format, index, Salt(salt), extension); // /01/02/3120201-salt.doc
		}

		/// <summary>
		/// Determines if the given <paramref name="path"/> is a path to store temporary data.  
		/// </summary>
		/// <param name="path">The path value to test.</param>
		/// <param name="config">Storage configuration</param>
		/// <returns></returns>
		public static bool IsTemporaryPath(string path, DirectoryStorageConfig? config = default)
		{
			config ??= DirectoryStorageConfig.Default;
			return path != null && path.StartsWith($"{config.TemporaryFolder}{config.PathSeparator}");
		}

		internal static (string Scheme, string Path) SplitSchemeAndPath(string uri)
		{
			int i = uri.IndexOf(':');
			if (i < 0 || i == 1 && Char.IsLetter(uri, 0))
				return ("", uri);
			var s = uri.Substring(0, i).Trim();
			var p = uri.Substring(i + 1);
			if (String.Equals(s, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase) && p.StartsWith("///"))
				p = p.Substring(3);
			else if (p.StartsWith("//", StringComparison.Ordinal))
				p = p.Substring(2);
			return (s, p.Trim().ToLowerInvariant());
		}
		
		private static string GetDirectory(int index, int directoryCount, string directoryFormat, int fileCount, char pathSeparator, DirectoryGenerationMode mode)
		{
			if (directoryCount < 0 || directoryCount > 1296)
				throw new ArgumentOutOfRangeException(nameof(directoryCount), directoryCount, null);
			var dir = new StringBuilder();
			if (mode == DirectoryGenerationMode.BigEndian)
			{
				// 1234567 -> "/12/34"
				int i = index / fileCount;
				while (i > 0)
				{
					dir.Insert(0, pathSeparator.ToString() + (i % directoryCount).ToString(directoryFormat));
					i /= directoryCount;
				}
			}
			else if (mode == DirectoryGenerationMode.LittleEndian)
			{
				// 1234567 -> "/67/45"
				int i = index;
				while (i > fileCount)
				{
					dir.Append(pathSeparator).Append((i % directoryCount).ToString(directoryFormat));
					i /= directoryCount;
				}
			}
			else // (mode == DirectoryGenerationMode.Compatible)
			{
				// 1234567 -> "/34/12"
				int i = index / fileCount;
				while (i > 0)
				{
					dir.Append(pathSeparator).Append((i % directoryCount).ToString(directoryFormat));
					i /= directoryCount;
				}
			}
			return dir.Append(pathSeparator).ToString();
		}

		private static string Salt(long salt)
		{
			string v = SixBitsCoder.Thirty((ulong)salt);
			return v.Length < 8 ? new string('0', 8 - v.Length) + v : v;
		}
	}
}


