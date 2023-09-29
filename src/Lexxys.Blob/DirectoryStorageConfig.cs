// Lexxys Infrastructural library.
// file: BlobStorage.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Globalization;
using System.Text;

namespace Lexxys;

/// <summary>
/// Directory and file name generation configuration
/// </summary>
public class DirectoryStorageConfig
{
	private const string DefaultTemporaryFolder = ".t";

	/// <summary>
	/// Default configuration
	/// </summary>
	public static readonly DirectoryStorageConfig Default = new DirectoryStorageConfig();

	/// <summary>
	/// Number of directories in subdirectories (default 100).
	/// </summary>
	public int DirectoryCount { get; }
	/// <summary>
	/// Number of files in directory (default 1000)
	/// </summary>
	public int FileCount { get; }
	/// <summary>
	/// Path separator (default Path.DirectorySeparatorChar)
	/// </summary>
	public char PathSeparator { get; }
	/// <summary>
	/// Temporary folder for files without index value (default "/.t")
	/// </summary>
	public string TemporaryFolder { get; }
	/// <summary>
	/// Directory generation mode (see <see cref="DirectoryGenerationMode"/>)
	/// </summary>
	public DirectoryGenerationMode Mode { get; }

	private readonly Fmt _format;

	/// <summary>
	/// Creates a new <see cref="DirectoryStorageConfig"/>
	/// </summary>
	/// <param name="directoryCount">Number of directories in subdirectories (default 100).</param>
	/// <param name="fileCount">Number of files in directory (default 1000)</param>
	/// <param name="radix">Number base to convert index to string. Supports: 8, 10, 16, 32, or 36</param>
	/// <param name="flat">Flat file structure</param>
	/// <param name="pathSeparator">Path separator (default Path.DirectorySeparatorChar)</param>
	/// <param name="temporaryFolder">Temporary folder for files without index value (default "/.t")</param>
	/// <param name="mode">Directory generation mode (see <see cref="DirectoryGenerationMode"/>)</param>
	public DirectoryStorageConfig(int directoryCount = default, int fileCount = default, int radix = default, bool flat = default, char pathSeparator = default, string? temporaryFolder = default, DirectoryGenerationMode mode = default)
	{
		if (directoryCount < 0)
			throw new ArgumentOutOfRangeException(nameof(directoryCount), directoryCount, null);
		if (fileCount < 0)
			throw new ArgumentOutOfRangeException(nameof(fileCount), fileCount, null);
		if (radix is not (0 or 8 or 10 or 16 or 32 or 36))
			throw new ArgumentOutOfRangeException(nameof(radix), radix, null);

		if (flat)
		{
			DirectoryCount = 0;
			FileCount = 0;
			if (radix == 0)
				radix = 10;
		}
		else
		{
			if (directoryCount == 0 && fileCount == 0)
			{
				if (radix == 0)
					radix = 10;
				DirectoryCount = radix * radix;
				FileCount = DirectoryCount * radix;
			}
			else
			{
				if (radix == 0)
					radix = DefaultRadix(directoryCount > 0 ? directoryCount: fileCount);

				if (fileCount > 0 && fileCount < radix)
					throw new ArgumentOutOfRangeException(nameof(fileCount), fileCount, null);

				DirectoryCount = directoryCount > 0 ? directoryCount : fileCount / radix;
				FileCount = fileCount > 0 ? fileCount : directoryCount * radix;
			}
		}

		PathSeparator = pathSeparator == default ? Path.DirectorySeparatorChar : pathSeparator;
		var tmp = FixTempDir(temporaryFolder, PathSeparator);
		TemporaryFolder = tmp[0] == PathSeparator ? tmp : PathSeparator + tmp;
		Mode = mode;
		_format = new Fmt(radix, DirectoryCount);

		static string FixTempDir(string? temporary, char separator)
		{
			if (temporary == null) return DefaultTemporaryFolder;
			if (separator != '\\')
				temporary = temporary.Replace('\\', separator);
			if (separator != '/')
				temporary = temporary.Replace('/', separator);
			return temporary.TrimEnd().TrimEnd(separator).TrimToNull() ?? DefaultTemporaryFolder;
		}

		static int DefaultRadix(int count) => count switch
		{
			8 => 8,
			8*8 => 8,
			8*8*8 => 8,
			10 => 10,
			10*10 => 10,
			10*10*10 => 10,
			10*10*10*10 => 10,
			16 => 16,
			16*16 => 16,
			16*16*16 => 16,
			16*16*16*16 => 16,
			32 => 32,
			32*32 => 32,
			32*32*32 => 32,
			32*32*32*32 => 32,
			36 => 36,
			36*36 => 36,
			36*36*36 => 36,
			36*36*36*36 => 36,
			_ => count % 16 == 0 ? 16: count % 36 == 0 ? 36: 10
		};
	}

	/// <summary>
	/// Determines if the given <paramref name="path"/> is a path to store temporary data.  
	/// </summary>
	/// <param name="path">The path value to test.</param>
	/// <returns></returns>
	public bool IsTemporaryPath(string? path)
		=> path != null && path.StartsWith($"{TemporaryFolder}{PathSeparator}", StringComparison.Ordinal);

	/// <summary>
	/// Creates a file path to be stored to a file system. Returns a file path in the form: /dir/.../index-salt.ext, ie /12/34/1234567-a3dre.doc
	/// </summary>
	/// <param name="index">File index</param>
	/// <param name="salt">Random file salt</param>
	/// <param name="extension">file extension</param>
	/// <returns></returns>
	public string MakePath(long? index, long salt, string? extension)
	{
		if (extension is { Length: >0 } && extension[0] != '.')
			extension = "." + extension;
		if (index.GetValueOrDefault() <= 0)
			return $"{TemporaryFolder}{PathSeparator}{Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)}{extension}";
		var path = new StringBuilder();
		if (FileCount > 0)
			AppendDirectory(path, (ulong)index.GetValueOrDefault(), (uint)DirectoryCount, (uint)FileCount, _format, PathSeparator, Mode); // /01/02/
		else
			path.Append(PathSeparator);

		path.Append(_format.FormatName((ulong)index.GetValueOrDefault()));
		if (salt > 0)
			path.Append('-').Append(SixBitsCoder.Thirty((ulong)salt).PadLeft(5, '0'));
		path.Append(extension);
		return path.ToString();
	}

	private class Fmt
	{
		private readonly int _partWidth;
		private readonly int _nameWidth;
		private readonly uint _radix;
		
		public Fmt(int radix, int count)
		{
			_radix = (uint)radix;
			_partWidth = (int)Math.Ceiling(Math.Log(count - 1, radix));
			_nameWidth = radix switch { < 10 => 8, < 16 => 7, < 32 => 6, _ => 5 };
		}

		public string FormatPart(ulong value) => Format(value, _radix, _partWidth);

		public string FormatName(ulong value) => Format(value, _radix, _nameWidth);

		private static unsafe string Format(ulong value, uint radix, int width)
		{
			if (value == 0)
				return new string('0', width);

			var array = stackalloc char[64];
			int i = 64;
			while (value > 0)
			{
				array[--i] = __digits[value % radix];
				value /= radix;
			}
			while (64 - i < width)
			{
				array[--i] = '0';
			}
			return new string(array, i, 64 - i);
		}
		private static readonly char[] __digits = { '0','1','2','3','4','5','6','7','8','9','a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z' };
	}

	/// <summary>Returns "random" salt value based on system timer.</summary>
	public static long InitSalt(bool ordinal = default)
	{
		return ordinal ?
			DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond - 63100000000000:
			(long)((Swap64((ulong)WatchTimer.Start()) >> 24 ^ (ulong)WatchTimer.Start()) & 0x01FF_FFFF_FFFF);

		static ulong Swap64(ulong value)
		{
			var x = value << 32 | value >> 32;
			x = (x & 0xFFFF0000_FFFF0000) >> 16 | (x & 0x0000FFFF_0000FFFF) << 16;
			x = (x & 0xFF00FF00_FF00FF00) >> 8 | (x & 0x00FF00FF_00FF00FF) << 8;
			return x;
		}
	}

	/// <summary>Increases the <paramref name="salt"/> value</summary>
	/// <param name="salt">The value to increase</param>
	public static long NextSalt(long salt) => salt + 1 + (WatchTimer.Query(0) & 7);

	private static unsafe void AppendDirectory(StringBuilder path, ulong index, uint directoryCount, uint fileCount, Fmt format, char pathSeparator, DirectoryGenerationMode mode)
	{
		if (mode == DirectoryGenerationMode.BigEndian)
		{
			var stack = stackalloc ulong[32];
			// 1234567 -> "/12/34"
			var k = index / fileCount;
			int i = 0;
			while (k > 0)
			{
				stack[i++] = k % directoryCount;
				k /= directoryCount;
			}
			while (i > 0)
			{
				path.Append(pathSeparator).Append(format.FormatPart(stack[--i]));
			}
		}
		else if (mode == DirectoryGenerationMode.LittleEndian)
		{
			// 1234567 -> "/67/45"
			var i = index;
			while (i > fileCount)
			{
				path.Append(pathSeparator).Append(format.FormatPart(i % directoryCount));
				i /= directoryCount;
			}
		}
		else // (mode == DirectoryGenerationMode.Compatible)
		{
			// 1234567 -> "/34/12"
			var i = index / fileCount;
			while (i > 0)
			{
				path.Append(pathSeparator).Append(format.FormatPart(i % directoryCount));
				i /= directoryCount;
			}
		}
		path.Append(pathSeparator);
	}
}


