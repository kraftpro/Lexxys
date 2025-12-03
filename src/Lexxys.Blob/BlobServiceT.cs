namespace Lexxys;

internal class BlobService<T>(IBlobStorageService service): IBlobStorageService<T>
{
	private readonly IBlobStorageService _service = service;

	public IBlobInfo GetBlobInfo(string location, CancellationToken cancellationToken = default) => _service.GetBlobInfo(location, cancellationToken);

	public Stream Read(string location, CancellationToken cancellationToken = default) => _service.Read(location, cancellationToken);

	public void Write(string location, Stream stream, BlobWriteMode mode = default, CancellationToken cancellationToken = default) => _service.Write(location, stream, mode, cancellationToken);
	public void Copy(string source, string destination, BlobWriteMode mode = default, CancellationToken cancellationToken = default) => _service.Copy(source, destination, mode, cancellationToken);

	public void Move(string source, string destination, BlobWriteMode mode = default, CancellationToken cancellationToken = default) => _service.Move(source, destination, mode, cancellationToken);

	public void Delete(string location, CancellationToken cancellationToken = default) => _service.Delete(location, cancellationToken);

	public Task<IBlobInfo> GetBlobInfoAsync(string location, CancellationToken cancellationToken = default) => _service.GetBlobInfoAsync(location, cancellationToken);

	public Task<Stream> ReadAsync(string location, CancellationToken cancellationToken = default) => _service.ReadAsync(location, cancellationToken);

	public Task WriteAsync(string location, Stream stream, BlobWriteMode mode = default, CancellationToken cancellationToken = default) => _service.WriteAsync(location, stream, mode, cancellationToken);

	public Task CopyAsync(string source, string destination, BlobWriteMode mode = default, CancellationToken cancellationToken = default) => _service.CopyAsync(source, destination, mode, cancellationToken);

	public Task MoveAsync(string source, string destination, BlobWriteMode mode = default, CancellationToken cancellationToken = default) => _service.MoveAsync(source, destination, mode, cancellationToken);

	public Task DeleteAsync(string location, CancellationToken cancellationToken = default) => _service.DeleteAsync(location, cancellationToken);

	public void Dispose()
	{
		_service.Dispose();
		GC.SuppressFinalize(this);
	}
}


internal class BlobService(IBlobStorageService service, Func<string, string> formatter): IBlobStorageService
{
	private readonly IBlobStorageService _service = service ?? throw new ArgumentNullException(nameof(service));
	private readonly Func<string, string> _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));

	public void Copy(string source, string destination, BlobWriteMode mode = default, CancellationToken cancellationToken = default) => _service.Copy(_formatter(source), _formatter(destination), mode, cancellationToken);

	public Task CopyAsync(string source, string destination, BlobWriteMode mode = default, CancellationToken cancellationToken = default) => _service.CopyAsync(_formatter(source), _formatter(destination), mode, cancellationToken);

	public void Delete(string location, CancellationToken cancellationToken = default) => _service.Delete(_formatter(location), cancellationToken);

	public Task DeleteAsync(string location, CancellationToken cancellationToken = default) => _service.DeleteAsync(_formatter(location), cancellationToken);

	public IBlobInfo GetBlobInfo(string location, CancellationToken cancellationToken = default) => _service.GetBlobInfo(_formatter(location), cancellationToken);

	public Task<IBlobInfo> GetBlobInfoAsync(string location, CancellationToken cancellationToken = default) => _service.GetBlobInfoAsync(_formatter(location), cancellationToken);

	public void Move(string source, string destination, BlobWriteMode mode = default, CancellationToken cancellationToken = default) => _service.Move(_formatter(source), _formatter(destination), mode, cancellationToken);
	public Task MoveAsync(string source, string destination, BlobWriteMode mode = default, CancellationToken cancellationToken = default) => _service.MoveAsync(_formatter(source), _formatter(destination), mode, cancellationToken);

	public Stream Read(string location, CancellationToken cancellationToken = default) => _service.Read(_formatter(location), cancellationToken);

	public Task<Stream> ReadAsync(string location, CancellationToken cancellationToken = default) => _service.ReadAsync(_formatter(location), cancellationToken);

	public void Write(string location, Stream stream, BlobWriteMode mode, CancellationToken cancellationToken = default) => _service.Write(_formatter(location), stream, mode, cancellationToken);

	public Task WriteAsync(string location, Stream stream, BlobWriteMode mode, CancellationToken cancellationToken = default) => _service.WriteAsync(_formatter(location), stream, mode, cancellationToken);

	public void Dispose()
	{
		_service.Dispose();
		GC.SuppressFinalize(this);
	}
}