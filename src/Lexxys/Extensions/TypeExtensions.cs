using System.Text;

namespace Lexxys;

public static class TypeExtensions
{

	/// <summary>
	/// Gets the full name of the specified type, including namespace if <paramref name="fullName"/> is true.
	/// </summary>
	/// <param name="type">The <see cref="Type"/> to get the name of.</param>
	/// <param name="fullName">If true, includes the namespace in the type name.</param>
	/// <returns>The name of the type.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
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

	private static StringBuilder BuildTypeName(StringBuilder text, Type type)
	{
		if (type.HasElementType)
			return type.IsArray ?
				BuildArrayTypeName(text, type):
				BuildTypeName(text, type.GetElementType() ?? typeof(void)).Append(type.IsPointer ? '*': '^');

		if (type.IsGenericParameter)
			return text.Append(type.Name);

		if (type.IsGenericType || type.IsGenericTypeDefinition)
		{
			var genericArguments = type.GetGenericArguments();
			return BuildGenericTypeName(text, type, genericArguments, genericArguments.Length);
		}

		if (type.DeclaringType != null)
			BuildTypeName(text, type.DeclaringType).Append('.');
		return text.Append(SimpleName(type));
	}

	private static StringBuilder BuildArrayTypeName(StringBuilder text, Type type)
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
		return text;
	}

	private static StringBuilder BuildGenericTypeName(StringBuilder text, Type type, Type[] args, int length)
	{
		int offset = 0;
		if (type.IsNested)
			offset = type.DeclaringType!.GetGenericArguments().Length;
		if (type.DeclaringType != null)
			BuildGenericTypeName(text, type.DeclaringType, args, offset).Append('.');

		var name = type.Name;
		int index = name.IndexOf('`');
		if (index < 0)
			return text.Append(SimpleName(type));

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
		for (int i = offset; i < length; ++i)
		{
			text.Append(c);
			BuildTypeName(text, args[i]);
			c = ',';
		}
		return text.Append(valueType ? ')' : '>');
	}

	private static string SimpleName(Type type) => __builtInTypes.TryGetValue(type, out var s) ? s : type.Name;

	private static readonly Dictionary<Type, string> __builtInTypes = new()
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
