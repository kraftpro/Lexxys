// Lexxys Infrastructural library.
// file: IDump.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Text;

namespace Lexxys;

public interface IDump
{
	DumpWriter DumpContent(DumpWriter writer);
}

public interface IDumpValue: IDump
{
}

public static class DumpExtensions
{
	public static DumpWriter Dump(this IDump obj, DumpWriter writer)
	{
		if (obj is null)
			throw new ArgumentNullException(nameof(obj));
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));

		return obj is IDumpValue ? obj.DumpContent(writer): obj.DumpContent(writer.Text('{')).Text('}');
	}

	public static string Dump(this IDump? obj, int maxLength = 0, int maxDepth = 0, int stringLimit = 0, int blobLimit = 0, int arrayLimit = 0, bool pretty = false, string? tab = null)
	{
		if (obj == null)
			return DumpWriter.NullValue;
		var text = new StringBuilder();
		obj.Dump(DumpWriter.Create(text, maxLength, maxDepth, stringLimit, blobLimit, arrayLimit).Pretty(pretty, tab));
		return text.ToString();
	}

	public static StringBuilder Dump(this IDump? obj, StringBuilder text, int maxLength = 0, int maxDepth = 0, int stringLimit = 0, int blobLimit = 0, int arrayLimit = 0, bool pretty = false, string? tab = null)
	{
		if (text is null)
			throw new ArgumentNullException(nameof(text));
		if (obj == null)
			return text.Append(DumpWriter.NullValue);
		obj.Dump(DumpWriter.Create(text, maxLength, maxDepth, stringLimit, blobLimit, arrayLimit).Pretty(pretty, tab));
		return text;
	}

	#region Dump with name

	public static DumpWriter Dump(this IDump? obj, DumpWriter writer, string name)
	{
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));
		if (obj == null)
			return writer.Text(name).Text('=').Text(DumpWriter.NullValue);
		else
			return obj.Dump(writer.Text(name).Text('='));
	}

	public static string Dump(this IDump? obj, string name, int maxLength = 0, int maxDepth = 0, int stringLimit = 0, int blobLimit = 0, int arrayLimit = 0, bool pretty = false, string? tab = null)
	{
		if (obj == null)
			return DumpWriter.NullValue;
		var text = new StringBuilder();
		obj.Dump(DumpWriter.Create(text, maxLength, maxDepth, stringLimit, blobLimit, arrayLimit).Pretty(pretty, tab), name);
		return text.ToString();
	}

	public static StringBuilder Dump(this IDump? obj, StringBuilder text, string name, int maxLength = 0, int maxDepth = 0, int stringLimit = 0, int blobLimit = 0, int arrayLimit = 0, bool pretty = false, string? tab = null)
	{
		if (text is null)
			throw new ArgumentNullException(nameof(text));
		if (obj == null)
			return text.Append(DumpWriter.NullValue);
		obj.Dump(DumpWriter.Create(text, maxLength, maxDepth, stringLimit, blobLimit, arrayLimit).Pretty(pretty, tab), name);
		return text;
	}

	#endregion
}


