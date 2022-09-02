// Lexxys Infrastructural library.
// file: IDumpJson.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Lexxys;

namespace Lexxys
{
	public interface IDumpJson
	{
		JsonBuilder ToJsonContent(JsonBuilder json);
	}

	public static class ToJsonExtensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static JsonBuilder ToJson(this IDumpJson? obj, JsonBuilder json)
		{
			if (json is null)
				throw new ArgumentNullException(nameof(json));
			return obj?.ToJsonContent(json.Obj()).End() ?? json;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static StringBuilder ToJson(this IDumpJson? obj, StringBuilder text)
		{
			Contract.Ensures(Contract.Result<StringBuilder>() != null);
			if (text is null)
				throw new ArgumentNullException(nameof(text));
			if (obj == null)
				return text;
			obj.ToJson(new JsonStringBuilder(text)).Flush();
			return text;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ToJson(this IDumpJson? obj)
		{
			Contract.Ensures(Contract.Result<string>() != null);
			return obj?.ToJson(new JsonStringBuilder()).ToString() ?? String.Empty;
		}
	}
}


