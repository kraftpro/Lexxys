using System.Text;

namespace Lexxys;

public interface IBlobNameFormatter
{
	string CreateName(object? id, string? type = null, IDictionary<string, object?>? metadata = null);
}

public class FileBlobService: IBlobService
{
	private readonly string _domain;
	private readonly string _domainRef;
	private readonly string _basePath;
	private readonly IBlobNameFormatter _nameFormatter;

	public string Domain => _domain;

	public FileBlobService(string domain, string basePath, IBlobNameFormatter? nameFormatter = null)
	{
		if (string.IsNullOrEmpty(domain)) throw new ArgumentNullException(nameof(domain));
		if (string.IsNullOrEmpty(basePath)) throw new ArgumentNullException(nameof(basePath));

		_domain = domain.TrimEnd('/');
		_domainRef = _domain + '/';
		_basePath = Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar);
		if (!Directory.Exists(_basePath))
			throw new DirectoryNotFoundException($"Base path '{_basePath}' does not exist.");
		_basePath += Path.DirectorySeparatorChar;
		_nameFormatter = nameFormatter ?? BlobNameFormatter.Default;
	}

	public bool CanOpen(string reference)
		=> reference != null && reference.StartsWith(_domainRef, StringComparison.OrdinalIgnoreCase);

	public string CreateReference(BlobObjectId id, string? type = null, IDictionary<string, object?>? metadata = null)
		=> _domainRef + _nameFormatter.CreateName(id, type, metadata);

	public IBlobInfo GetBlobInfo(string reference)
	{
		string path = GetTargetPath(reference);
		return new FileBlobInfo(path, reference);
	}

	public void Write(string reference, Stream stream, FileWriteMode fileWrite = default)
	{
		string path = GetTargetPath(reference);
		var fileMode = fileWrite switch
		{
			FileWriteMode.Write => FileMode.Create,
			FileWriteMode.Create => FileMode.CreateNew,
			FileWriteMode.Append => FileMode.Append,
			_ => throw new ArgumentOutOfRangeException(nameof(fileWrite), fileWrite, null)
		};
		using FileStream fileStream = new FileStream(path, fileMode, FileAccess.Write, FileShare.None);
		stream.CopyTo(fileStream);
	}

	public void Copy(string source, string target, bool overwrite = false)
	{
		string sourcePath = GetTargetPath(source);
		string targetPath = GetTargetPath(target);

		File.Copy(sourcePath, targetPath, overwrite);
	}

	public void Move(string source, string target, bool overwrite = false)
	{
		string sourcePath = GetTargetPath(source);
		string targetPath = GetTargetPath(target);

#if NET
		File.Move(sourcePath, targetPath, overwrite);
#else
		if (!overwrite && File.Exists(targetPath))
			throw new IOException($"Target file '{targetPath}' already exists and overwrite is not allowed.");
		if (overwrite)
			File.Delete(targetPath);
		File.Move(sourcePath, targetPath);
#endif
	}

	public void Delete(string reference)
	{
		string path = GetTargetPath(reference);
		File.Delete(path);
	}

	public async Task<IBlobInfo> GetBlobInfoAsync(string reference, CancellationToken cancellation = default)
	{
		string path = GetTargetPath(reference);
		return await Task.FromResult(new FileBlobInfo(path, reference));
	}

	public async Task WriteAsync(string reference, Stream stream, FileWriteMode writeMode = default, CancellationToken cancellation = default)
	{
		string path = GetTargetPath(reference);
		var fileMode = writeMode switch
		{
			FileWriteMode.Write => FileMode.Create,
			FileWriteMode.Create => FileMode.CreateNew,
			FileWriteMode.Append => FileMode.Append,
			_ => throw new ArgumentOutOfRangeException(nameof(writeMode), writeMode, null)
		};
		using FileStream fileStream = new FileStream(path, fileMode, FileAccess.Write, FileShare.None);
#if NET
		await stream.CopyToAsync(fileStream, cancellation);
#else
		await stream.CopyToAsync(fileStream, 81920, cancellation);
#endif
	}

	public async Task CopyAsync(string source, string target, bool overwrite = false, CancellationToken cancellation = default)
	{
		string sourcePath = GetTargetPath(source);
		string targetPath = GetTargetPath(target);

		var fileMode = overwrite ? FileMode.Create: FileMode.CreateNew;
		using FileStream sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
		using FileStream targetStream = new FileStream(targetPath, fileMode, FileAccess.Write, FileShare.None);
#if NET
		await sourceStream.CopyToAsync(targetStream, cancellation);
#else
		await sourceStream.CopyToAsync(targetStream, 81920, cancellation);
#endif
	}

	public Task MoveAsync(string source, string target, bool overwrite = false, CancellationToken cancellation = default)
	{
		string sourcePath = GetTargetPath(source);
		string targetPath = GetTargetPath(target);

#if NET
		return Task.Run(() => File.Move(sourcePath, targetPath, overwrite), cancellation);
#else
		if (!overwrite && File.Exists(targetPath))
			throw new IOException($"Target file '{targetPath}' already exists and overwrite is not allowed.");
		return Task.Run(() =>
		{
			if (overwrite)
				File.Delete(targetPath);
			File.Move(sourcePath, targetPath);
		}, cancellation);
#endif

	}

	public Task DeleteAsync(string reference, CancellationToken cancellation = default)
	{
		string path = GetTargetPath(reference);
		return Task.Run(() => File.Delete(path), cancellation);
	}

	private string GetTargetPath(string reference)
	{
		ValidateReference(reference);
		return Path.Combine(_basePath, reference[_domainRef.Length..]);
	}

	private void ValidateReference(string reference)
	{
		if (!CanOpen(reference))
			throw new ArgumentException("Invalid blob reference", nameof(reference));
	}

	class FileBlobInfo: IBlobInfo
	{
		private readonly FileInfo _fileInfo;
		private readonly bool _exists;
		private readonly string _reference;

		public FileBlobInfo(string path, string reference)
		{
			_fileInfo = new FileInfo(path);
			_reference = reference;
			_exists = _fileInfo.Exists;
		}

		public bool Exists => _exists;
		public string Name => _fileInfo.Name;
		public long Length => _exists ? _fileInfo.Length: 0;
		public DateTimeOffset? LastModified => _exists ? _fileInfo.LastWriteTimeUtc: null;
		public string Path => _reference;

		public Stream OpenReadStream() => _exists ? _fileInfo.OpenRead(): Stream.Null;

		public Task<Stream> OpenReadStreamAsync() => Task.FromResult(_exists ? new FileStream(_fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read): Stream.Null);
	}
}

public class BlobNameFormatter: IBlobNameFormatter
{
	public static readonly IBlobNameFormatter Default = new BlobNameFormatter(minLength: 7, padChar: '0', temporaryDirectory: ".t");
	public const char DirectorySeparator = '/';

	public BlobNameFormatter(int minLength = 0, char padChar = default, string? temporaryDirectory = null, int segmentCount = 0, int segmentLength = 0)
	{
		MinLength = minLength;
		PadChar = padChar == default ? '0': padChar;
		TemporaryDirectory = temporaryDirectory;
		DirectorySegmentCount = segmentCount >= 0 ? segmentCount: throw new ArgumentOutOfRangeException(nameof(segmentCount), segmentCount, null);
		DirectorySegmentLength = segmentLength;
	}

	public int MinLength { get; }
	public char PadChar { get; }
	public string? TemporaryDirectory { get; }
	public int DirectorySegmentCount { get; }
	public int DirectorySegmentLength { get; }

	public string CreateName(object? id, string? type = null, IDictionary<string, object?>? metadata = null)
	{
		StringBuilder name = new StringBuilder(120);

		var s = id?.ToString();
		if (s == null)
		{
			if (TemporaryDirectory is null)
				throw new ArgumentNullException(nameof(id), "Temporary directory must be specified when id is null.");
			name.Append(TemporaryDirectory).Append(Guid.NewGuid().ToString("n"));
		}
		else
		{
			if (DirectorySegmentCount > 0 && DirectorySegmentLength != 0 && s.Length > MinLength)
			{
				bool leftToRight = DirectorySegmentLength > 0;
				int segmentLength = Math.Abs(DirectorySegmentLength);
				var rest = leftToRight ? s.AsSpan(0, s.Length - MinLength): s.AsSpan(MinLength);

				for (int i = 0; i < DirectorySegmentCount && rest.Length > 0; ++i)
				{
					var part = rest.Length <= segmentLength ? rest: leftToRight ? rest[..segmentLength]: rest[^segmentLength..];
					if (part.Length < segmentLength)
						name.Append(PadChar, segmentLength - part.Length);
					name.Append(part).Append(DirectorySeparator);
					rest = rest.Length <= segmentLength ? []: leftToRight ? rest[segmentLength..]: rest[..^segmentLength];
				}
			}
			else if (s.Length < MinLength)
			{
				name.Append(PadChar, MinLength - s.Length); 
			}
			name.Append(s);
		}

		if (id != null && metadata != null)
		{
			foreach (var kv in metadata.OrderBy(o => o.Key))
			{
				name.Append('-').Append(kv.Value);
			}
		}
		if (type is { Length: > 0 })
			name.Append(MimeMapping.GetExtension(type, String.Empty));
		return name.ToString();
	}
}
