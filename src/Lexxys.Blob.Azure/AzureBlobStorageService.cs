using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Lexxys;

public class AzureBlobStorageService: IBlobStorageService
{
	private static readonly IReadOnlyCollection<string> _supportedSchemes = [Uri.UriSchemeHttp, Uri.UriSchemeHttps];
	private readonly BlobServiceClient _blobServiceClient;

	public AzureBlobStorageService(string connectionString)
	{
		_blobServiceClient = new BlobServiceClient(connectionString);
	}

	public IReadOnlyCollection<string> SupportedSchemes => _supportedSchemes;

	public bool CanOpen(Uri location)
	{
		return SupportedSchemes.Contains(location.Scheme);
	}

	public IBlobInfo GetFileInfo(Uri location)
	{
		var blobClient = GetBlobClient(location);
		var properties = blobClient.GetProperties();
		return new AzureBlobInfo(location, properties);
	}

	public async Task<IBlobInfo> GetFileInfoAsync(Uri location, CancellationToken cancellationToken = default)
	{
		var blobClient = GetBlobClient(location);
		var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
		return new AzureBlobInfo(location, properties);
	}

	public void WriteFile(Uri location, Stream stream, bool overwrite)
	{
		var blobClient = GetBlobClient(location);
		blobClient.Upload(stream, overwrite);
	}

	public async Task WriteFileAsync(Uri location, Stream stream, bool overwrite, CancellationToken cancellationToken = default)
	{
		var blobClient = GetBlobClient(location);
		await blobClient.UploadAsync(stream, overwrite, cancellationToken);
	}

	public void CopyFile(Uri source, Uri destination)
	{
		var sourceBlobClient = GetBlobClient(source);
		var destinationBlobClient = GetBlobClient(destination);
		destinationBlobClient.SyncCopyFromUri(sourceBlobClient.Uri);
	}

	public async Task CopyFileAsync(Uri source, Uri destination, CancellationToken cancellationToken = default)
	{
		var sourceBlobClient = GetBlobClient(source);
		var destinationBlobClient = GetBlobClient(destination);
		await destinationBlobClient.SyncCopyFromUriAsync(sourceBlobClient.Uri, cancellationToken: cancellationToken);
	}

	public void MoveFile(Uri source, Uri destination)
	{
		CopyFile(source, destination);
		DeleteFile(source);
	}

	public async Task MoveFileAsync(Uri source, Uri destination, CancellationToken cancellationToken = default)
	{
		await CopyFileAsync(source, destination, cancellationToken);
		await DeleteFileAsync(source, cancellationToken);
	}

	public void DeleteFile(Uri location)
	{
		var blobClient = GetBlobClient(location);
		blobClient.DeleteIfExists();
	}

	public async Task DeleteFileAsync(Uri location, CancellationToken cancellationToken = default)
	{
		var blobClient = GetBlobClient(location);
		await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
	}

	public void Dispose()
	{
		// Dispose resources if necessary
	}

	private BlobClient GetBlobClient(Uri location)
	{
		var blobContainerClient = _blobServiceClient.GetBlobContainerClient(location.Host);
		return blobContainerClient.GetBlobClient(location.AbsolutePath.TrimStart('/'));
	}

	private class AzureBlobInfo: IBlobInfo
	{
		public AzureBlobInfo(Uri location, BlobProperties properties)
		{
			Path = location.ToString();
			Exists = true;
			Length = properties.ContentLength;
			LastModified = properties.LastModified;
		}

		public bool Exists { get; }
		public long Length { get; }
		public string Path { get; }
		public DateTimeOffset? LastModified { get; }

		public Stream OpenReadStream()
		{
			var blobClient = new BlobClient(new Uri(Path));
			return blobClient.OpenRead();
		}

		public async Task<Stream> OpenReadStreamAsync()
		{
			var blobClient = new BlobClient(new Uri(Path));
			return await blobClient.OpenReadAsync();
		}
	}
}
