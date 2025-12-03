using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Linq.Expressions;
using System.Reflection;

namespace Lexxys;

public static partial class Factory
{
	private static readonly ConcurrentDictionary<(Type Type, ObjectAccessorKey Key), ObjectTypeAccessor> __typeAccessors = [];
	private static readonly ConcurrentDictionary<MemberInfo, Func<object?, object?[], object?>?> __compiledMethods = [];

	[Flags]
	enum ObjectAccessorKey
	{
		IncludeStatics = 1,
		IncludeNonPublic = 2,
	}

	public static IObjectAccessor CreateAccessor(object obj, bool includeStatics = false, bool includeNonPublic = false)
	{
		if (obj == null) throw new ArgumentNullException(nameof(obj));
		var key = (includeStatics ? ObjectAccessorKey.IncludeStatics: 0) | (includeNonPublic ? ObjectAccessorKey.IncludeNonPublic: 0);
		var type = obj.GetType();
		var accessor = __typeAccessors.GetOrAdd((type, key), o => new ObjectTypeAccessor(o.Type, o.Key));
		return new ObjectAccessor(accessor, obj);
	}

	public static IObjectTypeAccessor CreateTypeAccessor(Type type, bool includeStatics = false, bool includeNonPublic = false)
	{
		if (type is null) throw new ArgumentNullException(nameof(type));

		var key = (includeStatics ? ObjectAccessorKey.IncludeStatics: 0) | (includeNonPublic ? ObjectAccessorKey.IncludeNonPublic: 0);
		return __typeAccessors.GetOrAdd((type, key), o => new ObjectTypeAccessor(o.Type, o.Key));
	}

	class ObjectTypeAccessor: IObjectTypeAccessor
	{
		private readonly FrozenDictionary<string, (Func<object?, object?>? Get, Func<object?, object?, object?>? Set)> _properties;
		private readonly FrozenDictionary<string, (Func<object?, object?, object?>? Get, Func<object?, object?, object?, object?>? Set)> _indexedProperties;

		public ObjectTypeAccessor(Type type, ObjectAccessorKey key)
		{
			if (type is null) throw new ArgumentNullException(nameof(type));

			BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public;
			if (key.HasFlag(ObjectAccessorKey.IncludeStatics))
				bindingFlags |= BindingFlags.Static;
			if (key.HasFlag(ObjectAccessorKey.IncludeNonPublic))
				bindingFlags |= BindingFlags.NonPublic;

			Dictionary<string, (Func<object?, object?>? Get, Func<object?, object?, object?>? Set)>? properties = null;
			Dictionary<string, (Func<object?, object?, object?>? Get, Func<object?, object?, object?, object?>? Set)>? indexedProperties = null;
			CollectProperties(type, bindingFlags, ref properties, ref indexedProperties);
			CollectFields(type, bindingFlags, ref properties);
			
			_properties = properties?.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase) ?? FrozenDictionary<string, (Func<object?, object?>? Get, Func<object?, object?, object?>? Set)>.Empty;
			_indexedProperties = indexedProperties?.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase) ?? FrozenDictionary<string, (Func<object?, object?, object?>? Get, Func<object?, object?, object?, object?>? Set)>.Empty;
		}

		public IReadOnlyCollection<string> Properties => _properties.Keys;

		public IReadOnlyCollection<string> IndexedProperties => _indexedProperties.Keys;

		private void CollectProperties(Type type, BindingFlags bindingFlags,
			ref Dictionary<string, (Func<object?, object?>? Get, Func<object?, object?, object?>? Set)>? properties,
			ref Dictionary<string, (Func<object?, object?, object?>? Get, Func<object?, object?, object?, object?>? Set)>? indexedProperties)
		{
			var props = type.GetProperties(bindingFlags);

			foreach (var p in props)
			{
				if (p.GetCustomAttribute<CompilerGeneratedAttribute>() is not null) continue;

				var indexes = p.GetIndexParameters();
				if (indexes.Length > 1) continue;

				var getMethod = p.GetGetMethod(bindingFlags.HasFlag(BindingFlags.NonPublic));
				var setMethod = p.GetSetMethod(bindingFlags.HasFlag(BindingFlags.NonPublic));
				if (indexes.Length == 0)
				{
					var getter = getMethod == null ? null: Compile0(getMethod);
					var setter = setMethod == null ? null: Compile1(setMethod);
					properties ??= new (StringComparer.OrdinalIgnoreCase);
					properties[p.Name] = (getter, setter);
				}
				else
				{
					var getter = getMethod == null ? null: Compile1(getMethod);
					var setter = setMethod == null ? null: Compile2(setMethod);
					indexedProperties ??= new (StringComparer.OrdinalIgnoreCase);
					indexedProperties[p.Name] = (getter, setter);
				}
			}
		}

		private void CollectFields(Type type, BindingFlags bindingFlags, ref Dictionary<string, (Func<object?, object?>? Get, Func<object?, object?, object?>? Set)>? properties)
		{
			var fields = type.GetFields(bindingFlags);
			foreach (var f in fields)
			{
				if (f.GetCustomAttribute<CompilerGeneratedAttribute>() is not null) continue;

				var getter = CompileGetValue(f);
				var setter = CompileSetValue(f);
				properties ??= new (StringComparer.OrdinalIgnoreCase);
				properties[f.Name] = (getter, setter);
			}
		}

		public bool TryGetValue(object? instance, string name, out object? result)
		{
			if (!_properties.TryGetValue(name, out var accessor) || accessor.Get == null)
			{
				result = null;
				return false;
			}
			result = accessor.Get(instance);
			return true;
		}

		public bool TryGetValue(object? instance, string name, object index, out object? result)
		{
			if (!_indexedProperties.TryGetValue(name, out var accessor) || accessor.Get == null)
			{
				result = null;
				return false;
			}
			result = accessor.Get(instance, index);
			return true;
		}

		public bool TrySetValue(object? instance, string name, object? value)
		{
			if (!_properties.TryGetValue(name, out var accessor) || accessor.Set == null)
				return false;
			accessor.Set(instance, value);
			return true;
		}

		public bool TrySetValue(object? instance, string name, object index, object? value)
		{
			if (!_indexedProperties.TryGetValue(name, out var accessor) || accessor.Set == null)
				return false;
			accessor.Set(instance, index, value);
			return true;
		}
	}

	class ObjectAccessor(ObjectTypeAccessor accessor, object? obj): IObjectAccessor
	{
		private readonly ObjectTypeAccessor _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));

		public IReadOnlyCollection<string> Properties => _accessor.Properties;

		public IReadOnlyCollection<string> IndexedProperties => _accessor.IndexedProperties;

		public bool TryGetValue(string name, out object? result)
			=> _accessor.TryGetValue(obj, name, out result);

		public bool TryGetValue(string name, object index, out object? result)
			=> _accessor.TryGetValue(obj, name, index, out result);

		public bool TrySetValue(string name, object? value)
			=> _accessor.TrySetValue(obj, name, value);

		public bool TrySetValue(string name, object index, object? value)
			=> _accessor.TrySetValue(obj, name, index, value);
	}

	public static object? Invoke(object? instance, MethodInfo method, params object?[] parameters)
	{
		if (method == null) throw new ArgumentNullException(nameof(method));
		if (instance == null && !method.IsStatic) throw new ArgumentNullException(nameof(instance));

		Func<object?, object?[], object?>? f = __compiledMethods.GetOrAdd(method, o => Compile((MethodInfo)o));
		return f?.Invoke(instance, parameters);
	}

	public static object? Invoke(MethodInfo method, params object?[] parameters)
	{
		if (method == null) throw new ArgumentNullException(nameof(method));
		if (!method.IsStatic) throw new ArgumentException("Method must be static.", nameof(method));

		Func<object?, object?[], object?>? f = __compiledMethods.GetOrAdd(method, o => Compile((MethodInfo)o));
		return f?.Invoke(null, parameters);
	}

	public static object? Invoke(ConstructorInfo constructor, params object?[] parameters)
	{
		if (constructor == null) throw new ArgumentNullException(nameof(constructor));

		Func<object?, object?[], object?>? f = __compiledMethods.GetOrAdd(constructor, o => Compile((ConstructorInfo)o));
		return f?.Invoke(null, parameters);
	}

	private static Func<object?, object?[], object?> Compile(MethodInfo method)
	{
		if (method == null) throw new ArgumentNullException(nameof(method));

		ParameterExpression arg0 = Expression.Parameter(typeof(object));
		ParameterExpression args = Expression.Parameter(typeof(object[]), "args");
		Expression[] pp = CompileParameters(method, args);
		Expression? instance = method.IsStatic || method.DeclaringType == null ? null : Expression.Convert(arg0, method.DeclaringType);
		Expression call = WrapMethodCall(method.IsStatic ? Expression.Call(method, pp) : Expression.Call(instance, method, pp), method.ReturnType);

		return Expression.Lambda<Func<object?, object?[], object?>>(call, arg0, args)
#if NETFRAMEWORK && DEBUG
			.Compile(DebugInfo)!;
#else
			.Compile();
#endif

	}

	private static Expression WrapMethodCall(Expression call, Type type)
	{
		return type == typeof(void) ? Expression.Block(call, Expression.Constant(null)):
			type.IsValueType ? Expression.Convert(call, typeof(object)): call;
	}

	private static Func<object?, object?> CompileGetValue(FieldInfo field)
	{
		if (field == null) throw new ArgumentNullException(nameof(field));
		ParameterExpression arg0 = Expression.Parameter(typeof(object));
		Expression? instance = field.IsStatic ? null: Expression.Convert(arg0, field.DeclaringType!);
		Expression fieldAccess = WrapMethodCall(Expression.Field(instance, field), field.FieldType);
		return Expression.Lambda<Func<object?, object?>>(fieldAccess, arg0).Compile();
	}

	private static Func<object?, object?, object?>? CompileSetValue(FieldInfo field)
	{
		if (field == null) throw new ArgumentNullException(nameof(field));
		if (field.IsInitOnly || field.IsLiteral)
			return null;

		ParameterExpression arg0 = Expression.Parameter(typeof(object));
		ParameterExpression arg1 = Expression.Parameter(typeof(object), "value");
		Expression? instance = field.IsStatic ? null: Expression.Convert(arg0, field.DeclaringType!);
		Expression value = Expression.Convert(arg1, field.FieldType);
		Expression fieldAccess = Expression.Assign(Expression.Field(instance, field), value);
		return Expression.Lambda<Func<object?, object?, object?>>(Expression.Block(fieldAccess, arg1), arg0, arg1).Compile();
	}

	private static Func<object?, object?> Compile0(MethodInfo method)
	{
		if (method == null) throw new ArgumentNullException(nameof(method));

		ParameterExpression arg0 = Expression.Parameter(typeof(object));
		Expression? instance = method.IsStatic ? null: Expression.Convert(arg0, method.DeclaringType!);
		Expression call = WrapMethodCall(method.IsStatic ? Expression.Call(method): Expression.Call(instance, method), method.ReturnType);

		return Expression.Lambda<Func<object?, object?>>(call, arg0)
#if NETFRAMEWORK && DEBUG
			.Compile(DebugInfo)!;
#else
			.Compile();
#endif

	}

	private static Func<object?, object?, object?> Compile1(MethodInfo method)
	{
		if (method == null) throw new ArgumentNullException(nameof(method));

		ParameterInfo[] pp = method.GetParameters();
		if (pp.Length != 1) throw new ArgumentException($"Invalid number of parameters. Expected 1, actual {pp.Length}.", nameof(method));

		ParameterExpression arg0 = Expression.Parameter(typeof(object));
		ParameterExpression arg1 = Expression.Parameter(typeof(object), "arg");
		Expression p1 = Expression.Convert(arg1, pp[0].ParameterType);
		Expression? instance = method.IsStatic ? null: Expression.Convert(arg0, method.DeclaringType!);
		Expression call = WrapMethodCall(method.IsStatic ? Expression.Call(method, p1) : Expression.Call(instance, method, p1), method.ReturnType);

		return Expression.Lambda<Func<object?, object?, object?>>(call, arg0, arg1)
#if NETFRAMEWORK && DEBUG
			.Compile(DebugInfo)!;
#else
			.Compile();
#endif
	}

	private static Func<object?, object?, object?, object?> Compile2(MethodInfo method)
	{
		if (method == null) throw new ArgumentNullException(nameof(method));

		ParameterInfo[] pp = method.GetParameters();
		if (pp.Length != 2) throw new ArgumentException($"Invalid number of parameters. Expected 2, actual {pp.Length}.", nameof(method));

		ParameterExpression arg0 = Expression.Parameter(typeof(object));
		ParameterExpression arg1 = Expression.Parameter(typeof(object), "arg1");
		ParameterExpression arg2 = Expression.Parameter(typeof(object), "arg2");
		Expression p1 = Expression.Convert(arg1, pp[0].ParameterType);
		Expression p2 = Expression.Convert(arg2, pp[1].ParameterType);
		Expression? instance = method.IsStatic ? null: Expression.Convert(arg0, method.DeclaringType!);
		Expression call = WrapMethodCall(method.IsStatic ? Expression.Call(method, p1, p2) : Expression.Call(instance, method, p1, p2), method.ReturnType);

		return Expression.Lambda<Func<object?, object?, object?, object?>>(call, arg0, arg1, arg2)
#if NETFRAMEWORK && DEBUG
			.Compile(DebugInfo)!;
#else
			.Compile();
#endif
	}

	private static Func<object?, object?[], object?> Compile(ConstructorInfo constructor)
	{
		if (constructor == null) throw new ArgumentNullException(nameof(constructor));

		ParameterExpression instance = Expression.Parameter(typeof(object));
		ParameterExpression args = Expression.Parameter(typeof(object[]), "args");
		Expression[] pp = CompileParameters(constructor, args);
		Expression call = Expression.New(constructor, pp);
		if (constructor.ReflectedType is { IsValueType: true })
			call = Expression.Convert(call, typeof(object));

		return Expression.Lambda<Func<object?, object?[], object?>>(call, instance, args)
#if NETFRAMEWORK && DEBUG
			.Compile(DebugInfo)!;
#else
			.Compile();
#endif
	}

	private static Expression[] CompileParameters(MethodBase method, ParameterExpression args)
	{
		ParameterInfo[] ppInfo = method.GetParameters();
		var pp = new Expression[ppInfo.Length];
		for (int i = 0; i < ppInfo.Length; ++i)
		{
			Expression e = Expression.ArrayAccess(args, Expression.Constant(i));
			pp[i] = e.Type == ppInfo[i].ParameterType ? e: Expression.Convert(e, ppInfo[i].ParameterType);
		}
		return pp;
	}
}
