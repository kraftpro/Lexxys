using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.Configuration
{
	internal interface IConfigProviderFactory
	{
		IReadOnlyList<string> SupportedTypes { get; }
		// ini, txt, xml, json
		IConfigProvider CreateProvider(string content, string type);
	}
}
