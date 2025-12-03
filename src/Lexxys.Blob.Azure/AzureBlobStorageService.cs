using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

namespace Lexxys;

public class AzureBlobStorageService: IBlobStorageService
{
	private readonly BlobContainerClient _container;
	private bool _disposeContainer;

	public AzureBlobStorageService(BlobContainerClient containerClient)
	{
		_container = containerClient ?? throw new ArgumentNullException(nameof(containerClient));
	}

	public AzureBlobStorageService(string connectionString, string containerName)
	{
		if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));
		if (string.IsNullOrEmpty(containerName)) throw new ArgumentNullException(nameof(containerName));

		_container = new BlobContainerClient(connectionString, containerName);
		_disposeContainer = true;
	}

	public IBlobInfo GetBlobInfo(string location, CancellationToken cancellationToken = default)
	{
		if (location is not { Length: >0 }) throw new ArgumentNullException(nameof(location));
		cancellationToken.ThrowIfCancellationRequested();

		var blob = GetBlobClient(location);
		try
		{
			var props = blob.GetProperties(cancellationToken: cancellationToken);
			return props.HasValue ?
				new AzureBlobInfo(blob, exists: true, props.Value.ContentLength, props.Value.LastModified.UtcDateTime):
				new AzureBlobInfo(blob, exists: false, 0, null);
		}
		catch (RequestFailedException ex) when (ex.Status == 404)
		{
			return new AzureBlobInfo(blob, exists: false, 0, null);
		}
	}

	public async Task<IBlobInfo> GetBlobInfoAsync(string location, CancellationToken cancellationToken = default)
	{
		if (location is not { Length: >0 }) throw new ArgumentNullException(nameof(location));
		cancellationToken.ThrowIfCancellationRequested();

		var blob = GetBlobClient(location);
		try
		{
			var props = await blob.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
			return props.HasValue ?
				new AzureBlobInfo(blob, exists: true, props.Value.ContentLength, props.Value.LastModified.UtcDateTime):
				new AzureBlobInfo(blob, exists: false, 0, null);
		}
		catch (RequestFailedException ex) when (ex.Status == 404)
		{
			return new AzureBlobInfo(blob, exists: false, 0, null);
		}
	}

	public Stream Read(string location, CancellationToken cancellationToken = default)
	{
		if (location is not { Length: >0 }) throw new ArgumentNullException(nameof(location));
		cancellationToken.ThrowIfCancellationRequested();

		var blob = GetBlobClient(location);
		return blob.OpenRead(cancellationToken: cancellationToken);
	}

	public Task<Stream> ReadAsync(string location, CancellationToken cancellationToken = default)
	{
		if (location is not { Length: >0 }) throw new ArgumentNullException(nameof(location));
		cancellationToken.ThrowIfCancellationRequested();

		var blob = GetBlobClient(location);
		return blob.OpenReadAsync(cancellationToken: cancellationToken);
	}

	public void Write(string location, Stream stream, BlobWriteMode mode = default, CancellationToken cancellationToken = default)
	{
		if (location is not { Length: >0 }) throw new ArgumentNullException(nameof(location));
		if (stream is null) throw new ArgumentNullException(nameof(stream));
		cancellationToken.ThrowIfCancellationRequested();

		if (mode == BlobWriteMode.Default)
			mode = BlobWriteMode.Overwrite;

		if (mode == BlobWriteMode.Append)
		{
			var blob = _container.GetAppendBlobClient(NormalizeKey(location));
			blob.AppendBlock(stream, cancellationToken: cancellationToken);
		}
		else
		{
			var blob = GetBlobClient(location);
			blob.Upload(stream, overwrite: mode == BlobWriteMode.Overwrite, cancellationToken: cancellationToken);
		}
	}

	public Task WriteAsync(string location, Stream stream, BlobWriteMode mode, CancellationToken cancellationToken = default)
	{
		if (location is not { Length: >0 }) throw new ArgumentNullException(nameof(location));
		if (stream is null) throw new ArgumentNullException(nameof(stream));
		cancellationToken.ThrowIfCancellationRequested();

		if (mode == BlobWriteMode.Append)
		{
			var blob = _container.GetAppendBlobClient(NormalizeKey(location));
			return blob.AppendBlockAsync(stream, cancellationToken: cancellationToken);
		}
		else
		{
			var blob = GetBlobClient(location);
			return blob.UploadAsync(stream, overwrite: mode == BlobWriteMode.Overwrite, cancellationToken: cancellationToken);
		}
	}

	public void Copy(string source, string destination, BlobWriteMode mode, CancellationToken cancellationToken = default)
	{
		if (source is not { Length: >0 }) throw new ArgumentNullException(nameof(source));
		if (destination is not { Length: >0 }) throw new ArgumentNullException(nameof(destination));
		cancellationToken.ThrowIfCancellationRequested();

		if (mode == BlobWriteMode.Default)
			mode = BlobWriteMode.Create;

		var srcBlob = GetBlobClient(source);

		if (mode != BlobWriteMode.Append && TryServerSideCopy(srcBlob, destination, mode, cancellationToken))
			return;

		// Fallback: download to stream and upload
		using var s = srcBlob.OpenRead(cancellationToken: cancellationToken);
		Write(destination, s, mode, cancellationToken);


		bool TryServerSideCopy(BlobClient srcBlob, string destination, BlobWriteMode mode, CancellationToken cancellationToken)
		{
			var dstBlob = GetBlobClient(destination);

			// If source is not accessible via URI for service-side copy, fall back to download/upload.
			// Try service-side copy first.
			try
			{
				const int Delay = 200;

				var op = dstBlob.StartCopyFromUri(srcBlob.Uri,
					destinationConditions: mode == BlobWriteMode.Overwrite ? default : new BlobRequestConditions { IfNoneMatch = ETag.All },
					cancellationToken: cancellationToken);
				if (op.HasCompleted) return true;
				cancellationToken.ThrowIfCancellationRequested();

				// Wait for copy to complete synchronously.
				while (true)
				{
					BlobProperties props = dstBlob.GetProperties(cancellationToken: cancellationToken);
					if (props.CopyStatus == CopyStatus.Success) return true;
					if (props.CopyStatus != CopyStatus.Pending) break;

					if (!cancellationToken.CanBeCanceled)
						Thread.Sleep(Delay);
					else if (cancellationToken.WaitHandle.WaitOne(Delay))
						cancellationToken.ThrowIfCancellationRequested();
				}
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch
			{
				// Ignore and fall back to download/upload
			}

			return false;
		}
	}

	public async Task CopyAsync(string source, string destination, BlobWriteMode mode = default, CancellationToken cancellationToken = default)
	{
		if (source is not { Length: > 0 }) throw new ArgumentNullException(nameof(source));
		if (destination is not { Length: > 0 }) throw new ArgumentNullException(nameof(destination));
		cancellationToken.ThrowIfCancellationRequested();

		if (mode == BlobWriteMode.Default)
			mode = BlobWriteMode.Create;

		var srcBlob = GetBlobClient(source);

		if (mode != BlobWriteMode.Append && await TryCopyBlobInternal(srcBlob, destination, mode, cancellationToken).ConfigureAwait(false))
			return;

#if NET
		await
#endif
		using var s = await srcBlob.OpenReadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
		await WriteAsync(destination, s, mode, cancellationToken).ConfigureAwait(false);


		async Task<bool> TryCopyBlobInternal(BlobClient srcBlob, string destination, BlobWriteMode mode, CancellationToken cancellationToken)
		{
			var dstBlob = GetBlobClient(destination);

			// If source is not accessible via URI for service-side copy, fall back to download/upload.
			// Try service-side copy first.
			try
			{
				const int Delay = 200;

				var op = await dstBlob.StartCopyFromUriAsync(srcBlob.Uri,
					destinationConditions: mode == BlobWriteMode.Overwrite ? default : new BlobRequestConditions { IfNoneMatch = ETag.All },
					cancellationToken: cancellationToken).ConfigureAwait(false);
				if (op.HasCompleted) return true;
				cancellationToken.ThrowIfCancellationRequested();

				// Wait for copy to complete synchronously.
				while (true)
				{
					BlobProperties props = await dstBlob.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
					if (props.CopyStatus != CopyStatus.Success) return true;
					if (props.CopyStatus != CopyStatus.Pending) break;

					await Task.Delay(Delay, cancellationToken).ConfigureAwait(false);
				}
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch
			{
				// Ignore and fall back to download/upload
			}

			return false;
		}
	}

	public void Move(string source, string destination, BlobWriteMode mode = default, CancellationToken cancellationToken = default)
	{
		Copy(source, destination, mode, cancellationToken);
		Delete(source, cancellationToken);
	}

	public async Task MoveAsync(string source, string destination, BlobWriteMode mode = default, CancellationToken cancellationToken = default)
	{
		await CopyAsync(source, destination, mode, cancellationToken).ConfigureAwait(false);
		await DeleteAsync(source, cancellationToken).ConfigureAwait(false);
	}

	public void Delete(string location, CancellationToken cancellationToken = default)
	{
		if (location is not { Length: >0 }) throw new ArgumentNullException(nameof(location));

		var blob = GetBlobClient(location);
		blob.DeleteIfExists(cancellationToken: cancellationToken);
	}

	public async Task DeleteAsync(string location, CancellationToken cancellationToken = default)
	{
		if (location is not { Length: >0 }) throw new ArgumentNullException(nameof(location));

		var blob = GetBlobClient(location);
		await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
	}

	public void Dispose()
	{
		if (_disposeContainer && _container is IDisposable d)
			d.Dispose();
		GC.SuppressFinalize(this);
	}

	private BlobClient GetBlobClient(string location) => _container.GetBlobClient(NormalizeKey(location));

	private static string NormalizeKey(string location) => location;

	private sealed class AzureBlobInfo: IBlobInfo
	{
		private readonly BlobClient _blob;

		public AzureBlobInfo(BlobClient blob, bool exists, long length, DateTime? lastModified)
		{
			_blob = blob;
			Exists = exists;
			Length = length;
			Path = blob.Name;
			LastModified = lastModified;
		}

		public bool Exists { get; }
		public long Length { get; }
		public string Path { get; }
		public DateTime? LastModified { get; }

		public Stream OpenReadStream()
			=> _blob.OpenRead();

		public Task<Stream> OpenReadStreamAsync(CancellationToken cancellationToken = default)
			=> _blob.OpenReadAsync(0, null, null, cancellationToken);
	}
}
