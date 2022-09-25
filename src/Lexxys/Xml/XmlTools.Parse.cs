using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Lexxys.Xml
{
	public static partial class XmlTools
	{
		[return: NotNullIfNotNull("defaultValue")]
		public static object? GetValue(string? value, Type type, object? defaultValue)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			return TryGetValue(value, type, out object? result) ? result : defaultValue;
		}

		public static object GetValue(string value, Type type)
		{
			if (value is null || value.Length <= 0)
				throw new ArgumentNullException(nameof(value));
			if (type == null)
				throw new ArgumentNullException(nameof(type));
			return TryGetValue(value, type, out object? result) ? result! : throw new FormatException(SR.FormatException(value));
		}

		[return: NotNullIfNotNull("defaultValue")]
		public static T GetValue<T>(string? value, T defaultValue)
		{
			return TryGetValue<T>(value, out var result) ? result : defaultValue;
		}

		public static T GetValue<T>(string value)
		{
			if (value is null || value.Length <= 0)
				throw new ArgumentNullException(nameof(value));
			return TryGetValue<T>(value, out var result) ? result : throw new FormatException(SR.FormatException(value));
		}

		public static bool TryGetValue<T>(string? value, [MaybeNullWhen(false)] out T result)
		{
			if (TryGetValue(value, typeof(T), out var temp))
			{
				result = (T)temp;
				return true;
			}
			result = default;
			return false;
		}

		public static object? GetValueOrDefault(string? value, Type returnType)
		{
			if (returnType == null)
				throw new ArgumentNullException(nameof(returnType));

			return TryGetValue(value, returnType, out object? result) ? result : Factory.DefaultValue(returnType);
		}

		public static bool TryGetValue(string? value, Type returnType, [MaybeNullWhen(false)] out object result)
		{
			if (returnType == null)
				throw new ArgumentNullException(nameof(returnType));

			if (value == null || __missingConverters.ContainsKey(returnType))
			{
				result = Factory.DefaultValue(returnType);
				return false;
			}

			if (__stringConditionalConstructor.TryGetValue(returnType, out ValueParser? parser))
				return parser(value, out result);

			if (String.IsNullOrWhiteSpace(value))
			{
				result = Factory.DefaultValue(returnType);
				return false;
			}

			if (returnType.IsEnum || returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Nullable<>) && returnType.GetGenericArguments()[0].IsEnum)
			{
				bool nullable = returnType.IsGenericType;
				Type type1 = nullable ? returnType.GetGenericArguments()[0] : returnType;
				Type type2 = nullable ? returnType : typeof(Nullable<>).MakeGenericType(returnType);
				__stringConditionalConstructor.TryAdd(type1, (string x, [MaybeNullWhen(false)] out object y) => TryGetEnum(x, type1, out y));
				__stringConditionalConstructor.TryAdd(type2, (string x, [MaybeNullWhen(false)] out object y) =>
				{
					if (String.IsNullOrWhiteSpace(x))
					{
						y = null;
						return false;
					}
					return TryGetEnum(x, type1, out y);
				});

				if (String.IsNullOrWhiteSpace(value))
				{
					result = null;
					return nullable;
				}
				return TryGetEnum(value, type1, out result);
			}

			TypeConverter? converter = GetTypeConverter(returnType);
			if (converter != null)
				return TryConverter(value, returnType, converter, out result);

			parser = GetExplicitConverter(returnType);
			if (parser != null)
			{
				__stringConditionalConstructor.TryAdd(returnType, parser);
				return parser(value, out result);
			}

			__missingConverters.TryAdd(returnType, true);
			result = Factory.DefaultValue(returnType);
			return false;
		}
		private static readonly ConcurrentDictionary<Type, bool> __missingConverters = new ConcurrentDictionary<Type, bool>();

		private static bool TryConverter(string value, Type returnType, TypeConverter converter, [MaybeNullWhen(false)] out object result)
		{
			try
			{
				result = converter.ConvertFromInvariantString(value);
				return result != null;
			}
			catch (NotSupportedException)
			{
			}
			catch (ArgumentException)
			{
			}
			result = Factory.DefaultValue(returnType);
			return false;
		}

		private static TypeConverter? GetTypeConverter(Type targetType)
		{
			if (__typeConverterTypeMap.TryGetValue(targetType, out var converterType))
				return converterType == null ? null : Factory.Construct(converterType) as TypeConverter;

			converterType = GetConverterType(targetType);
			TypeConverter? converter = null;
			if (converterType != null)
			{
				converter = Factory.Construct(converterType) as TypeConverter;
				if (converter == null || !converter.CanConvertFrom(typeof(string)))
				{
					converterType = null;
					converter = null;
				}
			}
			__typeConverterTypeMap.TryAdd(targetType, converterType);
			return converter;
		}
		private static readonly ConcurrentDictionary<Type, Type?> __typeConverterTypeMap = new ConcurrentDictionary<Type, Type?>();

		private static Type? GetConverterType(Type? type)
		{
			while (type != null)
			{
				CustomAttributeTypedArgument argument = CustomAttributeData.GetCustomAttributes(type)
					.Where(o => o.Constructor.ReflectedType == typeof(TypeConverterAttribute) && o.ConstructorArguments.Count == 1)
					.Select(o => o.ConstructorArguments[0]).FirstOrDefault();

				if (argument != default)
				{
					var qualifiedType = argument.Value as Type;
					if (qualifiedType == null && argument.Value is string qualifiedTypeName)
						qualifiedType = Factory.GetType(qualifiedTypeName);
					return Factory.IsPublicType(qualifiedType) ? qualifiedType : null;
				}
				type = type.BaseType;
			}
			return null;
		}

		private static ValueParser? GetExplicitConverter(Type type)
		{
			MethodInfo? parser = type.GetMethod("TryParse", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string), type.MakeByRefType() }, null);
			if (parser != null)
				return Tgv(parser, type, Factory.IsNullableType(type));

			//		result = null;
			//		try
			//		{
			//
			//	3:		xtype tmp1;
			//			if (Convert(value, out tmp1))
			//			{
			//	3.1:		result = (object)operator(tmp1);
			//	3.2:		result = (object)new operator(tmp1);
			//				return true;
			//			}
			//		...
			//	3:		xtype tmp2;
			//			if (Convert(value, out tmp2))
			//			{
			//	3.1:		result = (object)operator(tmp2);
			//	3.2:		result = (object)new operator(tmp2);
			//				return true;
			//			}
			//		...
			//			string:
			//	1:			result = (object)operator(value);
			//	2:			result = (object)new operator(value);
			//				return true;
			//
			//			return false;
			//		}
			//		catch (Exception ex)
			//		{
			//			if (EX.IsCriticalException(ex))
			//				throw;
			//			return false;
			//		}


			ParameterExpression value = Expression.Parameter(typeof(string), "value");
			ParameterExpression result = Expression.Parameter(typeof(object).MakeByRefType(), "result");

			List<(MethodInfo Method, ParameterInfo Parameter, MethodInfo Parser)> operators = type.GetMethods(BindingFlags.Static | BindingFlags.Public)
				.Select(o =>
				{
					if (!type.IsAssignableFrom(o.ReturnType))
						return default;
					if (!(o.IsSpecialName && (o.Name == "op_Explicit" || o.Name == "op_Implicit")))
						return default;
					ParameterInfo[] pp = o.GetParameters();
					if (pp.Length != 1 || pp[0].ParameterType == type || pp[0].ParameterType.IsPointer)
						return default;
					ParserPair ps = Array.Find(__stringTypedParsers, p => p.Type == pp[0].ParameterType);
					if (ps.Type == null)
						return default;
					return (o, pp[0], ps.Method);
				})
				.Where(o => o.Method != null).ToList();

			if (operators.Count > 1)
				operators = __stringTypedParsers.Select(o => operators.FirstOrDefault(p => p.Parameter.ParameterType == o.Type)).ToList();

			List<(ConstructorInfo Constructor, ParameterInfo Parameter, MethodInfo Parser)> constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public)
				.Select(o =>
				{
					ParameterInfo[] pp = o.GetParameters();
					if (pp.Length != 1 || pp[0].ParameterType == type || pp[0].ParameterType.IsPointer ||
						operators.Any(x => x.Parameter.ParameterType == pp[0].ParameterType))
						return default;
					ParserPair ps = Array.Find(__stringTypedParsers, p => p.Type == pp[0].ParameterType);
					if (ps.Type == null)
						return default;

					return (Constructor: o, Parameter: pp[0], Parser: ps.Method);
				})
				.Where(o => o.Constructor != null).ToList();

			if (constructors.Count > 1)
				constructors = __stringTypedParsers.Select(o => constructors.FirstOrDefault(p => p.Parameter.ParameterType == o.Type)).ToList();

			LabelTarget end = Expression.Label(typeof(bool));
			var convert = new List<Expression>();
			var variable = new List<ParameterExpression>();
			foreach (var item in operators)
			{
				if (item.Parameter.ParameterType != typeof(string))
				{
					//	xtype tmp;
					//	if (Convert(value, out tmp))
					//	{
					//		result = (object)operator(tmp);
					//		return true;
					//	}
					ParameterExpression tmp = Expression.Variable(item.Parameter.ParameterType);
					Expression conv = Expression.IfThen(
						Expression.Call(item.Parser, value, tmp),
						Expression.Block(
							Expression.Assign(result,
								type.IsValueType ? (Expression)
									Expression.TypeAs(Expression.Call(item.Method, tmp), typeof(object)) :
									Expression.Call(item.Method, tmp)),
							Expression.Goto(end, Expression.Constant(true))
							)
						);
					convert.Add(conv);
					variable.Add(tmp);
				}
			}

			foreach (var item in constructors)
			{
				if (item.Parameter.ParameterType != typeof(string))
				{
					//	xtype tmp;
					//	if (Convert(value, out tmp))
					//	{
					//		result = (object)new operator(tmp);
					//		return true;
					//	}
					ParameterExpression tmp = Expression.Variable(item.Parameter.ParameterType);
					Expression conv = Expression.IfThen(
						Expression.Call(item.Parser, value, tmp),
						Expression.Block(
							Expression.Assign(result,
								type.IsValueType ? (Expression)
									Expression.TypeAs(Expression.New(item.Constructor, tmp), typeof(object)) :
									Expression.New(item.Constructor, tmp)),
							Expression.Goto(end, Expression.Constant(true))
							)
						);
					convert.Add(conv);
					variable.Add(tmp);
				}
			}

			var stringOperator = operators.FirstOrDefault(o => o.Parameter.ParameterType == typeof(string));
			if (stringOperator.Method != null)
			{
				Expression last = Expression.Assign(result,
					type.IsValueType ? (Expression)
						Expression.TypeAs(Expression.Call(stringOperator.Method, value), typeof(object)) :
						Expression.Call(stringOperator.Method, value));
				convert.Add(last);
				convert.Add(Expression.Goto(end, Expression.Constant(true)));
			}
			else
			{
				var stringConstructor = constructors.FirstOrDefault(o => o.Parameter.ParameterType == typeof(string));
				if (stringConstructor.Constructor != null)
				{
					Expression last = Expression.Assign(result,
						type.IsValueType ? (Expression)
							Expression.TypeAs(Expression.New(stringConstructor.Constructor, value), typeof(object)) :
							Expression.New(stringConstructor.Constructor, value));
					convert.Add(last);
					convert.Add(Expression.Goto(end, Expression.Constant(true)));
				}
			}

			if (convert.Count == 0)
				return null;

			convert.Add(Expression.Label(end, Expression.Constant(false)));

			ParameterExpression ex = Expression.Variable(typeof(Exception), "ex");

			Expression body = Expression.Block(
				Expression.Assign(result, Expression.Constant(null)),
				Expression.TryCatch(
					Expression.Block(variable, convert),
					Expression.Catch(ex, Expression.Block(
						Expression.IfThen(
							Expression.Call(((Func<Exception, bool>)ExceptionExtensions.IsCriticalException).Method, ex),
							Expression.Throw(ex)),
						Expression.Constant(false))
						)
					));
			return Expression.Lambda<ValueParser>(body, value, result).Compile();
		}

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
				result = (T)temp;
				return true;
			}
			result = default;
			return false;
		}

		public static object GetValue(XmlLiteNode node, Type returnType)
		{
			if (node is null)
				throw new ArgumentNullException(nameof(node));

			return TryGetValue(node, returnType, out var result) ? result : throw new FormatException(SR.FormatException(node.ToString().Left(1024), returnType));
		}

		public static bool TryGetValue(XmlLiteNode? node, Type returnType, [MaybeNullWhen(false)] out object result)
		{
			if (returnType is null)
				throw new ArgumentNullException(nameof(returnType));

			if (node == null)
			{
				result = Factory.DefaultValue(returnType);
				return false;
			}

			try
			{
				var typeName = node["$type"];
				Type? type = null;
				if (typeName != null)
					type = Factory.GetType(typeName) ??
						Factory.GetType(typeName + "Config") ??
						Factory.GetType(typeName + "Configuration") ??
						Factory.GetType(typeName + "Setting") ??
						Factory.GetType(typeName + "Settings") ??
						Factory.GetType(typeName + "Parameters") ??
						Factory.GetType(typeName + "Option") ??
						Factory.GetType(typeName + "Options");
				return type is null || type == returnType || !returnType.IsAssignableFrom(type) ?
					TryGetValueFromNode(node, returnType, out result):
					TryGetValueFromNode(node, type, out result) || TryGetValueFromNode(node, returnType, out result);
			}
			catch (Exception e)
			{
				throw new FormatException(SR.CannotParseValue(node.ToString().Left(1024), returnType), e);
			}
		}

		private static bool TryGetValueFromNode(XmlLiteNode node, Type returnType, [MaybeNullWhen(false)] out object result)
		{
			if (__stringConditionalConstructor.TryGetValue(returnType, out ValueParser? stringParser))
				return stringParser(node.Value, out result);

			if (__nodeConditionalConstructor.TryGetValue(returnType, out TryGetNodeValue? nodeParser))
			{
				if (nodeParser == null)
				{
					result = Factory.DefaultValue(returnType);
					return false;
				}
				return nodeParser(node, returnType, out result);
			}
			if (returnType.IsGenericType && __nodeGenericConstructor.TryGetValue(returnType.GetGenericTypeDefinition(), out nodeParser))
			{
				if (nodeParser == null)
				{
					result = Factory.DefaultValue(returnType);
					return false;
				}
				__nodeConditionalConstructor[returnType] = nodeParser;
				return nodeParser(node, returnType, out result);
			}
			if (returnType.IsEnum)
			{
				__stringConditionalConstructor.TryAdd(returnType, (string x, [MaybeNullWhen(false)] out object y) => TryGetEnum(x, returnType, out y));
				return TryGetEnum(node.Value, returnType, out result);
			}

			nodeParser = TryReflection;
			if (TestFromXmlLite(node, returnType, out result))
				nodeParser = TryFromXmlLite;
			else if (TestFromXmlReader(node, returnType, out result))
				nodeParser = TryFromXmlReader;
			else if (TestSerializer(node, returnType, out result))
				nodeParser = TrySerializer;
			else
				TryReflection(node, returnType, out result);

			__nodeConditionalConstructor.TryAdd(returnType, nodeParser);
			return result != null;
		}

		private static bool TryFromXmlLite(XmlLiteNode node, Type returnType, [MaybeNullWhen(false)] out object value)
		{
			value = __fromXmlLiteNodeParsers[returnType]?.Invoke(node);
			return value != null;
		}

		private static bool TryFromXmlReader(XmlLiteNode node, Type returnType, [MaybeNullWhen(false)] out object value)
		{
			using (XmlReader reader = node.ReadSubtree())
			{
				value = __fromXmlReaderParsers[returnType]?.Invoke(reader);
			}
			return value != null;
		}

		private static bool TrySerializer(XmlLiteNode node, Type returnType, [MaybeNullWhen(false)] out object result)
		{
			try
			{
				var xs = new XmlSerializer(returnType, new XmlRootAttribute(node.Name));
				using XmlReader reader = node.ReadSubtree();
				if (xs.CanDeserialize(reader))
				{
					result = xs.Deserialize(reader);
					if (result != null)
						return true;
				}
			}
			catch (InvalidOperationException)
			{
			}
			result = Factory.DefaultValue(returnType);
			return false;
		}

		private static bool TestFromXmlLite(XmlLiteNode node, Type returnType, [MaybeNullWhen(false)] out object value)
		{
			value = null;
			if (!__fromXmlLiteNodeParsers.TryGetValue(returnType, out Func<XmlLiteNode, object>? parser))
			{
				Type baseType = Factory.NullableTypeBase(returnType);
				parser = __fromXmlLiteNodeParsers.GetOrAdd(baseType, type => GetFromXmlConstructor<XmlLiteNode>(type) ?? GetFromXmlStaticConstructor<XmlLiteNode>(type));
				if (baseType.IsValueType)
					__fromXmlLiteNodeParsers.TryAdd(typeof(Nullable<>).MakeGenericType(baseType), parser);
			}
			if (parser == null)
				return false;

			value = parser(node);
			return true;
		}
		private static readonly ConcurrentDictionary<Type, Func<XmlLiteNode, object>?> __fromXmlLiteNodeParsers = new ConcurrentDictionary<Type, Func<XmlLiteNode, object>?>();

		private static bool TestFromXmlReader(XmlLiteNode node, Type returnType, [MaybeNullWhen(false)] out object value)
		{
			value = null;
			if (!__fromXmlReaderParsers.TryGetValue(returnType, out Func<XmlReader, object>? parser))
			{
				Type baseType = Factory.NullableTypeBase(returnType);
				parser = __fromXmlReaderParsers.GetOrAdd(baseType, type => GetFromXmlConstructor<XmlReader>(type) ?? GetFromXmlStaticConstructor<XmlReader>(type));
				if (baseType.IsValueType)
					__fromXmlReaderParsers.TryAdd(typeof(Nullable<>).MakeGenericType(baseType), parser);
			}
			if (parser == null)
				return false;

			using (XmlReader reader = node.ReadSubtree())
			{
				value = parser(reader);
			}
			return true;
		}
		private static readonly ConcurrentDictionary<Type, Func<XmlReader, object>?> __fromXmlReaderParsers = new ConcurrentDictionary<Type, Func<XmlReader, object>?>();


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
						if (method.Name == "Create" || method.Name == "FromXml")
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

		private static bool TestSerializer(XmlLiteNode node, Type returnType, out object? result)
		{
			result = null;
			if (!returnType.IsPublic)
				return false;
			if (!returnType.IsAbstract)
				return false;
			try
			{
				var xs = new XmlSerializer(returnType, new XmlRootAttribute(node.Name));
				using XmlReader reader = node.ReadSubtree();
				if (!xs.CanDeserialize(reader))
					return false;
				result = xs.Deserialize(reader);
				return true;
			}
			catch (InvalidOperationException)
			{
			}
			return false;
		}

		#region Try Reflection

		private static bool TryReflection(XmlLiteNode node, Type returnType, out object? result)
		{
			result = Factory.DefaultValue(returnType);
			if (node == null)
				return false;

			if (node.Elements.Count == 0 && node.Attributes.Count == 0)
				return TryGetValue(node.Value, returnType, out result);

			if (TryCollection(node, node.Elements, returnType, ref result))
				return true;

			var args = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
			foreach (var item in node.Attributes)
			{
				args[item.Key] = item.Value;
			}
			var skipped = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var item in node.Elements)
			{
				if (skipped.Contains(item.Name))
					continue;
				if (args.ContainsKey(item.Name))
				{
					skipped.Add(item.Name);
					args.Remove(item.Name);
				}
				else
				{
					args.Add(item.Name, item);
				}
			}

			var (missings, obj) = TryConstruct(returnType, args);
			if (obj == null)
			{
				result = Factory.DefaultValue(returnType);
				return false;
			}
			result = obj;

			if (missings.Count > 0 || skipped.Count > 0)
			{
				if (skipped.Count > 0)
				{
					foreach (var item in missings)
					{
						skipped.Add(item);
					}
					missings = skipped;
				}

				foreach (FieldInfo item in returnType.GetFields(BindingFlags.Instance | BindingFlags.Public))
				{
					GetFieldValue(node, item.Name, item.FieldType, missings, () => item.GetValue(obj), o => item.SetValue(obj, o));
				}
				foreach (PropertyInfo item in returnType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
				{
					if (!item.CanWrite || !item.CanWrite || item.SetMethod == null || !item.SetMethod.IsPublic || item.GetIndexParameters().Length != 0)
						continue;

					GetFieldValue(node, item.Name, item.PropertyType, missings, () => item.GetValue(obj), o => item.SetValue(obj, o));
				}

				static void GetFieldValue(XmlLiteNode node, string name, Type itemType, IReadOnlyCollection<string> missings, Func<object?> getter, Action<object?> setter)
				{
					if (missings.FirstOrDefault(o => String.Equals(o, name, StringComparison.OrdinalIgnoreCase)) != null)
					{
						var element = node.Element(name, StringComparer.OrdinalIgnoreCase);
						if (element.IsEmpty)
						{
							var attrib = node.Attributes.FirstOrDefault(o => String.Equals(o.Key, name, StringComparison.OrdinalIgnoreCase));
							if (attrib.Value != null && TryGetValue(attrib.Value, itemType, out var value))
								setter(value);
						}
						else
						{
							object? value = getter();
							if (TryCollection(element, element.Elements, itemType, ref value) || TryGetValue(element, itemType, out value))
								setter(value);
						}
					}
					else
					{
						var singular = Lingua.Singular(name);
						if (name != singular && missings.FirstOrDefault(o => String.Equals(o, singular, StringComparison.OrdinalIgnoreCase)) != null)
						{
							object? value = getter();
							if (TryCollection(XmlLiteNode.Empty, node.Elements.Where(o => String.Equals(o.Name, singular, StringComparison.OrdinalIgnoreCase)), itemType, ref value))
								setter(value);
						}
					}
				}
			}

			return true;
		}

		private readonly struct TypesKey: IEquatable<TypesKey>
		{
			private readonly int _hashCode;

			public TypesKey(Type type, Type[] types)
			{
				Type = type;
				Parameters = types;
				_hashCode = HashCode.Join(type.GetHashCode(), Parameters);
			}

			public Type Type { get; }

			public Type[] Parameters { get; }

			public override int GetHashCode() => _hashCode;

			public override bool Equals(object? obj) => obj is TypesKey other && Equals(other);

			public bool Equals(TypesKey other)
			{
				if (_hashCode != other._hashCode)
					return false;
				var a = Parameters;
				var b = other.Parameters;
				if (a.Length != b.Length)
					return false;
				for (int i = 0; i < a.Length; ++i)
				{
					if (a[i] != b[i])
						return false;
				}
				return true;
			}
		}

		private static (IReadOnlyCollection<string> Missings, object? Value) TryConstruct(Type type, Dictionary<string, object> arguments)
		{
			if (arguments == null || arguments.Count == 0)
				return (Array.Empty<string>(), Factory.TryConstruct(type));

			var key = new ConstructorKey(type, arguments);
			if (!__attributedConstructors.TryGetValue(key, out var constructor))
				constructor = __attributedConstructors.GetOrAdd(key, AttributedConstructor.Create(type, arguments));
			return (constructor?.Missings ?? (IReadOnlyCollection<string>)arguments.Keys, constructor?.Invoke(arguments));
		}
		private static readonly ConcurrentDictionary<ConstructorKey, AttributedConstructor?> __attributedConstructors = new ConcurrentDictionary<ConstructorKey, AttributedConstructor?>();

		private readonly struct ConstructorKey: IEquatable<ConstructorKey>
		{
			private readonly Type _type;
			private readonly string _key;
			private readonly int _hashCode;

			public ConstructorKey(Type type, Dictionary<string, object>? arguments)
			{
				_type = type ?? throw new ArgumentNullException(nameof(type));
				if (arguments == null || arguments.Count <= 0)
				{
					_key = String.Empty;
				}
				else
				{
					var ss = new StringBuilder(arguments.Count * 12);
					foreach (var key in arguments.Keys)
					{
						ss.Append(':').Append(key);
					}
					_key = ss.ToString().ToUpperInvariant();
				}
				_hashCode = HashCode.Join(_type.GetHashCode(), _key.GetHashCode());
			}

			public override int GetHashCode() => _hashCode;

			public override bool Equals(object? obj) => obj is ConstructorKey other && Equals(other);

			public bool Equals(ConstructorKey other) => other._hashCode == _hashCode && _type == other._type && _key == other._key;
		}

		private class AttributedConstructor
		{
			private readonly Func<object?[], object?> _constructor;
			private readonly (string Name, Type Type)[] _parameters;
			private readonly Type _type;

			private AttributedConstructor(Type type, Func<object?[], object?> constructor, (string Name, Type Info)[] parameters, IReadOnlyList<string> missings)
			{
				_type = type ?? throw new ArgumentNullException(nameof(type));
				_constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
				_parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
				Missings = missings ?? Array.Empty<string>();
			}

			public IReadOnlyList<string> Missings { get; }

			public object? Invoke(Dictionary<string, object> arguments)
			{
				if (_parameters.Length > 0)
				{
					if (arguments == null)
						throw new ArgumentNullException(nameof(arguments));
					if (arguments.Count < _parameters.Length)
						throw new ArgumentOutOfRangeException(nameof(arguments), arguments.Count, null).Add("expected value", _parameters.Length);
				}

				var values = new object?[_parameters.Length];
				for (int i = 0; i < _parameters.Length; ++i)
				{
					if (!arguments.TryGetValue(_parameters[i].Name, out var value))
						return null;
					if (value is string s)
					{
						if (!TryGetValue(s, _parameters[i].Type, out var v))
						{
							Log.Trace($"{nameof(AttributedConstructor)}.{nameof(Invoke)}: Cannot parse parameter {_parameters[i].Name} of {_parameters[i].Type.Name} used to construct {_type.Name}.");
							return null;
						}
						values[i] = v;
					}
					else if (value is XmlLiteNode x)
					{
						if (!TryGetValue(x, _parameters[i].Type, out var v))
						{
							Log.Trace($"{nameof(AttributedConstructor)}.{nameof(Invoke)}: Cannot convert parameter {_parameters[i].Name} to type {_parameters[i].Type.Name} used to construct {_type.Name}.");
							return null;
						}
						values[i] = v;
					}
					else if (value is null)
					{
						values[i] = Factory.DefaultValue(_parameters[i].Type);
					}
					else if (_parameters[i].Type.IsAssignableFrom(value.GetType()))
					{
						values[i] = value;
					}
					else
					{
						Log.Trace($"{nameof(AttributedConstructor)}.{nameof(Invoke)}: Cannot convert parameter {_parameters[i].Name} to type {_parameters[i].Type.Name} from {value.GetType().Name} used to construct {_type.Name}.");
						return null;
					}
				}
				return _constructor.Invoke(values);
			}

			public static AttributedConstructor? Create(Type type, Dictionary<string, object> arguments)
			{
				if (type == null)
					throw new ArgumentNullException(nameof(type));
				if (arguments == null)
					throw new ArgumentNullException(nameof(arguments));

				ConstructorInfo[] constructors = type.GetConstructors();
				var parametersSet = new ParameterInfo[constructors.Length][];
				for (int i = 0; i < constructors.Length; ++i)
				{
					parametersSet[i] = constructors[i].GetParameters();
				}

				Array.Sort(parametersSet, constructors, Comparer.Create<ParameterInfo[]>((a, b) => b.Length.CompareTo(a.Length)));

				ParameterInfo[]? parameters = null;
				ConstructorInfo? constructor = null;
				BitArray? index = null;
				int weight = 0;
				for (int i = 0; i < constructors.Length; ++i)
				{
					var prm = parametersSet[i];
					if (prm.Length < weight)
						break;
					var idx = new BitArray(prm.Length);
					int w = 0;
					for (int j = 0; j < prm.Length; ++j)
					{
						if (prm[j].Name == null)
							goto skip;
						idx[j] = arguments.ContainsKey(prm[j].Name!);
						if (idx[j])
							++w;
						else if (!prm[j].IsOptional)
							goto skip;
					}
					if (index == null || w >= weight)
					{
						index = idx;
						weight = w;
						constructor = constructors[i];
						parameters = prm;
					}
				skip:;
				}

				if (constructor == null)
				{
					if (!type.IsValueType)
					{
						Log.Trace($"{nameof(AttributedConstructor)}.{nameof(Create)}: Cannot find a counstructor for {type.Name} and arguments: {String.Join(", ", arguments.Keys)}");
						return null;
					}
					parameters = Array.Empty<ParameterInfo>();
				}
				Debug.Assert(parameters != null);

				ParameterExpression arg = Expression.Parameter(typeof(object[]));
				Expression ctor;
				var parms = new List<(string Name, Type Type)>();
				if (weight == 0 && type.IsValueType)
				{
					ctor = Expression.TypeAs(Expression.Default(type), typeof(object));
				}
				else if (parameters.Length == 0)
				{
					ctor = Expression.New(type);
				}
				else
				{
					Debug.Assert(index != null);
					Debug.Assert(constructor != null);

					var args = new Expression[index.Length];
					for (int i = 0; i < index.Length; ++i)
					{
						if (!index[i])
						{
							args[i] = Expression.Convert(Expression.Constant(DefaultParameterValue(parameters[i])), parameters[i].ParameterType);
						}
						else
						{
							args[i] = Expression.Convert(Expression.ArrayAccess(arg, Expression.Constant(parms.Count)), parameters[i].ParameterType);
							parms.Add((parameters[i].Name!, parameters[i].ParameterType));
						}
					}
					if (type.IsValueType)
						ctor = Expression.TypeAs(Expression.New(constructor, args), typeof(object));
					else
						ctor = Expression.New(constructor, args);
				}
				var missings = new List<string>(arguments.Keys.Except(parameters.Select(o => o.Name!), StringComparer.OrdinalIgnoreCase));
				return new AttributedConstructor(type, Expression.Lambda<Func<object?[], object?>>(ctor, arg).Compile(), parms.ToArray(), missings);
			}
		}

		private static object? DefaultParameterValue(ParameterInfo parameter)
		{
			object? value = null;
			if (parameter.ParameterType == typeof(DateTime))
				return default(DateTime);
			try { value = parameter.DefaultValue; } catch { }
			return value ?? Factory.DefaultValue(parameter.ParameterType);
		}

		private static bool TryCollection(XmlLiteNode node, IEnumerable<XmlLiteNode> items, Type returnType, ref object? result)
		{
			if (returnType.IsArray)
			{
				if (returnType.GetArrayRank() != 1)
				{
					result = Factory.DefaultValue(returnType);
					return false;
				}
				Type itemType = returnType.GetElementType()!;
				var valueType = typeof(List<>).MakeGenericType(itemType);
				bool r = TryParseCollection(node, items, valueType, valueType, itemType, ref result);
				if (result != null)
					result = valueType.GetMethod("ToArray")?.Invoke(result, null);
				return r;
			}

			if (!returnType.IsInterface)
			{
				Type? collectionType = returnType.GetInterfaces().FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ICollection<>));
				if (collectionType == null)
				{
					result = Factory.DefaultValue(returnType);
					return false;
				}
				return TryParseCollection(node, items, returnType, collectionType, collectionType.GetGenericArguments()[0], ref result);
			}

			if (!returnType.IsGenericType)
			{
				result = Factory.DefaultValue(returnType);
				return false;
			}

			var genericType = returnType.GetGenericTypeDefinition();
			Type[] genericArgs = returnType.GetGenericArguments();

			if (genericArgs.Length == 1)
			{
				var valueType = typeof(List<>).MakeGenericType(genericArgs);
				Type? roType = null;
				if (returnType.IsAssignableFrom(valueType))
				{
					if (returnType.IsAssignableFrom(typeof(IReadOnlyList<>).MakeGenericType(genericArgs)))
						roType = typeof(IReadOnlyList<>);
				}
				else
				{
					valueType = typeof(HashSet<>).MakeGenericType(genericArgs);
					if (returnType.IsAssignableFrom(valueType))
					{
						if (!returnType.IsAssignableFrom(typeof(IReadOnlySet<>).MakeGenericType(genericArgs)))
							roType = typeof(IReadOnlySet<>);
					}
					else
					{
						result = Factory.DefaultValue(returnType);
						return false;
					}
				}
				bool r = TryParseCollection(node, items, valueType, valueType, genericArgs[0], ref result);
				if (roType != null)
					result = WrapCollection(result, genericArgs, roType);
				return r;
			}

			if (genericArgs.Length == 2)
			{
				var valueType = typeof(Dictionary<,>).MakeGenericType(genericArgs);
				if (returnType.IsAssignableFrom(valueType))
				{
					Type itemType = typeof(KeyValuePair<,>).MakeGenericType(genericArgs);
					bool r = TryParseCollection(node, items, valueType, typeof(ICollection<>).MakeGenericType(itemType), itemType, ref result);
					if (returnType.IsAssignableFrom(typeof(IReadOnlyDictionary<,>).MakeGenericType(genericArgs)))
						result = WrapCollection(result, genericArgs, typeof(IDictionary<,>));
					return true;
				}
			}

			result = Factory.DefaultValue(returnType);
			return false;
		}

		private static object? WrapCollection(object? value, Type[] parametersType, Type collectionType)
		{
			if (value == null)
				return null;
			Func<object, object?>? f = __genericReadonlyWrappers.GetOrAdd(new TypesKey(collectionType, parametersType),
				key =>
				{
					MethodInfo? m = Factory.GetGenericMethod(typeof(ReadOnly), "Wrap", new[] { key.Type });
					if (m == null)
						return null;
					Type type = key.Type.MakeGenericType(key.Parameters);
					m = m.MakeGenericMethod(key.Parameters);
					ParameterExpression arg = Expression.Parameter(typeof(object));
					return Expression.Lambda<Func<object, object>>(
						Expression.Call(m, Expression.Convert(arg, type)),
						arg).Compile();
				});
			return f == null ? value : f(value);
		}
		private static readonly ConcurrentDictionary<TypesKey, Func<object, object?>?> __genericReadonlyWrappers = new ConcurrentDictionary<TypesKey, Func<object, object?>?>();

		private static bool TryParseCollection(XmlLiteNode node, IEnumerable<XmlLiteNode> items, Type returnType, Type collectionType, Type itemType, [MaybeNullWhen(false)] ref object? result)
		{
			MethodInfo? add = collectionType.GetMethod("Add", new[] { itemType });
			if (add == null)
				return false;

			PropertyInfo? readOnly = collectionType.GetProperty("IsReadOnly", typeof(bool));
			if (readOnly != null && !readOnly.CanRead)
				readOnly = null;
			var readOnlyMethod = readOnly?.GetGetMethod();
			if (result == null || !returnType.IsInstanceOfType(result) ||
				(readOnlyMethod != null && Object.Equals(Factory.Invoke(result, readOnlyMethod), true)))
			{
				var args = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
				foreach (var item in node.Attributes)
				{
					args[item.Key] = item.Value;
				}
				result = TryConstruct(returnType, args).Value;
				if (result == null || (readOnlyMethod != null && Object.Equals(Factory.Invoke(result, readOnlyMethod), true)))
					return false;
			}

			foreach (var item in items)
			{
				if (TryGetValue(item, itemType, out object? itemValue))
					Factory.Invoke(result, add, itemValue);
			}
			return true;
		}

		#endregion

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
			return true;
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
				result = null;
				return true;
			}
			TryGetValue(node, returnType.GetGenericArguments()[0], out var value);
			result = value;
			return true;
		}

		private delegate bool TryGetNodeValue(XmlLiteNode node, Type returnType, out object? result);

		private static bool TryCopy(XmlLiteNode node, Type returnType, out object result)
		{
			result = node;
			return true;
		}

		private static ValueParser Tgv(MethodInfo getter, Type type, bool nullable = false)
		{
			//	T tmp = default(T);
			//	1: bool res = getter(argValue, out tmp)
			//	2: bool res = String.IsNullOrWhiteSpace(argValue) || getter(argValue, out tmp)
			//	if (res)
			//		argResult = tmp;
			//	else
			//		argResult = null;
			//	return res;
			ParameterExpression argValue = Expression.Parameter(typeof(string), "value");
			ParameterExpression argResult = Expression.Parameter(typeof(object).MakeByRefType(), "result");
			ParameterExpression tmp = Expression.Variable(type, "tmp");
			ParameterExpression res = Expression.Variable(typeof(bool), "res");
			Expression condition = Expression.Call(getter, argValue, tmp);
			if (nullable)
				condition = Expression.OrElse(Expression.Call(((Func<string, bool>)String.IsNullOrWhiteSpace).Method, argValue), condition);
			BlockExpression body = Expression.Block(typeof(bool),
				new[] { tmp, res },
				Expression.Assign(tmp, Expression.Default(type)),
				Expression.Assign(res, condition),
					//Expression.IfThenElse(res,
					Expression.Assign(argResult, Expression.TypeAs(tmp, typeof(object))),
				//	Expression.Assign(argResult, Expression.Constant(null))
				//	),
				res
				);

			return Expression.Lambda<ValueParser>(body, argValue, argResult).Compile();
		}
		private static bool TryParseString(string value, out string result)
		{
			result = value;
			return true;
		}
		private delegate bool ConcreteValueParser<T>(string value, [MaybeNullWhen(false)] out T result);
		private delegate bool ValueParser(string value, [MaybeNullWhen(false)] out object result);
		private struct ParserPair
		{
			public readonly Type Type;
			public readonly MethodInfo Method;

			private ParserPair(Type type, MethodInfo method)
			{
				Type = type;
				Method = method;
			}

			public static ParserPair New<T>(ConcreteValueParser<T> parser)
			{
				return new ParserPair(typeof(T), parser.Method);
			}
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
		private static readonly ParserPair[] __stringTypedParsers =
		{
			ParserPair.New<bool>(TryGetBoolean),
			ParserPair.New<byte>(Byte.TryParse),
			ParserPair.New<sbyte>(SByte.TryParse),
			ParserPair.New<short>(Int16.TryParse),
			ParserPair.New<ushort>(UInt16.TryParse),
			ParserPair.New<int>(Int32.TryParse),
			ParserPair.New<uint>(UInt32.TryParse),
			ParserPair.New<long>(Int64.TryParse),
			ParserPair.New<ulong>(UInt64.TryParse),
			ParserPair.New<float>(Single.TryParse),
			ParserPair.New<double>(Double.TryParse),
			ParserPair.New<decimal>(Decimal.TryParse),
			ParserPair.New<char>(TryGetChar),
			ParserPair.New<DateTime>(TryGetDateTime),
			ParserPair.New<TimeSpan>(TryGetTimeSpan),
			ParserPair.New<Guid>(TryGetGuid),
			ParserPair.New<Type>(TryGetType),
			ParserPair.New<string>(TryParseString),
		};
		private static readonly ConcurrentDictionary<Type, ValueParser> __stringConditionalConstructor = new ConcurrentDictionary<Type, ValueParser>
			(
				__stringTypedParsers.Select(o => new KeyValuePair<Type, ValueParser>(o.Type, Tgv(o.Method, o.Type)))
				.Union(
				__stringTypedParsers.Where(o => !o.Type.IsClass).Select(o => new KeyValuePair<Type, ValueParser>(typeof(Nullable<>).MakeGenericType(o.Type), Tgv(o.Method, o.Type, true)))
				)
			);

		#endregion
	}
}
