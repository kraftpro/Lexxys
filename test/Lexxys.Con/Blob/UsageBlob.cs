using System;
using System.Collections.Generic;
using System.Text;

namespace Lexxys.Con;

internal static class BlobUsage
{




}

public class Document
{
	private readonly IBlobStorageService _storageService;

	public long Id { get; set; }
	public string Name { get; set => field = value ?? throw new ArgumentNullException(nameof(value)); }

	public Document(string name, IBlobStorageService storageService)
	{
		Name = name ?? throw new ArgumentNullException(nameof(name));
		_storageService = storageService;
	}


	public Stream GetContentStream()
	{
		return Stream.Null;
	}

	public void UploadContent(Stream content)
	{
		if (content == null)
			throw new ArgumentNullException(nameof(content));

		_storageService.Write(GetBlobName(), content, BlobWriteMode.Overwrite);
	}

	private string GetBlobName()
	{
		return $"{Id}-{Name}";
	}
}