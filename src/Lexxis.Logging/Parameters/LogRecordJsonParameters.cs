using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Lexxys.Logging;

public class LogRecordJsonParameters: ILogRecordFormatterParameters
{
	public LogRecordJsonParameters()
	{
	}
	
	public LogRecordJsonParameters(NamingCaseRule naming)
	{
		Naming = naming;
	}

	public NamingCaseRule Naming { get; set; }

	public ILogRecordFormatter CreateFormatter() => new LogRecordJsonFormatter(Naming);
}