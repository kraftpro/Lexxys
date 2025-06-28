using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.Configuration.New;
internal interface IConfigService
{
	int AddConfiguration(IConfigSource provider, int priority = 0);
	IConfigSource Build();
}


