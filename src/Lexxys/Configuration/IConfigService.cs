using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.Configuration
{
	public interface IConfigService: IConfigSource
	{
		bool AddConfiguration(Uri location, IReadOnlyCollection<string>? parameters = null, bool tail = false);
		bool AddConfiguration(IConfigProvider provider, bool tail = false);
	}
}
