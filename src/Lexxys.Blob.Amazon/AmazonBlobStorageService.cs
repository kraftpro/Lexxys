using Amazon.S3;
using Amazon.S3.Model;

using System.Net;
using System.Reflection;

namespace Lexxys;

public class AmazonBlobStorageService: IBlobStorageService
{
	private readonly IAmazonS3 _client;
	private readonly bool _disposeClient;
	private readonly string _bucketName;

	public AmazonBlobStorageService(string bucketName, IAmazonS3 client, bool disposeClient = false)
	{
		if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
		if (client is null) throw new ArgumentNullException(nameof(client));

		_bucketName = bucketName;
		_client = client;
		_disposeClient = disposeClient;
	}

	public AmazonBlobStorageService(string bucketName, AmazonS3Config config)
	{
		if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
		if (config is null) throw new ArgumentNullException(nameof(config));

		_bucketName = bucketName;
		_client = new AmazonS3Client(config);
		_disposeClient = true;
	}

	private static string NormalizeKey(string location) => location;

	public IBlobInfo GetBlobInfo(string location, CancellationToken cancellationToken = default)
	{
		if (location is null) throw new ArgumentNullException(nameof(location));
		cancellationToken.ThrowIfCancellationRequested();

		var key = NormalizeKey(location);
		try
		{
			var meta = _client.GetObjectMetadataAsync(_bucketName, key).GetAwaiter().GetResult();
			return new S3BlobInfo(_client, _bucketName, key, exists: true, meta.ContentLength, meta.LastModified);
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			return new S3BlobInfo(_client, _bucketName, key, exists: false, 0, null);
		}
	}

	public async Task<IBlobInfo> GetBlobInfoAsync(string location, CancellationToken cancellationToken = default)
	{
		if (location is null) throw new ArgumentNullException(nameof(location));
		cancellationToken.ThrowIfCancellationRequested();

		var key = NormalizeKey(location);
		try
		{
			var meta = await _client.GetObjectMetadataAsync(_bucketName, key, cancellationToken).ConfigureAwait(false);
			return new S3BlobInfo(_client, _bucketName, key, exists: true, meta.ContentLength, meta.LastModified);
		}
		catch (AmazonS3Exception)
		{
			return new S3BlobInfo(_client, _bucketName, key, exists: false, 0, null);
		}
	}

	public Stream Read(string location, CancellationToken cancellationToken = default)
		=> ReadAsync(location, cancellationToken).GetAwaiter().GetResult();

	public async Task<Stream> ReadAsync(string location, CancellationToken cancellationToken = default)
	{
		if (location is null) throw new ArgumentNullException(nameof(location));
		cancellationToken.ThrowIfCancellationRequested();

		var key = NormalizeKey(location);
		var response = await _client.GetObjectAsync(_bucketName, key, cancellationToken).ConfigureAwait(false);
		return new ResponseStreamWrapper(response);
	}

	public void Write(string location, Stream stream, BlobWriteMode mode = default, CancellationToken cancellationToken = default)
		=> WriteAsync(location, stream, mode, cancellationToken).GetAwaiter().GetResult();

	public async Task WriteAsync(string location, Stream stream, BlobWriteMode mode = default, CancellationToken cancellationToken = default)
	{
		if (location is null) throw new ArgumentNullException(nameof(location));
		if (stream is null) throw new ArgumentNullException(nameof(stream));
		cancellationToken.ThrowIfCancellationRequested();

		var key = NormalizeKey(location);
		long offset = 0;
		if (mode == BlobWriteMode.Append)
		{
			try
			{
				var meta = await _client.GetObjectMetadataAsync(_bucketName, key, cancellationToken).ConfigureAwait(false);
				offset = meta.StorageClass == S3StorageClass.ExpressOnezone ? meta.ContentLength: -meta.ContentLength;
			}
			catch (AmazonS3Exception)
			{
				// the object does not exist
			}
			if (offset < 0)
			{
				WriteAppendAsync(key, stream, cancellationToken).Wait(cancellationToken);
				return;
			}
		}
		var request = new PutObjectRequest
		{
			BucketName = _bucketName,
			Key = key,
			InputStream = stream,
			WriteOffsetBytes = offset,
			IfNoneMatch = mode == BlobWriteMode.Create ? "*" : null
		};
		var resp = await _client.PutObjectAsync(request).ConfigureAwait(false);
		if (resp.HttpStatusCode != HttpStatusCode.OK)
			throw new IOException(resp.HttpStatusCode == HttpStatusCode.PreconditionFailed ? $"The blob '{location}' already exists.": $"Failed to write blob '{location}'.");

		async Task WriteAppendAsync(string key, Stream stream, CancellationToken cancellationToken)
		{
			using var temp = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.DeleteOnClose);
			var getRequest = new GetObjectRequest
			{
				BucketName = _bucketName,
				Key = key
			};
			using (var response = await _client.GetObjectAsync(getRequest, cancellationToken).ConfigureAwait(false))
			{
				await response.ResponseStream.CopyToAsync(temp, 81920, cancellationToken).ConfigureAwait(false);
			}
			await stream.CopyToAsync(temp, 81920, cancellationToken).ConfigureAwait(false);
			temp.Seek(0, SeekOrigin.Begin);
			var putRequest = new PutObjectRequest
			{
				BucketName = _bucketName,
				Key = key,
				InputStream = temp
			};
			await _client.PutObjectAsync(putRequest, cancellationToken).ConfigureAwait(false);
		}
	}

	public void Copy(string source, string destination, BlobWriteMode mode = default, CancellationToken cancellationToken = default)
		=> CopyAsync(source, destination, mode, cancellationToken).GetAwaiter().GetResult();

	public async Task CopyAsync(string source, string destination, BlobWriteMode mode = default, CancellationToken cancellationToken = default)
	{
		if (source is null) throw new ArgumentNullException(nameof(source));
		if (destination is null) throw new ArgumentNullException(nameof(destination));
		var src = NormalizeKey(source);
		var dst = NormalizeKey(destination);
		if (mode == BlobWriteMode.Append)
		{
			await CopyppendAsync(src, dst, cancellationToken).ConfigureAwait(false);
			return;
		}

		var request = new CopyObjectRequest
		{
			SourceBucket = _bucketName,
			SourceKey = src,
			DestinationBucket = _bucketName,
			DestinationKey = dst,
			IfNoneMatch = mode == BlobWriteMode.Overwrite ? null: "*"
		};
		await _client.CopyObjectAsync(request, cancellationToken).ConfigureAwait(false);

		async Task CopyppendAsync(string source, string destination, CancellationToken cancellationToken)
		{
			using var temp = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.DeleteOnClose);
			var getRequest = new GetObjectRequest
			{
				BucketName = _bucketName,
				Key = destination
			};
			using (var response = await _client.GetObjectAsync(getRequest, cancellationToken).ConfigureAwait(false))
			{
				await response.ResponseStream.CopyToAsync(temp, 81920, cancellationToken).ConfigureAwait(false);
			}
			var getSourceRequest = new GetObjectRequest
			{
				BucketName = _bucketName,
				Key = source
			};
			using (var response = await _client.GetObjectAsync(getSourceRequest, cancellationToken).ConfigureAwait(false))
			{
				await response.ResponseStream.CopyToAsync(temp, 81920, cancellationToken).ConfigureAwait(false);
			}
			temp.Seek(0, SeekOrigin.Begin);
			var putRequest = new PutObjectRequest
			{
				BucketName = _bucketName,
				Key = destination,
				InputStream = temp
			};
			await _client.PutObjectAsync(putRequest, cancellationToken).ConfigureAwait(false);
		}
	}

	public void Move(string source, string destination, BlobWriteMode mode = default, CancellationToken cancellationToken = default)
		=> MoveAsync(source, destination, mode, cancellationToken).GetAwaiter().GetResult();

	public async Task MoveAsync(string source, string destination, BlobWriteMode mode = default, CancellationToken cancellationToken = default)
	{
		if (source is null) throw new ArgumentNullException(nameof(source));
		if (destination is null) throw new ArgumentNullException(nameof(destination));

		await CopyAsync(source, destination, mode, cancellationToken).ConfigureAwait(false);
		await DeleteAsync(source, cancellationToken).ConfigureAwait(false);
	}

	public void Delete(string location, CancellationToken cancellationToken)
		=> DeleteAsync(location, cancellationToken).GetAwaiter().GetResult();


	public async Task DeleteAsync(string location, CancellationToken cancellationToken = default)
	{
		if (location is null) throw new ArgumentNullException(nameof(location));
		var key = NormalizeKey(location);
		await _client.DeleteObjectAsync(_bucketName, key, cancellationToken).ConfigureAwait(false);
	}

	public void Dispose()
	{
		if (_disposeClient)
			_client.Dispose();
		GC.SuppressFinalize(this);
	}

	private sealed class S3BlobInfo: IBlobInfo
	{
		private readonly IAmazonS3 _client;
		private readonly string _bucket;

		public S3BlobInfo(IAmazonS3 client, string bucket, string key, bool exists, long length, DateTime? lastModified)
		{
			_client = client;
			_bucket = bucket;
			Exists = exists;
			Length = length;
			Path = key;
			LastModified = lastModified.HasValue ? lastModified.Value.ToUniversalTime(): null;
		}

		public bool Exists { get; }
		public long Length { get; }
		public string Path { get; }
		public DateTime? LastModified { get; }

		public Stream OpenReadStream()
		{
			var response = _client.GetObjectAsync(_bucket, Path).GetAwaiter().GetResult();
			return new ResponseStreamWrapper(response);
		}

		public async Task<Stream> OpenReadStreamAsync(CancellationToken cancellationToken = default)
		{
			var response = await _client.GetObjectAsync(_bucket, Path, cancellationToken).ConfigureAwait(false);
			return new ResponseStreamWrapper(response);
		}
	}

	// Wraps the underlying response stream and disposes the response when stream is disposed.
	private sealed class ResponseStreamWrapper: Stream
	{
		private readonly Stream _stream;
		private readonly GetObjectResponse _response;

		public ResponseStreamWrapper(GetObjectResponse response)
		{
			_response = response ?? throw new ArgumentNullException(nameof(response));
			_stream = response.ResponseStream;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_stream.Dispose();
				_response.Dispose();
			}
			base.Dispose(disposing);
		}

		public override bool CanRead => _stream.CanRead;
		public override bool CanSeek => _stream.CanSeek;
		public override bool CanWrite => _stream.CanWrite;
		public override long Length => _stream.Length;
		public override long Position { get => _stream.Position; set => _stream.Position = value; }
		public override void Flush() => _stream.Flush();
		public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, count);
		public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);
		public override void SetLength(long value) => _stream.SetLength(value);
		public override void Write(byte[] buffer, int offset, int count) => _stream.Write(buffer, offset, count);
		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _stream.ReadAsync(buffer, offset, count, cancellationToken);
		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _stream.WriteAsync(buffer, offset, count, cancellationToken);
		public override Task FlushAsync(CancellationToken cancellationToken) => _stream.FlushAsync(cancellationToken);

#if NET
		public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => _stream.ReadAsync(buffer, cancellationToken);
		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) => _stream.WriteAsync(buffer, cancellationToken);
#else
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) => _stream.BeginRead(buffer, offset, count, callback, state);
        public override int EndRead(IAsyncResult asyncResult) => _stream.EndRead(asyncResult);
#endif
	}
}
