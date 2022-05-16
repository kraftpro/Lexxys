using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace Lexxys.Configuration
{
	public interface IConfigService: IConfigSource
	{
		void AddConfigurationProvider(IConfigProvider provider, bool tail = false);
	}
}
