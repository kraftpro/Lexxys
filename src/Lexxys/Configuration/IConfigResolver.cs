using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.Configuration;
internal interface IConfigResolver
{
	IConfigSource Resolve(Uri path, IReadOnlyCollection<string>? parameters = null);
}

//internal class ConfigResolver: IConfigResolver
//{
//	private readonly IConfigService _service;

//	public ConfigResolver(IConfigService service)
//	{
//		_service = service;
//	}

//	public IConfigSource Resolve(Uri path, IReadOnlyCollection<string>? parameters = null)
//	{
//		if (path is null) throw new ArgumentNullException(nameof(path));
//		if (path.IsAbsoluteUri)
//			return new ConfigSource(path, parameters);
//		if (path.IsFile)
//			return new ConfigFileSource(path.LocalPath, parameters);
//		if (path.IsUnc)
//			return new ConfigUncSource(path.LocalPath, parameters);
//		if (path.IsLoopback)
//			return new ConfigLoopbackSource(path.LocalPath, parameters);
//		if (path.IsWellFormedOriginalString())
//			return new ConfigHttpSource(path, parameters);
//		return new ConfigSource(path, parameters);
//	}
//}
