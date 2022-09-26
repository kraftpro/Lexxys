using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexxys.Logging;
using Xml;

public class LogRecordJsonFormatter: ILogRecordFormatter
{
	private readonly NamingCaseRule _namingRule;

	public LogRecordJsonFormatter(): this(NamingCaseRule.None)
	{
	}

	public LogRecordJsonFormatter(XmlLiteNode config): this(config["namingRule"].AsEnum<NamingCaseRule>(NamingCaseRule.None))
	{
	}

	public LogRecordJsonFormatter(NamingCaseRule namingRule)
	{
		_namingRule = namingRule;
	}

	public void Format(TextWriter writer, LogRecord record)
	{
		record.ToJson(JsonBuilder.Create(writer).WithNamingRule(_namingRule));
	}
}
