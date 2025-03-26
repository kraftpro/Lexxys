using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.Configuration;

public interface IConfigSourceFactory
{
	IConfigSource CreateConfiguration(Uri path, IReadOnlyCollection<string>? parameters = null);
}


public class ConfigSourceFactory: IConfigSourceFactory
{
	public IConfigSource CreateConfiguration(Uri path, IReadOnlyCollection<string>? parameters = null)
	{
		throw new NotImplementedException();
	}
}