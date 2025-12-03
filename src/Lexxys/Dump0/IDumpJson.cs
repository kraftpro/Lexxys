// Lexxys Infrastructural library.
// file: IDumpJson.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Text;

namespace Lexxys;

[Obsolete("Use IDump instead.")]
public interface IDumpJson
{
	JsonBuilder ToJsonContent(JsonBuilder json);
}

public static class ToJsonExtensions
{
	extension(IDumpJson? obj)
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public JsonBuilder ToJson(JsonBuilder json)
		{
			if (json is null) throw new ArgumentNullException(nameof(json));
			return obj?.ToJsonContent(json.Obj()).End() ?? json;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public StringBuilder ToJson(StringBuilder text)
		{
			if (text is null) throw new ArgumentNullException(nameof(text));
			if (obj == null) return text;
			obj.ToJson(new JsonStringBuilder(text)).Flush();
			return text;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public string ToJson()
		{
			return obj?.ToJson(new JsonStringBuilder()).ToString() ?? String.Empty;
		}
	}
}


