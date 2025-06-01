using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Lexxys;

public class AzureBlobStorageService: IBlobStorageService
{
	private static readonly IReadOnlyCollection<string> _supportedSchemes = [Uri.UriSchemeHttp, Uri.UriSchemeHttps];
	private readonly BlobServiceClient _blobServiceClient;
	private readonly BlobContainerClient _blobContainerClient;

	public AzureBlobStorageService(string connectionString, string container)
	{
		_blobServiceClient = new BlobServiceClient(connectionString);
		_blobContainerClient = _blobServiceClient.GetBlobContainerClient(container);
	}

	public IReadOnlyCollection<string> SupportedSchemes => _supportedSchemes;

	public bool CanOpen(Uri location) => SupportedSchemes.Contains(location.Scheme);

	protected virtual BlobClient GetBlobClient(Uri location)
	{
		return _blobContainerClient.GetBlobClient(location.AbsolutePath.TrimStart('/'));
	}

	public IBlobInfo GetFileInfo(Uri location)
	{
		var x = SplitUrl(location);
		var blobClient = GetBlobClient(location);
		var properties = blobClient.GetProperties();
		return new AzureBlobInfo(location, properties);
	}

	private static (string? Container, string? Blob) SplitUrl(Uri location)
	{
		var scheme = location.Scheme;
		var parts = location.AbsolutePath.Split(['/'], 3, StringSplitOptions.None);
		return scheme != "azure" || parts.Length < 2 ? default:
			parts.Length < 3 ? (parts[1], null): (parts[1], parts[2]);
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
