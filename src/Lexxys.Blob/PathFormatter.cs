using Microsoft.Extensions.Options;

using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Lexxys;

/// <summary>
/// Provides functionality for formatting file and directory names using a configurable, sharded path structure.
/// Supports generating unique names and paths for files based on identifiers, metadata, and directory organization
/// settings.
/// </summary>
/// <remarks>The <see cref="PathFormatter"/> class enables flexible organization of files in hierarchical directory structures,
/// which is useful for scenarios involving large numbers of files or sharding strategies. It allows customization of
/// directory and file counts, path separators, temporary folder handling, and directory generation modes. The formatter
/// can generate both flat and multi-level directory structures and supports metadata inclusion in generated names.
/// Thread safety is not guaranteed for instance members.</remarks>
public class PathFormatter: IBlobNameFormatter
{
	private const string DefaultTemporaryFolder = ".t";
	private const int DefaultRadix = 10;

	/// <summary>
	/// Default configuration
	/// </summary>
	public static readonly PathFormatter Default = new PathFormatter();

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

	public PathFormatter(IOptions<PathFormatterOptions>? options): this(
		options?.Value.DirectoryCount ?? default,
		options?.Value.FileCount ?? default,
		options?.Value.Radix ?? default,
		options?.Value.Flat ?? default,
		options?.Value.PathSeparator ?? default,
		options?.Value.TemporaryFolder,
		options?.Value.Mode ?? default)
	{		
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PathFormatter"/> class, configuring directory and file structure
	/// formatting for file names based on the specified parameters.
	/// </summary>
	/// <remarks>If both <paramref name="directoryCount"/> and <paramref name="fileCount"/> are set to 0, default
	/// values are inferred based on the radix. The <paramref name="flat"/> parameter overrides directory and file counts,
	/// resulting in a single-level structure. The <paramref name="temporaryFolder"/> path is normalized to use the
	/// specified <paramref name="pathSeparator"/>. This constructor allows flexible configuration for file name formatting
	/// in scenarios such as sharding or organizing large numbers of files.</remarks>
	/// <param name="directoryCount">The number of directories to use in the generated subdirectory structure. Must be non-negative. If set to 0, the
	/// value is inferred from other parameters.</param>
	/// <param name="fileCount">The number of files to place in each directory. Must be non-negative and at least equal to the radix if specified.
	/// If set to 0, the value is inferred from other parameters.</param>
	/// <param name="radix">The numeric base used to convert file and directory indices to strings. Supported values are 10 (decimal), 16
	/// (hexadecimal), or 36 (alphanumeric). If set to 0, the radix is determined automatically.</param>
	/// <param name="flat">Indicates whether to use a flat file structure without subdirectories. If <see langword="true"/>, all files are
	/// placed in a single directory.</param>
	/// <param name="pathSeparator">The character used to separate directory levels in generated paths. Defaults to <see
	/// cref="System.IO.Path.DirectorySeparatorChar"/> if not specified.</param>
	/// <param name="temporaryFolder">The path to the temporary folder used for files that do not have an index value. If null or empty, defaults to
	/// "/.t".</param>
	/// <param name="mode">The directory generation mode that determines how directories are created. See <see
	/// cref="DirectoryGenerationMode"/> for available options.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="directoryCount"/> or <paramref name="fileCount"/> is negative, or when <paramref
	/// name="radix"/> is not 0, 10, 16, or 36, or when <paramref name="fileCount"/> is less than the specified radix.</exception>
	public PathFormatter(int directoryCount = default, int fileCount = default, int radix = default, bool flat = default, char pathSeparator = default, string? temporaryFolder = default, DirectoryGenerationMode mode = default)
	{
		if (directoryCount < 0)
			throw new ArgumentOutOfRangeException(nameof(directoryCount), directoryCount, null);
		if (fileCount < 0)
			throw new ArgumentOutOfRangeException(nameof(fileCount), fileCount, null);
		if (radix is not (0 or 10 or 16 or 36))
			throw new ArgumentOutOfRangeException(nameof(radix), radix, null);

		if (flat)
		{
			DirectoryCount = 0;
			FileCount = 0;
			if (radix == 0)
				radix = DefaultRadix;
		}
		else if (directoryCount == 0 && fileCount == 0)
		{
			if (radix == 0)
				radix = DefaultRadix;
			DirectoryCount = radix * radix;
			FileCount = DirectoryCount * radix;
		}
		else
		{
			if (radix == 0)
				radix = GetRadixFromCount(directoryCount > 0 ? directoryCount: fileCount);

			if (fileCount > 0 && fileCount < radix)
				throw new ArgumentOutOfRangeException(nameof(fileCount), fileCount, null);

			DirectoryCount = directoryCount > 0 ? directoryCount: fileCount / radix;
			FileCount = fileCount > 0 ? fileCount: directoryCount * radix;
		}

		PathSeparator = pathSeparator == default ? Path.DirectorySeparatorChar: pathSeparator;
		var tmp = FixTempDir(temporaryFolder, PathSeparator);
		TemporaryFolder = tmp[0] == PathSeparator ? tmp: PathSeparator + tmp;
		Mode = mode;
		_format = new Fmt(radix, DirectoryCount);

		static string FixTempDir(string? temporary, char separator)
		{
			if (temporary == null) return DefaultTemporaryFolder;
			if (separator != '\\')
				temporary = temporary.Replace('\\', separator);
			if (separator != '/')
				temporary = temporary.Replace('/', separator);
			temporary = temporary.TrimStart().TrimStart(separator).Trim();
			return temporary.Length > 0 ? temporary: DefaultTemporaryFolder;
		}

		static int GetRadixFromCount(int count) => count % 16 == 0 ? 16: count % 36 == 0 ? 36: DefaultRadix;
	}

	/// <summary>
	/// Determines whether the specified <paramref name="path"/> refers to a temporary folder managed by this instance.
	/// </summary>
	/// <param name="path">The file system path to evaluate. Can be null.</param>
	/// <returns>true if the path starts with the temporary folder prefix; otherwise, false.</returns>
	public bool IsTemporaryPath(string? path)
		=> path != null && path.StartsWith($"{TemporaryFolder}{PathSeparator}", StringComparison.Ordinal);


	/// <summary>
	/// Generates a unique name string based on the specified identifier, type, and optional metadata.
	/// </summary>
	/// <remarks>If the identifier is null or a valid long integer, the name is generated using a salted path
	/// format. Otherwise, the name is constructed by concatenating the identifier, metadata, and type. The resulting
	/// string is suitable for use as a unique key or resource name.</remarks>
	/// <param name="id">The base identifier used to construct the name. If the value can be parsed as a long integer, it is treated as an
	/// index; otherwise, it is used as a string. Can be null.</param>
	/// <param name="type">An optional type suffix to append to the name. If provided, it is added after a period separator. Can be null or
	/// empty.</param>
	/// <param name="metadata">An optional collection of key-value pairs to include in the name. Each non-null entry is appended in the format
	/// '-key_value', ordered by key. Can be null.</param>
	/// <returns>A string representing the constructed name, incorporating the identifier, type, and metadata as specified.</returns>
	public string CreateName(string? id, string? type = null, IDictionary<string, object?>? metadata = null)
	{
		if (type is { Length: > 0 })
			if (type[0] != '.')
				type = "." + type;
			else if (type.Length == 1)
				type = String.Empty;
		if (id is null)
			return $"{TemporaryFolder}{PathSeparator}{Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)}{type}";

		var path = new StringBuilder();
		if (Int64.TryParse(id, out var index))
		{
			AppendDirectory(path, index, (uint)DirectoryCount, (uint)FileCount, _format, PathSeparator, Mode)
				.Append(_format.FormatName(index));
		}
		else
		{
			path.Append(id);
		}
		if (metadata != null)
		{
			foreach (var kvp in metadata.OrderBy(k => k.Key, StringComparer.Ordinal))
			{
				if (kvp.Value != null)
					path.Append('-').Append(kvp.Key).Append('_').Append(kvp.Value.ToString());
			}
		}
		return path.Append(type).ToString();
	}

	/// <summary>
	/// Generates a file path string based on the specified index, salt, and file extension.
	/// </summary>
	/// <remarks>If the index is not specified or is less than or equal to zero, the method returns a path in the
	/// temporary folder with a randomly generated name. When a valid index is provided, the path includes directory
	/// segments and a formatted file name based on the index and salt. The extension is normalized to ensure it starts
	/// with a period.</remarks>
	/// <param name="index">The optional index value used to determine the directory structure and file name. If the value is null or less than
	/// or equal to zero, a temporary path with a unique identifier is generated.</param>
	/// <param name="salt">An additional numeric value appended to the file name for uniqueness or differentiation. If greater than zero, it
	/// is encoded and included in the path.</param>
	/// <param name="extension">The file extension to append to the generated path. If not null or empty and does not start with a period ('.'), a
	/// period is automatically prepended.</param>
	/// <param name="metadata">An optional collection of metadata key-value pairs to include in the file name. Each key-value pair is appended to the file name</param>
	/// <returns>A string representing the constructed file path, including directory structure, file name, salt (if provided), and
	/// extension.</returns>
	public StringBuilder MakePath(long index, long salt, IDictionary<string, object?>? metadata = null)
	{
		var path = AppendDirectory(new StringBuilder(), index, (uint)DirectoryCount, (uint)FileCount, _format, PathSeparator, Mode)
			.Append(_format.FormatName(index));

		if (metadata != null)
		{
			foreach (var kvp in metadata.OrderBy(k => k.Key, StringComparer.Ordinal))
			{
				if (kvp.Value != null)
					path.Append('-').Append(kvp.Key).Append('_').Append(kvp.Value.ToString());
			}
		}
		return salt == 0 ? path : path.Append('-').Append(Fmt.Format(salt, 36, 5));
	}

	private class Fmt
	{
		private readonly int _partWidth;
		private readonly int _nameWidth;
		private readonly uint _radix;

		public Fmt(int radix, int count)
		{
			if (radix is not (10 or 16 or 36))
				throw new ArgumentOutOfRangeException(nameof(radix), radix, null);

			_radix = (uint)radix;
			_partWidth = (int)Math.Ceiling(Math.Log(count - 1, radix));
			_nameWidth = radix switch { 10 => 8, 16 => 7, _ => 5 };
		}

		public string FormatPart(long value) => Format(value, _radix, _partWidth);

		public string FormatName(long value) => Format(value, _radix, _nameWidth);

		public static string Format(long value, uint radix, int width)
		{
			value &= long.MaxValue;
			Span<char> array = stackalloc char[64];
			ReadOnlySpan<char> digits = __digits;
			int i = 64;
			while (value > 0)
			{
				value = Math.DivRem(value, radix, out var x);
				array[--i] = digits[(int)x];
			}
			while (64 - i < width)
			{
				array[--i] = '0';
			}
			return array[i..].ToString();
		}
		private static readonly char[] __digits = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'];
	}

	private static StringBuilder AppendDirectory(StringBuilder path, long index, uint directoryCount, uint fileCount, Fmt format, char pathSeparator, DirectoryGenerationMode mode)
	{
		if (fileCount == 0 || directoryCount == 0)
			return path;
		switch (mode)
		{
			case DirectoryGenerationMode.BigEndian:
				{
					Span<long> stack = stackalloc long[32];
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

					break;
				}
			case DirectoryGenerationMode.LittleEndian:
				{
					// 1234567 -> "/67/45"
					var i = index;
					while (i > fileCount)
					{
						path.Append(pathSeparator).Append(format.FormatPart(i % directoryCount));
						i /= directoryCount;
					}

					break;
				}
			// (mode == DirectoryGenerationMode.Compatible)
			default:
				{
					// 1234567 -> "/34/12"
					var i = index / fileCount;
					while (i > 0)
					{
						path.Append(pathSeparator).Append(format.FormatPart(i % directoryCount));
						i /= directoryCount;
					}

					break;
				}
		}
		return path.Append(pathSeparator);
	}
}



public class PathFormatter2Options
{
	public int FileDigits { get; init; }
	public int DirectoryDigits { get; init; }
	public char PathSeparator { get; init; }
	public bool LittleEndian { get; init; }

	public PathFormatter2Options()
	{
		FileDigits = 4;
		DirectoryDigits = 3;
	}

	public PathFormatter2Options(int fileDigits, int directoryDigits = 0, char pathSeparator = default, bool littleEndian = false)
	{
		if (fileDigits <= 1 || fileDigits > 10)
			throw new ArgumentOutOfRangeException(nameof(fileDigits), fileDigits, null);
		if (directoryDigits < 0 || directoryDigits > fileDigits)
			throw new ArgumentOutOfRangeException(nameof(directoryDigits), directoryDigits, null);

		FileDigits = fileDigits;
		DirectoryDigits = directoryDigits == 0 ? FileDigits - 1: directoryDigits;
		PathSeparator = pathSeparator;
		LittleEndian = littleEndian;
	}

	public string MakePath(long index, string? extension = null)
	{
		var path = new StringBuilder();

		AppendDirectory(path, index, PathSeparator == default ? Path.DirectorySeparatorChar : PathSeparator);
		path.Append(index.ToString($"D{FileDigits}", CultureInfo.InvariantCulture));

		if (extension is { Length: > 0 })
		{
			if (extension[0] != '.')
				path.Append('.');
			path.Append(extension);
		}
		return path.ToString();
	}

	private void AppendDirectory(StringBuilder path, long index, char pathSeparator)
	{
		var x = index.ToString();
		if (LittleEndian)    // 1234567 -> "/67/45"
		{
			for (int i = x.Length - DirectoryDigits; i > FileDigits; i -= DirectoryDigits)
			{
				path.Append(pathSeparator).Append(x, i, DirectoryDigits);
			}
		}
		else            // 1234567 -> "/12/34"
		{
			for (int i = 0; i < x.Length - FileDigits; i += DirectoryDigits)
			{
				path.Append(pathSeparator).Append(x, i, DirectoryDigits);
			}
		}
	}

	public static string SplitToPath(string value, int partSize, int restSize, char partSeparator = default)
	{
		var path = new StringBuilder();
		if (partSeparator == default)
			partSeparator = Path.DirectorySeparatorChar;
		if (partSize <= 0 || restSize <= 0 || value.Length <= restSize)
			return value;

		int bound = value.Length - restSize;
		int first = bound % partSize;
		if (first > 0)
			path.Append('0', partSize - first).Append(value, 0, first).Append(partSeparator);

		for (int i = first; i < bound; i += partSize)
		{
			path.Append(value, i, partSize).Append(partSeparator);
		}
		path.Append(value, bound, restSize);
		return path.ToString();
	}

	public static string SplitToPath(long index, int partSize, int restSize, char partSeparator = default, string? suffix = null, PathSalt? salt = null)
	{
		var path = new StringBuilder();
		if (partSeparator == default)
			partSeparator = Path.DirectorySeparatorChar;

		var value = index.ToString();
		if (partSize <= 0 || restSize <= 0 || value.Length <= restSize)
			return value;

		int bound = value.Length - restSize;
		int first = bound % partSize;
		if (first > 0)
			path.Append('0', partSize - first).Append(value, 0, first).Append(partSeparator);

		for (int i = first; i < bound; i += partSize)
		{
			path.Append(value, i, partSize).Append(partSeparator);
		}
		path.Append(value, bound, restSize);
		if (salt != null)
			path.Append('-').Append(salt.ToString());
		if (suffix is { Length: >0 })
			if (suffix[0] != '.')
				path.Append('.').Append(suffix);
			else if (suffix.Length > 1)
				path.Append(suffix);

		return path.ToString();
	}
}

/// <summary>
/// Generates a salt value for path or file name generation, supporting both sequential and random modes. 
/// </summary>
/// <remarks>
/// Initializes a new instance of the PathSalt class, optionally using a sequential salt value.
/// <para>When sequential is set to true, the salt is derived from the current UTC timestamp, which can be
/// useful for scenarios requiring predictable or incrementing values. When false, a random salt is used to maximize
/// unpredictability.
/// </para>
/// </remarks>
/// <param name="sequential">true to generate a salt value based on the current time, ensuring sequential uniqueness; false to generate a random
/// salt value.</param>
public class PathSalt(bool sequential = false)
{
	private ulong _salt = (sequential ? (ulong)DateTime.UtcNow.Ticks : GetSalt());

	public ulong Value => _salt;

	/// <summary>
	/// Advances to the next variant in the sequence or selects a new variant at random, depending on the current mode.
	/// </summary>
	/// <remarks>If the object is in sequential mode, this method advances to the next variant in order. Otherwise,
	/// it selects a new variant using a random salt value. The current variant can be retrieved after calling this method.
	/// This method does not return a value.</remarks>
	public void Next() => _salt = (sequential ? NextSalt(_salt): GetSalt());

	/// <summary>
	/// Returns a string representation of the current salt value, formatted as a base-36 encoded string with leading zeros
	/// to ensure a fixed length.
	/// </summary>
	/// <remarks>The returned string is suitable for display or serialization purposes and preserves leading zeros
	/// to ensure a consistent length.</remarks>
	/// <returns>A string representing the salt value in base-36 format, padded with leading zeros to a length of 10 characters.</returns>
	public override string ToString() => ToString(sequential ? 10 : 8);

	/// <summary>
	/// Returns a base-36 string representation of the current salt value, padded with leading zeros to the specified
	/// width.
	/// </summary>
	/// <param name="width">The number of characters in the resulting string. Must be between 1 and 16, inclusive.</param>
	/// <returns>A string containing the base-36 representation of the salt value, left-padded with zeros to match the specified
	/// width.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when width is less than 1 or greater than 128.</exception>
	public string ToString(int width)
	{
		if (width is < 1 or > 16)
			throw new ArgumentOutOfRangeException(nameof(width));
		Span<char> chars = stackalloc char[width];
		ulong value = _salt;
		while (width > 0)
		{
			chars[--width] = __digits[(int)(value % 36)];
			value /= 36;
		}
		return chars.ToString();
	}
	private static readonly char[] __digits = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'];

	private static ulong GetSalt()
	{
		Guid guid = Guid.NewGuid();
#if NET
		Span<byte> bytes = stackalloc byte[16];
		guid.TryWriteBytes(bytes);
#else
		Span<byte> bytes = guid.ToByteArray();
#endif
		ref ulong x1 = ref Unsafe.As<byte, ulong>(ref MemoryMarshal.GetReference(bytes));
		var x2 = Unsafe.Add(ref x1, 1);
		return x1 ^ x2;
	}



#if NET
	private static ulong NextSalt(ulong salt) => salt + (uint)((Random.Shared.Next() & 31) + 1);
#else
	private static ulong NextSalt(ulong salt) => salt + (uint)((__r.Next() & 31) + 1);
	private static readonly Random __r = new Random();
#endif
}
