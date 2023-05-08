using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Lexxys.Xml;

public static partial class XmlTools
{
	public static T GetValue<T>(XmlLiteNode node)
	{
		if (node is null)
			throw new ArgumentNullException(nameof(node));

		return TryGetValue<T>(node, out var result) ? result : throw new FormatException(SR.FormatException(node.ToString().Left(1024), typeof(T)));
	}

	public static T GetValue<T>(XmlLiteNode node, T defaultValue)
	{
		if (node is null)
			throw new ArgumentNullException(nameof(node));

		return TryGetValue<T>(node, out var result) ? result : defaultValue;
	}

	public static bool TryGetValue<T>(XmlLiteNode? node, [MaybeNullWhen(false)] out T result)
	{
		if (TryGetValue(node, typeof(T), out var temp))
		{
			result = (T)temp!;
			return true;
		}
		result = default!;
		return false;
	}

	public static object? GetValue(XmlLiteNode node, Type returnType)
	{
		if (node is null)
			throw new ArgumentNullException(nameof(node));

		return TryGetValue(node, returnType, out var result) ? result : throw new FormatException(SR.FormatException(node.ToString().Left(1024), returnType));
	}

	public static bool TryGetValue(XmlLiteNode? node, Type returnType, out object? result)
	{
		if (returnType is null)
			throw new ArgumentNullException(nameof(returnType));

		if (node == null)
		{
			result = null;
			return false;
		}

		try
		{
			var typeName = node["$type"];
			Type? specifiedType = null;
			if (typeName != null)
				specifiedType = Factory.GetType(typeName) ??
					Factory.GetType(typeName + "Config") ??
					Factory.GetType(typeName + "Configuration") ??
					Factory.GetType(typeName + "Setting") ??
					Factory.GetType(typeName + "Parameter") ??
					Factory.GetType(typeName + "Option") ??
					Factory.GetType(typeName + "Configs") ??
					Factory.GetType(typeName + "Configurations") ??
					Factory.GetType(typeName + "Settings") ??
					Factory.GetType(typeName + "Parameters") ??
					Factory.GetType(typeName + "Options");
			return specifiedType is null || specifiedType == returnType || !returnType.IsAssignableFrom(specifiedType) ?
				TryGetValueFromNode(node, returnType, out result):
				TryGetValueFromNode(node, specifiedType, out result) || TryGetValueFromNode(node, returnType, out result);
		}
		catch (Exception e)
		{
			throw new FormatException(SR.CannotParseValue(node.ToString().Left(1024), returnType), e);
		}
	}

	private static bool TryGetValueFromNode(XmlLiteNode node, Type returnType, out object? result)
	{
		if (node.Attributes.Count == 0 && node.Elements.Count == 0 && __stringConditionalConstructor.TryGetValue(returnType, out ValueParser? stringParser))
			return stringParser(node.Value, out result);

		result = null;
		if (__nodeConditionalConstructor.TryGetValue(returnType, out TryGetNodeValue? nodeParser))
			return nodeParser(node, returnType, out result);

		if (returnType.IsGenericType && __nodeGenericConstructor.TryGetValue(returnType.GetGenericTypeDefinition(), out nodeParser))
		{
			__nodeConditionalConstructor[returnType] = nodeParser;
			return nodeParser(node, returnType, out result);
		}

		if (returnType.IsEnum)
			return TryGetValue(node.Value, returnType, out result);

		if (TestFromXmlLite(node, returnType, ref result))
			return true;
		if (TestFromXmlReader(node, returnType, ref result))
			return true;
		if (TestSerializer(node, returnType, ref result))
			return true;

		__nodeConditionalConstructor.TryAdd(returnType, TryReflection);
		return TryReflection(node, returnType, out result);
	}

	private static bool TestFromXmlLite(XmlLiteNode node, Type returnType, ref object? value)
	{
		var (type1, type2) = NullableTypes(returnType);

		var parser = GetFromXmlConstructor<XmlLiteNode>(type1) ?? GetFromXmlStaticConstructor<XmlLiteNode>(type1);
		if (parser == null)
			return false;

		__nodeConditionalConstructor.TryAdd(type1, (XmlLiteNode n, Type _, out object? r) =>
		{
			r = parser(n);
			return true;
		});
		if (type2 != null)
			__nodeConditionalConstructor.TryAdd(type2, (XmlLiteNode n, Type _, out object? r) =>
			{
				r = parser(n);
				return true;
			});

		value = parser(node);
		return true;
	}

	private static bool TestFromXmlReader(XmlLiteNode node, Type returnType, ref object? value)
	{
		var (type1, type2) = NullableTypes(returnType);
		var parser = GetFromXmlConstructor<XmlReader>(type1) ?? GetFromXmlStaticConstructor<XmlReader>(type1);

		if (parser == null)
			return false;

		__nodeConditionalConstructor.TryAdd(type1, (XmlLiteNode n, Type _, out object? r) =>
		{
			using XmlReader rdr = n.ReadSubtree();
			r = parser(rdr);
			return true;
		});
		if (type2 != null)
			__nodeConditionalConstructor.TryAdd(type2, (XmlLiteNode n, Type _, out object? r) =>
			{
				using XmlReader rdr = n.ReadSubtree();
				r = parser(rdr);
				return true;
			});

		using XmlReader reader = node.ReadSubtree();
		value = parser(reader);
		return true;
	}

	private static Func<T, object>? GetFromXmlConstructor<T>(Type type)
	{
		Func<T, object>? result = null;
		ConstructorInfo? ci = type.GetConstructor(new[] { typeof(T) });
		if (ci != null)
		{
			try
			{
				ParameterExpression a = Expression.Parameter(typeof(object), "arg");
				Expression e = Expression.Lambda<Func<T, object>>(Expression.New(ci, a), a);
				if (type.IsValueType)
					e = Expression.TypeAs(e, typeof(object));
				result = Expression.Lambda<Func<T, object>>(e).Compile();
			}
			catch (ArgumentException)
			{
			}
		}
		return result;
	}

	private static Func<T, object>? GetFromXmlStaticConstructor<T>(Type type)
	{
		try
		{
			IEnumerable<MethodInfo> methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

			foreach (MethodInfo method in methods)
			{
				if (type == Factory.NullableTypeBase(method.ReturnType))
				{
					if (method.Name is "Create" or "TryCreate" or "FromXml")
					{
						ParameterInfo[] parameters = method.GetParameters();
						if (parameters.Length == 1)
						{
							if (parameters[0].ParameterType == typeof(T))
							{
								ParameterExpression arg = Expression.Parameter(typeof(T), "arg");
								Expression e = Expression.Call(method, arg);
								if (type.IsValueType)
									e = Expression.TypeAs(e, typeof(object));
								return Expression.Lambda<Func<T, object>>(e, arg).Compile();
							}
						}
					}
				}
			}
		}
		catch (ArgumentException)
		{
		}
		return null;
	}

	private static bool TestSerializer(XmlLiteNode node, Type returnType, ref object? result)
	{
		if (!returnType.IsPublic)
			return false;
		if (returnType.IsInterface || !returnType.IsAbstract && returnType.GetCustomAttributes(typeof(XmlRootAttribute), true).Length == 0 && returnType.GetCustomAttributes(typeof(XmlTypeAttribute), true).Length == 0)
			return false;
		try
		{
			var xs = new XmlSerializer(returnType);
			using XmlReader reader = node.ReadSubtree();
			if (!xs.CanDeserialize(reader))
				return false;

			result = xs.Deserialize(reader);

			var (type1, type2) = NullableTypes(returnType);
			__nodeConditionalConstructor.TryAdd(type1, Parser);
			if (type2 != null)
				__nodeConditionalConstructor.TryAdd(type2, Parser);

			return true;
		}
		catch (InvalidOperationException)
		{
		}
		return false;

		static bool Parser(XmlLiteNode node, Type type, out object? result)
		{
			try
			{
				var xs = new XmlSerializer(type);
				using XmlReader reader = node.ReadSubtree();
				result = xs.Deserialize(reader);
				return true;
			}
			catch (InvalidOperationException)
			{
			}
			result = null;
			return false;
		}
	}

	#region Predefined Parsers

	/// <summary>
	/// Parse Key/Value pair
	/// </summary>
	/// <param name="node">XmlLiteNode to get value from</param>
	/// <param name="returnType">Concrete type of KeyValue pair structure</param>
	/// <param name="result">Parsed Value</param>
	/// <returns>True if success</returns>
	/// <remarks>
	/// supported nodes structure:
	///		node
	///			:key	value_of_key
	///			:value	value_of_value
	///		
	///		key	value
	///			
	///		key
	///			value
	///	
	///		node		value_of_value
	///			:key	value_of_key
	///
	///		node
	///			:key	value_of_key
	///			value
	///
	///		node
	///			key
	///			value
	/// </remarks>
	private static bool TryKeyValuePair(XmlLiteNode node, Type returnType, out object? result)
	{
		if (node == null)
			throw new ArgumentNullException(nameof(node));
		if (returnType == null)
			throw new ArgumentNullException(nameof(returnType));
		if (!returnType.IsGenericType || returnType.GetGenericTypeDefinition() != typeof(KeyValuePair<,>))
			throw new ArgumentOutOfRangeException(nameof(returnType), returnType, null);

		result = null;

		Type[] typeArgs = returnType.GetGenericArguments();
		Type keyType = typeArgs[0];
		Type valueType = typeArgs[1];
		object? parsedKey;
		object? parsedValue;

		if (node.Attributes.Count == 0)
		{
			XmlLiteNode? nodeKey = node.FirstOrDefault("key");
			XmlLiteNode? nodeValue = node.FirstOrDefault("value");
			// <item><key>Key</key><value>Value</value></item>
			if (node.Elements.Count == 2 && nodeKey != null && nodeValue != null)
			{
				if (!TryGetValue(nodeKey, keyType, out parsedKey))
					return false;
				if (!TryGetValue(nodeValue, valueType, out parsedValue))
					return false;
			}
			// <Key ...>
			else
			{
				if (!TryGetValue(node.Name, keyType, out parsedKey))
					return false;
				// <Key value="Value" />
				if (node.Elements.Count == 1 && nodeValue != null)
				{
					if (!TryGetValue(nodeValue, valueType, out parsedValue))
						return false;
				}
				// <Key>Value</Key>
				else
				{
					if (!TryGetValue(node, valueType, out parsedValue))
						return false;
				}
			}

		}
		// <item key="Key" value="Value" />
		// <Key ... />
		else if (node.Attributes.Count == 2 && node.Elements.Count == 0)
		{
			if (!TryGetValue(node["key"] ?? node.Name, keyType, out parsedKey))
				return false;
			if (node["value"] == null)
			{
				if (!TryGetValue(node, valueType, out parsedValue))
					return false;
			}
			else
			{
				if (!TryGetValue(node["value"], valueType, out parsedValue))
					return false;
			}
		}
		// <Key ...> ... </Key>
		// <item key="Key" ...> ... </item>
		else
		{
			if (!TryGetValue(node["key"] ?? node.Name, keyType, out parsedKey))
				return false;

			XmlLiteNode? nodeValue;
			// <... ><value>Value</value></...>
			if (node.Elements.Count == 1 && (nodeValue = node.FirstOrDefault("value")) != null)
			{
				if (!TryGetValue(nodeValue, valueType, out parsedValue))
					return false;
			}
			// <... >Value</...>
			else
			{
				if (!TryGetValue(node, valueType, out parsedValue))
					return false;
			}
		}
		ConstructorInfo constructor = returnType.GetConstructor(typeArgs) ?? throw new InvalidOperationException(SR.CannotFindConstructor(returnType, typeArgs));
		result = Factory.Invoke(constructor, parsedKey, parsedValue);
		return result != null;
	}

	private static bool TryNullable(XmlLiteNode node, Type returnType, out object? result)
	{
		if (node == null)
			throw new ArgumentNullException(nameof(node));
		if (returnType == null)
			throw new ArgumentNullException(nameof(returnType));
		if (!returnType.IsGenericType || returnType.GetGenericTypeDefinition() != typeof(Nullable<>))
			throw new ArgumentOutOfRangeException(nameof(returnType), returnType, null);

		if (node.Attributes.Count == 0 && node.Elements.Count == 0 && node.Value.Length == 0)
		{
			result = null!;
			return true;
		}
		return TryGetValue(node, returnType.GetGenericArguments()[0], out result);
	}

	private delegate bool TryGetNodeValue(XmlLiteNode node, Type returnType, out object? result);

	private static bool TryCopy(XmlLiteNode node, Type returnType, out object result)
	{
		result = node;
		return true;
	}

	private static readonly ConcurrentDictionary<Type, TryGetNodeValue> __nodeConditionalConstructor = new ConcurrentDictionary<Type, TryGetNodeValue>(
		new[]
		{
			new KeyValuePair<Type, TryGetNodeValue>(typeof(XmlLiteNode), TryCopy)
		});

	private static readonly Dictionary<Type, TryGetNodeValue> __nodeGenericConstructor =
		new Dictionary<Type, TryGetNodeValue>
		{
			{ typeof(KeyValuePair<,>), TryKeyValuePair },
			{ typeof(Nullable<>), TryNullable }
		};

	#endregion
}
