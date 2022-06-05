using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace Lexxys.Logging
{
	using Xml;

	public class LogRecordJsonFormatter : ILogRecordFormatter
	{
		private NamingCaseRule _namingRule;

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

		public TextWriter Format(TextWriter writer, LogRecord? record)
		{
			record?.ToJson(JsonBuilder.Create(writer));
			return writer;
		}
	}
}
