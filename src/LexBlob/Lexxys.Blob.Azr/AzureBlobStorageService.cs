using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Lexxys.Blob;

/// <summary>
/// Azure Blob Storage implementation of <see cref="IBlobStorageService"/>.
/// Supports simple read/write/copy/move/delete semantics and maps <see cref="BlobWriteMode"/> to Azure operations.
/// </summary>
public sealed partial class AzureBlobStorageService: IBlobStorageService
{
	private readonly BlobContainerClient _container;
	private readonly bool _disposeContainer;

	/// <summary>
	/// Construct from an existing container client.
	/// </summary>
	public AzureBlobStorageService(BlobContainerClient containerClient)
	{
		_container = containerClient ?? throw new ArgumentNullException(nameof(containerClient));
		_disposeContainer = false;
	}

	/// <summary>
	/// Construct from connection string + container name.
	/// </summary>
	public AzureBlobStorageService(string connectionString, string containerName)
	{
		if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));
		if (string.IsNullOrEmpty(containerName)) throw new ArgumentNullException(nameof(containerName));

		_container = new BlobContainerClient(connectionString, containerName);
		_disposeContainer = true;
	}

	private static (string container, string blobName) ParseLocation(string location, string defaultContainer)
	{
		if (location is null) throw new ArgumentNullException(nameof(location));

		// Accept azure://container/blob or just blob name (use default container)
		if (location.StartsWith("azure://", StringComparison.OrdinalIgnoreCase))
		{
			var without = location.Substring("azure://".Length);
			var idx = without.IndexOf('/');
			if (idx <= 0) throw new ArgumentException("Invalid Azure uri. Expected azure://container/blob", nameof(location));
			return (without.Substring(0, idx), without.Substring(idx + 1));
		}

		return (defaultContainer, location);
	}

	// ---------- synchronous API ----------

	public IBlobInfo GetBlobInfo(string location, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var (containerName, blobName) = ParseLocation(location, _container.Name);
		var client = _container.GetBlobClient(blobName);
		try
		{
			var props = client.GetProperties(cancellationToken: cancellationToken);
			return props.HasValue ?
				new AzureBlobInfo(client, exists: true, props.Value.ContentLength, props.Value.LastModified):
				new AzureBlobInfo(client, exists: false, 0, null);
		}
		catch (RequestFailedException ex) when (ex.Status == 404)
		{
			return new AzureBlobInfo(client, exists: false, 0, null);
		}
	}

	public Stream ReadBlob(string location, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var (containerName, blobName) = ParseLocation(location, _container.Name);
		var client = _container.GetBlobClient(blobName);
		// OpenRead returns a stream reading from service; caller disposes it.
		return client.OpenRead(cancellationToken: cancellationToken);
	}

	public void WriteBlob(string location, Stream stream, BlobWriteMode mode = default, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (stream is null) throw new ArgumentNullException(nameof(stream));

		var (containerName, blobName) = ParseLocation(location, _container.Name);
		var client = _container.GetBlobClient(blobName);
		var effective = mode == BlobWriteMode.Default ? BlobWriteMode.Overwrite : mode;

		switch (effective)
		{
			case BlobWriteMode.Create:
				{
					// fail if exists
					if (client.Exists(cancellationToken).Value)
						throw new IOException($"Blob '{blobName}' already exists in container '{_container.Name}'.");
					client.Upload(stream, overwrite: false, cancellationToken: cancellationToken);
					break;
				}
			case BlobWriteMode.Overwrite:
				{
					client.Upload(stream, overwrite: true, cancellationToken: cancellationToken);
					break;
				}
			case BlobWriteMode.Append:
				{
					// Append is not supported on block blobs directly; implement by downloading existing content and re-uploading combined.
					var temp = Path.GetTempFileName();
					try
					{
						// if exists, download existing
						if (client.Exists(cancellationToken).Value)
						{
							using var existing = client.OpenRead(cancellationToken: cancellationToken);
							using var f = File.Open(temp, FileMode.Create, FileAccess.Write, FileShare.None);
							existing.CopyTo(f);
						}

						// append provided stream
						using (var f = File.Open(temp, FileMode.Append, FileAccess.Write, FileShare.None))
						{
							stream.CopyTo(f);
						}

						// upload combined
						using var combined = File.OpenRead(temp);
						client.Upload(combined, overwrite: true, cancellationToken: cancellationToken);
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
		var (containerName, blobName) = ParseLocation(location, _container.Name);
		var client = _container.GetBlobClient(blobName);
		client.DeleteIfExists(cancellationToken: cancellationToken);
	}

	// ---------- asynchronous API ----------

	public async Task<IBlobInfo> GetBlobInfoAsync(string location, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var (containerName, blobName) = ParseLocation(location, _container.Name);
		var client = _container.GetBlobClient(blobName);
		try
		{
			var props = await client.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
			return new AzureBlobInfo(client, exists: true, props.Value.ContentLength, props.Value.LastModified);
		}
		catch (RequestFailedException ex) when (ex.Status == 404)
		{
			return new AzureBlobInfo(client, exists: false, 0, null);
		}
	}

	public Task<Stream> ReadBlobAsync(string location, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var (containerName, blobName) = ParseLocation(location, _container.Name);
		var client = _container.GetBlobClient(blobName);
		// OpenReadAsync returns Stream; wrap into completed task
		return Task.FromResult<Stream>(client.OpenRead(cancellationToken: cancellationToken));
	}

	public async Task WriteBlobAsync(string location, Stream stream, BlobWriteMode mode = default, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (stream is null) throw new ArgumentNullException(nameof(stream));

		var (containerName, blobName) = ParseLocation(location, _container.Name);
		var client = _container.GetBlobClient(blobName);
		var effective = mode == BlobWriteMode.Default ? BlobWriteMode.Overwrite : mode;

		switch (effective)
		{
			case BlobWriteMode.Create:
				{
					var exists = await client.ExistsAsync(cancellationToken).ConfigureAwait(false);
					if (exists.Value) throw new IOException($"Blob '{blobName}' already exists in container '{_container.Name}'.");
					await client.UploadAsync(stream, overwrite: false, cancellationToken: cancellationToken).ConfigureAwait(false);
					break;
				}
			case BlobWriteMode.Overwrite:
				{
					await client.UploadAsync(stream, overwrite: true, cancellationToken: cancellationToken).ConfigureAwait(false);
					break;
				}
			case BlobWriteMode.Append:
				{
					var temp = Path.GetTempFileName();
					try
					{
						var exists = await client.ExistsAsync(cancellationToken).ConfigureAwait(false);
						if (exists.Value)
						{
							await using var existing = await client.OpenReadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
							await using var f = File.Open(temp, FileMode.Create, FileAccess.Write, FileShare.None);
							await existing.CopyToAsync(f, cancellationToken).ConfigureAwait(false);
						}

						await using (var f = File.Open(temp, FileMode.Append, FileAccess.Write, FileShare.None))
						{
							await stream.CopyToAsync(f, cancellationToken).ConfigureAwait(false);
						}

						await using var combined = File.OpenRead(temp);
						await client.UploadAsync(combined, overwrite: true, cancellationToken: cancellationToken).ConfigureAwait(false);
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
		var (srcContainer, srcName) = ParseLocation(source, _container.Name);
		var (dstContainer, dstName) = ParseLocation(destination, _container.Name);

		var srcClient = _container.GetBlobClient(srcName);
		var dstClient = _container.GetBlobClient(dstName);

		var effective = mode == BlobWriteMode.Default ? BlobWriteMode.Create : mode;

		if (effective == BlobWriteMode.Create)
		{
			var dstExists = await dstClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
			if (dstExists.Value) throw new IOException($"Destination '{dstName}' already exists in container '{_container.Name}'.");
		}

		if (effective == BlobWriteMode.Append)
		{
			// download destination (if exists) and append source to it, then upload combined
			var temp = Path.GetTempFileName();
			try
			{
				var dstExists = await dstClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
				if (dstExists.Value)
				{
					await using var d = await dstClient.OpenReadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
					await using var f = File.Open(temp, FileMode.Create, FileAccess.Write, FileShare.None);
					await d.CopyToAsync(f, cancellationToken).ConfigureAwait(false);
				}

				var srcResponse = await srcClient.OpenReadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
				await using (srcResponse)
				await using (var f = File.Open(temp, FileMode.Append, FileAccess.Write, FileShare.None))
				{
					await srcResponse.CopyToAsync(f, cancellationToken).ConfigureAwait(false);
				}

				await using var combined = File.OpenRead(temp);
				await dstClient.UploadAsync(combined, overwrite: true, cancellationToken: cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				try { File.Delete(temp); } catch { }
			}
		}
		else
		{
			// service-side copy
			var srcUri = srcClient.Uri;
			var op = await dstClient.StartCopyFromUriAsync(srcUri, cancellationToken: cancellationToken).ConfigureAwait(false);

			// Poll until copy finishes (synchronous behaviour expected by interface)
			while (true)
			{
				var props = await dstClient.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
				if (props.Value.CopyStatus != CopyStatus.Pending) break;
				await Task.Delay(200, cancellationToken).ConfigureAwait(false);
			}
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
		var (containerName, blobName) = ParseLocation(location, _container.Name);
		var client = _container.GetBlobClient(blobName);
		await client.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
	}

	public void Dispose()
	{
		if (_disposeContainer && _container is IDisposable d) d.Dispose();
	}

	// ---------- nested types ----------

	private sealed class AzureBlobInfo: IBlobInfo
	{
		private readonly BlobClient _client;

		public AzureBlobInfo(BlobClient client, bool exists, long length, DateTimeOffset? lastModified)
		{
			_client = client;
			Exists = exists;
			Length = length;
			Path = client.Name;
			LastModified = lastModified;
		}

		public bool Exists { get; }
		public long Length { get; }
		public string Path { get; }
		public DateTimeOffset? LastModified { get; }

		public Stream OpenReadStream()
		{
			return _client.OpenRead();
		}

		public Task<Stream> OpenReadStreamAsync(CancellationToken cancellationToken = default)
		{
			return _client.OpenReadAsync(cancellationToken: cancellationToken);
		}
	}
}