using Amazon.S3;
using Amazon.S3.Model;

using System.Net;

namespace Lexxys.Blob;

public sealed partial class AmazonBlobStorage: IBlobStorageService
{
	private readonly IAmazonS3 _client;
	private readonly bool _disposeClient;
	private readonly string _defaultBucket;

	public AmazonBlobStorage(string bucketName, IAmazonS3? client = null)
	{
		if (string.IsNullOrEmpty(bucketName)) throw new ArgumentNullException(nameof(bucketName));
		_defaultBucket = bucketName;
		if (client is null)
		{
			_client = new AmazonS3Client();
			_disposeClient = true;
		}
		else
		{
			_client = client;
			_disposeClient = false;
		}
	}

	private static (string bucket, string key) ParseLocation(string location, string defaultBucket)
	{
		if (location is null) throw new ArgumentNullException(nameof(location));
		// support s3://bucket/key or key
		if (location.StartsWith("s3://", StringComparison.OrdinalIgnoreCase))
		{
			var without = location.Substring(5);
			var idx = without.IndexOf('/');
			if (idx <= 0)
				throw new ArgumentException("Invalid S3 URI, expected s3://bucket/key", nameof(location));
			return (without.Substring(0, idx), without.Substring(idx + 1));
		}
		return (defaultBucket, location);
	}

	// ----- sync operations -----

	public IBlobInfo GetBlobInfo(string location, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var (bucket, key) = ParseLocation(location, _defaultBucket);
		try
		{
			var meta = _client.GetObjectMetadataAsync(bucket, key, cancellationToken).GetAwaiter().GetResult();
			return new S3BlobInfo(this, bucket, key, exists: true, meta.ContentLength, meta.LastModified);
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			return new S3BlobInfo(this, bucket, key, exists: false, 0, null);
		}
	}

	public Stream ReadBlob(string location, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var (bucket, key) = ParseLocation(location, _defaultBucket);
		var response = _client.GetObjectAsync(bucket, key, cancellationToken).GetAwaiter().GetResult();
		return new ResponseStreamWrapper(response.ResponseStream, response);
	}

	public void WriteBlob(string location, Stream stream, BlobWriteMode mode = default, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (stream is null) throw new ArgumentNullException(nameof(stream));

		var (bucket, key) = ParseLocation(location, _defaultBucket);
		var effectiveMode = mode == BlobWriteMode.Default ? BlobWriteMode.Overwrite : mode;

		switch (effectiveMode)
		{
			case BlobWriteMode.Create:
				{
					// throw if exists
					try
					{
						var meta = _client.GetObjectMetadataAsync(bucket, key, cancellationToken).GetAwaiter().GetResult();
						if (meta != null) throw new IOException($"Object '{key}' already exists in bucket '{bucket}'.");
					}
					catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
					{
						// ok - continue
					}
					UploadSync(bucket, key, stream, cancellationToken);
					break;
				}
			case BlobWriteMode.Overwrite:
				{
					UploadSync(bucket, key, stream, cancellationToken);
					break;
				}
			case BlobWriteMode.Append:
				{
					// S3 doesn't support append natively — implement by combining existing data (if any) and new stream.
					var temp = Path.GetTempFileName();
					try
					{
						// if exists, download existing content first
						try
						{
							var get = _client.GetObjectAsync(bucket, key, cancellationToken).GetAwaiter().GetResult();
							using (var s = get.ResponseStream)
							using (var f = File.Open(temp, FileMode.Create, FileAccess.Write, FileShare.None))
							{
								s.CopyTo(f);
							}
						}
						catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
						{
							// no existing content - continue
						}

						// append provided stream
						using (var f = File.Open(temp, FileMode.Append, FileAccess.Write, FileShare.None))
						{
							stream.CopyTo(f);
						}

						// upload combined file
						using (var f = File.OpenRead(temp))
						{
							UploadSync(bucket, key, f, cancellationToken);
						}
					}
					finally
					{
						try { File.Delete(temp); } catch { }
					}
					break;
				}
			default:
				throw new NotSupportedException($"Write mode '{mode}' is not supported.");
		}
	}

	public void CopyBlob(string source, string destination, BlobWriteMode mode = default, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		CopyBlobAsync(source, destination, mode, cancellationToken).GetAwaiter().GetResult();
	}

	public void MoveBlob(string source, string destination, BlobWriteMode mode = default, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		MoveBlobAsync(source, destination, mode, cancellationToken).GetAwaiter().GetResult();
	}

	public void DeleteBlob(string location, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var (bucket, key) = ParseLocation(location, _defaultBucket);
		_client.DeleteObjectAsync(bucket, key, cancellationToken).GetAwaiter().GetResult();
	}

	// ----- async operations -----

	public async Task<IBlobInfo> GetBlobInfoAsync(string location, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var (bucket, key) = ParseLocation(location, _defaultBucket);
		try
		{
			var meta = await _client.GetObjectMetadataAsync(bucket, key, cancellationToken).ConfigureAwait(false);
			return new S3BlobInfo(this, bucket, key, exists: true, meta.ContentLength, meta.LastModified);
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			return new S3BlobInfo(this, bucket, key, exists: false, 0, null);
		}
	}

	public async Task<Stream> ReadBlobAsync(string location, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var (bucket, key) = ParseLocation(location, _defaultBucket);
		var response = await _client.GetObjectAsync(bucket, key, cancellationToken).ConfigureAwait(false);
		return new ResponseStreamWrapper(response.ResponseStream, response);
	}

	public async Task WriteBlobAsync(string location, Stream stream, BlobWriteMode mode = default, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (stream is null) throw new ArgumentNullException(nameof(stream));

		var (bucket, key) = ParseLocation(location, _defaultBucket);
		var effectiveMode = mode == BlobWriteMode.Default ? BlobWriteMode.Overwrite : mode;

		switch (effectiveMode)
		{
			case BlobWriteMode.Create:
				{
					try
					{
						var meta = await _client.GetObjectMetadataAsync(bucket, key, cancellationToken).ConfigureAwait(false);
						if (meta != null) throw new IOException($"Object '{key}' already exists in bucket '{bucket}'.");
					}
					catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
					{
						// ok
					}
					await UploadAsync(bucket, key, stream, cancellationToken).ConfigureAwait(false);
					break;
				}
			case BlobWriteMode.Overwrite:
				{
					await UploadAsync(bucket, key, stream, cancellationToken).ConfigureAwait(false);
					break;
				}
			case BlobWriteMode.Append:
				{
					var temp = Path.GetTempFileName();
					try
					{
						// if exists download existing
						try
						{
							var get = await _client.GetObjectAsync(bucket, key, cancellationToken).ConfigureAwait(false);
							await using (var s = get.ResponseStream)
							await using (var f = File.Open(temp, FileMode.Create, FileAccess.Write, FileShare.None))
							{
								await s.CopyToAsync(f, cancellationToken).ConfigureAwait(false);
							}
						}
						catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
						{
							// no existing
						}

						// append provided stream
						await using (var f = File.Open(temp, FileMode.Append, FileAccess.Write, FileShare.None))
						{
							await stream.CopyToAsync(f, cancellationToken).ConfigureAwait(false);
						}

						await using (var f = File.OpenRead(temp))
						{
							await UploadAsync(bucket, key, f, cancellationToken).ConfigureAwait(false);
						}
					}
					finally
					{
						try { File.Delete(temp); } catch { }
					}
					break;
				}
			default:
				throw new NotSupportedException($"Write mode '{mode}' is not supported.");
		}
	}

	public async Task CopyBlobAsync(string source, string destination, BlobWriteMode mode = default, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var (srcBucket, srcKey) = ParseLocation(source, _defaultBucket);
		var (dstBucket, dstKey) = ParseLocation(destination, _defaultBucket);

		var effectiveMode = mode == BlobWriteMode.Default ? BlobWriteMode.Create : mode;

		if (effectiveMode == BlobWriteMode.Create)
		{
			// throw if destination exists
			try
			{
				var m = await _client.GetObjectMetadataAsync(dstBucket, dstKey, cancellationToken).ConfigureAwait(false);
				if (m != null) throw new IOException($"Destination '{dstKey}' already exists in bucket '{dstBucket}'.");
			}
			catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
			{
				// ok
			}
		}

		if (effectiveMode == BlobWriteMode.Append)
		{
			// download destination (if exists) and append source to it, then upload combined
			var temp = Path.GetTempFileName();
			try
			{
				try
				{
					var dstGet = await _client.GetObjectAsync(dstBucket, dstKey, cancellationToken).ConfigureAwait(false);
					await using (var s = dstGet.ResponseStream)
					await using (var f = File.Open(temp, FileMode.Create, FileAccess.Write, FileShare.None))
					{
						await s.CopyToAsync(f, cancellationToken).ConfigureAwait(false);
					}
				}
				catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
				{
					// no destination content
				}

				// append source
				var srcGet = await _client.GetObjectAsync(srcBucket, srcKey, cancellationToken).ConfigureAwait(false);
				await using (var s = srcGet.ResponseStream)
				await using (var f = File.Open(temp, FileMode.Append, FileAccess.Write, FileShare.None))
				{
					await s.CopyToAsync(f, cancellationToken).ConfigureAwait(false);
				}

				await using (var f = File.OpenRead(temp))
				{
					await UploadAsync(dstBucket, dstKey, f, cancellationToken).ConfigureAwait(false);
				}
			}
			finally
			{
				try { File.Delete(temp); } catch { }
			}
		}
		else
		{
			// service-side copy
			var request = new CopyObjectRequest
			{
				SourceBucket = srcBucket,
				SourceKey = srcKey,
				DestinationBucket = dstBucket,
				DestinationKey = dstKey
			};
			await _client.CopyObjectAsync(request, cancellationToken).ConfigureAwait(false);
		}
	}

	public async Task MoveBlobAsync(string source, string destination, BlobWriteMode mode = default, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		await CopyBlobAsync(source, destination, mode, cancellationToken).ConfigureAwait(false);
		await DeleteBlobAsync(source, cancellationToken).ConfigureAwait(false);
	}

	public async Task DeleteBlobAsync(string location, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var (bucket, key) = ParseLocation(location, _defaultBucket);
		await _client.DeleteObjectAsync(bucket, key, cancellationToken).ConfigureAwait(false);
	}

	// ----- helpers -----

	private void UploadSync(string bucket, string key, Stream stream, CancellationToken cancellationToken)
	{
		var request = new PutObjectRequest
		{
			BucketName = bucket,
			Key = key,
			InputStream = stream
		};
		_client.PutObjectAsync(request, cancellationToken).GetAwaiter().GetResult();
	}

	private Task UploadAsync(string bucket, string key, Stream stream, CancellationToken cancellationToken)
	{
		var request = new PutObjectRequest
		{
			BucketName = bucket,
			Key = key,
			InputStream = stream
		};
		return _client.PutObjectAsync(request, cancellationToken);
	}

	public void Dispose()
	{
		if (_disposeClient) _client.Dispose();
	}

	// ----- nested types -----

	private sealed class S3BlobInfo: IBlobInfo
	{
		private readonly string _bucket;
		private readonly string _key;
		private readonly AmazonBlobStorage _owner;

		public S3BlobInfo(AmazonBlobStorage owner, string bucket, string key, bool exists, long length, DateTime? lastModified)
		{
			_owner = owner;
			_bucket = bucket;
			_key = key;
			Exists = exists;
			Length = length;
			Path = $"{_bucket}/{_key}";
			LastModified = lastModified.HasValue ? new DateTimeOffset(lastModified.Value) : (DateTimeOffset?)null;
		}

		public bool Exists { get; }
		public long Length { get; }
		public string Path { get; }
		public DateTimeOffset? LastModified { get; }

		public Stream OpenReadStream()
		{
			var response = _owner._client.GetObjectAsync(_bucket, _key).GetAwaiter().GetResult();
			return new ResponseStreamWrapper(response.ResponseStream, response);
		}

		public Task<Stream> OpenReadStreamAsync(CancellationToken cancellationToken = default)
		{
			return _owner._client.GetObjectAsync(_bucket, _key, cancellationToken)
				.ContinueWith<Task<Stream>>(t => Task.FromResult<Stream>(new ResponseStreamWrapper(t.Result.ResponseStream, t.Result)))
				.Unwrap();
		}
	}

	/// <summary>
	/// Wraps response stream and disposes response when the stream is disposed.
	/// </summary>
	private sealed class ResponseStreamWrapper: Stream
	{
		private readonly Stream _inner;
		private readonly IDisposable _response;

		public ResponseStreamWrapper(Stream inner, IDisposable response)
		{
			_inner = inner ?? throw new ArgumentNullException(nameof(inner));
			_response = response ?? throw new ArgumentNullException(nameof(response));
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				try { _inner.Dispose(); } catch { }
				try { _response.Dispose(); } catch { }
			}
			base.Dispose(disposing);
		}

		public override bool CanRead => _inner.CanRead;
		public override bool CanSeek => _inner.CanSeek;
		public override bool CanWrite => _inner.CanWrite;
		public override long Length => _inner.Length;
		public override long Position { get => _inner.Position; set => _inner.Position = value; }
		public override void Flush() => _inner.Flush();
		public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
		public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
		public override void SetLength(long value) => _inner.SetLength(value);
		public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);
		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _inner.ReadAsync(buffer, offset, count, cancellationToken);
		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _inner.WriteAsync(buffer, offset, count, cancellationToken);
		public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);
#if NETSTANDARD2_0 || NET462
		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) => _inner.BeginRead(buffer, offset, count, callback, state);
		public override int EndRead(IAsyncResult asyncResult) => _inner.EndRead(asyncResult);
#endif
	}
}