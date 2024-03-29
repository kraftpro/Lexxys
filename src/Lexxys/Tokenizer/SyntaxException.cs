// Lexxys Infrastructural library.
// file: SyntaxException.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Runtime.Serialization;
#if NETFRAMEWORK
using System.Security.Permissions;
#endif

namespace Lexxys.Tokenizer
{
	[Serializable]
	public class SyntaxException: Exception
	{
		public SyntaxException()
			: base(SR.SyntaxException())
		{
		}

		public SyntaxException(string message)
			: base(message ?? SR.SyntaxException())
		{
		}

		public SyntaxException(string message, Exception exception)
			: base(message ?? SR.SyntaxException(), exception)
		{
		}

		public SyntaxException(string message, string file, int line, int column)
			: base(message ?? SR.SyntaxException())
		{
			Line = line;
			Column = column;
			File = file;
		}

		public SyntaxException(string message, string file, int line, int column, Exception exception)
			: base(message ?? SR.SyntaxException(), exception)
		{
			Line = line;
			Column = column;
			File = file;
		}

		protected SyntaxException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			File = (string)info.GetValue("file", typeof(string));
			Line = (int)info.GetValue("line", typeof(int));
			Column = (int)info.GetValue("column", typeof(int));
		}

		public string File { get; }
		public int Line { get; }
		public int Column { get; }

		public override string Message
		{
			get
			{
				string format = File == null ?
					(Line <= 0 ? "{0}":
					Column <= 0 ? "({2}): {0}": "({2},{3}): {0}"):
					(Line <= 0 ? "{1}: {0}":
					Column <= 0 ? "{1}({2}): {0}": "{1}({2},{3}): {0}");
				return String.Format(CultureInfo.InvariantCulture, format, base.Message, File, Line, Column);
			}
		}

#if NETFRAMEWORK
		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
#endif
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
				throw new ArgumentNullException(nameof(info));
			base.GetObjectData(info, context);
			info.AddValue("file", File);
			info.AddValue("line", Line);
			info.AddValue("column", Column);
		}
	}
}


