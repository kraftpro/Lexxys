using Amazon.S3;
using Amazon.S3.Model;

namespace Lexxys;

public class AmazonBlobStorageService: IBlobStorageService
{
	private static readonly IReadOnlyCollection<string> __supportedSchemes = ["s3"];
	private readonly AmazonS3Client _client;

    public AmazonBlobStorageService(AmazonS3Config clientConfig)
    {
		if (clientConfig is null) throw new ArgumentNullException(nameof(clientConfig));

		_client = new AmazonS3Client(clientConfig);
	}

    public IReadOnlyCollection<string> SupportedSchemes => __supportedSchemes;

    public bool CanOpen(Uri uri)
    {
		if (uri == null) throw new ArgumentNullException(nameof(uri));
        return SupportedSchemes.Contains(uri.Scheme);
    }

	public IBlobInfo GetFileInfo(Uri uri) => GetFileInfoAsync(uri).GetAwaiter().GetResult();

	public async Task<IBlobInfo> GetFileInfoAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        if (uri == null) throw new ArgumentNullException(nameof(uri));

        try
        {
            var response = await _client.GetObjectMetadataAsync(new GetObjectMetadataRequest
			{
				BucketName = uri.Host,
				Key = uri.AbsolutePath.TrimStart('/')
			}, cancellationToken);
            return new BlobFileInfo(_client, uri, response.ContentLength, response.LastModified);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new BlobFileInfo(_client, uri, 0, default);
		}
    }

    public void WriteFile(Uri uri, Stream content, bool overwrite) => WriteFileAsync(uri, content, overwrite).GetAwaiter().GetResult();

	public async Task WriteFileAsync(Uri uri, Stream content, bool overwrite, CancellationToken cancellationToken = default)
    {
        if (uri == null) throw new ArgumentNullException(nameof(uri));
        if (content == null) throw new ArgumentNullException(nameof(content));

        await _client.PutObjectAsync(new PutObjectRequest
		{
			BucketName = uri.Host,
			Key = uri.AbsolutePath.TrimStart('/'),
			InputStream = content
		}, cancellationToken);
    }

    public void CopyFile(Uri source, Uri destination) => CopyFileAsync(source, destination).GetAwaiter().GetResult();

	public async Task CopyFileAsync(Uri source, Uri destination, CancellationToken cancellationToken = default)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (destination == null) throw new ArgumentNullException(nameof(destination));

        await _client.CopyObjectAsync(new CopyObjectRequest
		{
			SourceBucket = source.Host,
			SourceKey = source.AbsolutePath.TrimStart('/'),
			DestinationBucket = destination.Host,
			DestinationKey = destination.AbsolutePath.TrimStart('/')
		}, cancellationToken);
    }

    public void MoveFile(Uri source, Uri destination) => MoveFileAsync(source, destination).GetAwaiter().GetResult();

	public async Task MoveFileAsync(Uri source, Uri destination, CancellationToken cancellationToken = default)
    {
        await CopyFileAsync(source, destination, cancellationToken);
        await DeleteFileAsync(source, cancellationToken);
    }

    public void DeleteFile(Uri uri) => DeleteFileAsync(uri).GetAwaiter().GetResult();

	public async Task DeleteFileAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        if (uri == null) throw new ArgumentNullException(nameof(uri));

        await _client.DeleteObjectAsync(new DeleteObjectRequest
		{
			BucketName = uri.Host,
			Key = uri.AbsolutePath.TrimStart('/')
		}, cancellationToken);
    }

	public void Dispose()
	{
		_client.Dispose();
		GC.SuppressFinalize(this);
	}

	private class BlobFileInfo(AmazonS3Client client, Uri uri, long length, DateTime? lastModified): IBlobInfo
	{
		private readonly Uri _uri = uri ?? throw new ArgumentNullException(nameof(uri));

		public bool Exists => length == 0 && lastModified == default;

		public long Length => length;

		public string Path => _uri.ToString();

		public DateTimeOffset? LastModified => lastModified;

		public Stream OpenReadStream() => OpenReadStreamAsync().GetAwaiter().GetResult();

		public async Task<Stream> OpenReadStreamAsync()
		{
			var response = await client.GetObjectAsync(new GetObjectRequest
			{
				BucketName = _uri.Host,
				Key = _uri.AbsolutePath.TrimStart('/')
			});
			return response.ResponseStream;
		}
	}
}
