using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lexxys
{
	public static class TypeExtensions
	{
		public static string GetTypeName(this Type type, bool fullName = false)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));
			var text = new StringBuilder();
			if (fullName)
				text.Append(type.Namespace).Append('.');
			BuildTypeName(text, type);
			return text.ToString();
		}

		static void BuildTypeName(StringBuilder text, Type type)
		{
			if (type.HasElementType)
			{
				if (type.IsArray)
				{
					BuildArrayTypeName(text, type);
					return;
				}
				BuildTypeName(text, type.GetElementType() ?? typeof(void));
				text.Append(type.IsPointer ? '*': '^');
				return;
			}

			if (type.IsGenericParameter)
			{
				text.Append(type.Name);
				return;
			}

			if (type.IsGenericType || type.IsGenericTypeDefinition)
			{
				var genericArguments = type.GetGenericArguments();
				BuildGenericTypeName(text, type, genericArguments, genericArguments.Length);
				return;
			}

			if (type.DeclaringType != null)
			{
				BuildTypeName(text, type.DeclaringType);
				text.Append('.');
			}

			text.Append(SimpleName(type));
			return;
		}

		private static void BuildArrayTypeName(StringBuilder text, Type type)
		{
			Type elementType = type;
			while (elementType.IsArray)
			{
				elementType = elementType.GetElementType()!;
			}
			BuildTypeName(text, elementType);
			while (type.IsArray)
			{
				text.Append('[');
				text.Append(',', type.GetArrayRank() - 1);
				text.Append(']');
				type = type.GetElementType()!;
			}
		}

		private static void BuildGenericTypeName(StringBuilder text, Type type, Type[] args, int lenth)
		{
			int offset = 0;
			if (type.IsNested)
				offset = type.DeclaringType!.GetGenericArguments().Length;
			if (type.DeclaringType != null)
			{
				BuildGenericTypeName(text, type.DeclaringType, args, offset);
				text.Append('.');
			}

			var name = type.Name;
			int index = name.IndexOf('`');
			if (index < 0)
			{
				text.Append(SimpleName(type));
				return;
			}
			char c;
			bool valueType = name.StartsWith("ValueTuple`", StringComparison.Ordinal);
			if (valueType)
			{
				c = '(';
			}
			else
			{
				c = '<';
				text.Append(name, 0, index);
			}
			for (int i = offset; i < lenth; ++i)
			{
				text.Append(c);
				BuildTypeName(text, args[i]);
				c = ',';
			}
			text.Append(valueType ? ')' : '>');
		}

		private static string SimpleName(Type type) => __builtinTypes.TryGetValue(type, out var s) ? s : type.Name;

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
