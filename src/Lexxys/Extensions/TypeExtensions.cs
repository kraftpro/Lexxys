using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Extensions.Primitives;


#nullable enable

namespace Lexxys
{
	public static class TypeExtensions
	{
		public static string GetTypeName(this Type type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));
			if (type.HasElementType)
				return GetTypeName(type.GetElementType() ?? typeof(void)) + (type.IsArray ? "[]" : type.IsByRef ? "^" : "*");
			if (!type.IsGenericType)
				return __builtinTypes.TryGetValue(type, out var s) ? s : type.Name;
			var text = new StringBuilder();
			var name = type.Name;
			char c;
			if (name.StartsWith("ValueTuple`"))
			{
				c = '(';
			}
			else
			{
				c = '<';
				text.Append(type.Name.Substring(0, type.Name.IndexOf('`')));
			}
			foreach (var item in type.GetGenericArguments())
			{
				text.Append(c).Append(GetTypeName(item));
				c = ',';
			}
			text.Append(name.StartsWith("ValueTuple`") ? ')': '>');
			return text.ToString();
		}

		private static readonly Dictionary<Type, string> __builtinTypes = new()
		{
			{ typeof(void), "void" },
			{ typeof(bool), "bool" },
			{ typeof(byte), "byte" },
			{ typeof(sbyte), "sbyte" },
			{ typeof(char), "char" },
			{ typeof(short), "short" },
			{ typeof(ushort), "ushort" },
			{ typeof(int), "int" },
			{ typeof(uint), "uint" },
			{ typeof(long), "long" },
			{ typeof(ulong), "ulong" },
			{ typeof(float), "float" },
			{ typeof(double), "double" },
			{ typeof(decimal), "decimal" },
			{ typeof(string), "string" },
		};
	}
}
