using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Lexxys;

public static partial class Strings
{
	public static object? GetValue(string? value, Type type, object? defaultValue)
	{
		if (type == null)
			throw new ArgumentNullException(nameof(type));

		return TryGetValue(value, type, out object? result) ? result: defaultValue;
	}

	public static object? GetValue(string value, Type type)
	{
		if (value is not { Length: > 0 })
			throw new ArgumentNullException(nameof(value));
		if (type == null)
			throw new ArgumentNullException(nameof(type));
		return TryGetValue(value, type, out object? result) ? result: throw new FormatException(SR.FormatException(value));
	}

	public static T GetValue<T>(string? value, T defaultValue)
	{
		if (value is not { Length: > 0 })
			return defaultValue;
		return TryGetValue<T>(value, out var result) ? result: defaultValue;
	}

	public static T GetValue<T>(string value)
	{
		if (value is not { Length: > 0 })
			throw new ArgumentNullException(nameof(value));
		return TryGetValue<T>(value, out var result) ? result: throw new FormatException(SR.FormatException(value));
	}

	public static bool TryGetValue<T>(string? value, [MaybeNullWhen(false)] out T result)
	{
		if (TryGetValue(value, typeof(T), out var temp))
		{
			result = (T)temp!;
			return true;
		}
		result = default!;
		return false;
	}

	public static object? GetValueOrDefault(string? value, Type returnType)
	{
		if (returnType == null)
			throw new ArgumentNullException(nameof(returnType));

		return TryGetValue(value, returnType, out object? result) ? result: Factory.DefaultValue(returnType);
	}

	public static bool TryGetValue(string? value, Type returnType, out object? result)
	{
		if (returnType == null)
			throw new ArgumentNullException(nameof(returnType));

		result = null;
		if (value == null || __missingConverters.ContainsKey(returnType))
			return false;

		if (IsNullValue(value))
			return Factory.IsNullableType(returnType);

		if (TryWellKnownConverter(value, returnType, out result))
			return true;

		if (returnType.IsEnum || returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Nullable<>) && returnType.GetGenericArguments()[0].IsEnum)
		{
			var (regular, nullable) = NullableTypes(returnType);
			__stringConditionalConstructor.TryAdd(regular!, (string x, out object? y) => TryGetEnum(x, regular, out y));
			__stringConditionalConstructor.TryAdd(nullable!, (string x, out object? y) => TryGetEnum(x, regular, out y));

			return TryGetEnum(value, regular, out result);
		}

		var parser = GetTypeConverter(returnType) ?? GetExplicitConverter(returnType);
		if (parser != null)
		{
			var (type1, type2) = NullableTypes(returnType);
			__stringConditionalConstructor.TryAdd(type1, parser);
			if (type2 != null)
				__stringConditionalConstructor.TryAdd(type2, parser);
			return parser(value, out result);
		}

		__missingConverters.TryAdd(returnType, true);
		return false;
	}

	internal static bool TryWellKnownConverter(string value, Type returnType, out object? result)
	{
		if (__stringConditionalConstructor.TryGetValue(returnType, out ValueParser? stringParser))
			return stringParser(value, out result);
		result = null;
		return false;
	}

	private static bool IsNullValue(string? value)
	{
		value = value.TrimToNull();
		return value == null ||
			value.Length == 4 && String.Equals(value, "NULL", StringComparison.OrdinalIgnoreCase) ||
			value.Length == 5 && String.Equals(value, "$NULL", StringComparison.OrdinalIgnoreCase);
	}

	private static ValueParser? GetTypeConverter(Type targetType)
	{
		var converterType = GetConverterType(targetType);
		if (converterType == null)
			return null;
		TypeConverter? converter;
		try
		{
			converter = Factory.Construct(converterType) as TypeConverter;
		}
		catch
		{
			return null;
		}
		if (converter == null || !converter.CanConvertFrom(typeof(string)))
			return null;
		return (string v, out object? r) =>
		{
			try
			{
				r = converter.ConvertFromInvariantString(v);
				return true;
			}
			catch (NotSupportedException)
			{
			}
			catch (ArgumentException)
			{
			}
			r = null;
			return false;
		};
	}

	private static Type? GetConverterType(Type? type)
	{
		if (type is null)
			return null;
		while (type != typeof(object))
		{
			CustomAttributeTypedArgument argument = CustomAttributeData.GetCustomAttributes(type)
				.Where(o => o.Constructor.ReflectedType == typeof(TypeConverterAttribute) && o.ConstructorArguments.Count == 1)
				.Select(o => o.ConstructorArguments[0]).FirstOrDefault();

			if (argument != default)
			{
				if (argument.Value is Type qualifiedType)
					return Factory.IsPublicType(qualifiedType) ? qualifiedType: null;

				if (argument.Value is string qualifiedTypeName)
				{
					qualifiedType = Factory.GetType(qualifiedTypeName)!;
					return Factory.IsPublicType(qualifiedType) ? qualifiedType: null;
				}
				return null;
			}
			type = type.BaseType!;
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
							type.IsValueType ?
								Expression.TypeAs(Expression.Call(item.Method, tmp), typeof(object)):
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
							type.IsValueType ?
								Expression.TypeAs(Expression.New(item.Constructor, tmp), typeof(object)):
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
				type.IsValueType ?
					Expression.TypeAs(Expression.Call(stringOperator.Method, value), typeof(object)):
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
					type.IsValueType ?
						Expression.TypeAs(Expression.New(stringConstructor.Constructor, value), typeof(object)):
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

	#region Predefined Parsers

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
				Expression.Assign(argResult, Expression.TypeAs(tmp, typeof(object))),
			res
			);

		return Expression.Lambda<ValueParser>(body, argValue, argResult).Compile();
	}
	private static bool TryParseString(string value, out string result)
	{
		result = value;
		return true;
	}
	internal delegate bool ConcreteValueParser<T>(string value, [MaybeNullWhen(false)] out T result);
	internal delegate bool ValueParser(string value, out object? result);
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
	private static readonly ConcurrentDictionary<Type, bool> __missingConverters = new ConcurrentDictionary<Type, bool>();

	#endregion
}
