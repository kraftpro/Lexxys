using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

#pragma warning disable CA1307 // Specify StringComparison

namespace Lexxys.Xml;

public static partial class XmlTools
{
	private static bool TryReflection(XmlLiteNode? node, Type returnType, out object? result)
	{
		result = null;
		if (node == null)
			return false;

		if (node.Elements.Count == 0 && node.Attributes.Count == 0)
			return TryGetValue(node.Value, returnType, out result);

		var collectionType = GetCollectionType(returnType);
		if (collectionType != null && TryCollection(node, collectionType, ref result))
			return true;

		var (extra, args) = CollectArguments(node);
		var (missing, obj) = TryConstruct(returnType, args);
		if (obj == null)
			return false;

		result = obj;

		if (missing?.Count > 0 || extra != null)
		{
			foreach (FieldInfo item in returnType.GetFields(BindingFlags.Instance | BindingFlags.Public))
			{
				bool attribs = true;
				var itemName = ItemName(missing, item.Name);
				if (itemName == null)
				{
					itemName = ItemName(extra, item.Name);
					if (itemName == null)
						continue;
					attribs = false;
				}
				if (item.IsInitOnly)
				{
					var value = item.GetValue(obj);
					if (value != null)
						SetFieldValue(node, itemName, item.FieldType, () => value, null, attribs);
				}
				else
				{
					SetFieldValue(node, itemName, item.FieldType, () => item.GetValue(obj), o => item.SetValue(obj, o), attribs);
				}
			}
			foreach (PropertyInfo item in returnType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
			{
				bool attribs = true;
				var itemName = ItemName(missing, item.Name);
				if (itemName == null)
				{
					itemName = ItemName(extra, item.Name);
					if (itemName == null)
						continue;
					attribs = false;
				}
				if (item.CanWrite && item.GetSetMethod() != null)
				{
					if (item.GetIndexParameters().Length != 0)
						continue;
					SetFieldValue(node, itemName, item.PropertyType, () => item.GetValue(obj), o => item.SetValue(obj, o), attribs);
				}
				else if (item.CanRead && item.GetGetMethod() != null)
				{
					if (item.GetIndexParameters().Length != 0)
						continue;
					var value = item.GetValue(obj);
					if (value != null)
						SetFieldValue(node, itemName, item.PropertyType, () => value, null, attribs);
				}
			}
		}

		return true;

		static (List<string>? Extra, Dictionary<string, object> Args) CollectArguments(XmlLiteNode node)
		{
			var args = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
			List<string>? extra = null;
			// Collect attributes.
			foreach (var item in node.Attributes)
			{
				args[item.Key] = item.Value;
			}
			// Collect elements.
			foreach (XmlLiteNode item in node.Elements)
			{
				if (!args.TryGetValue(item.Name, out var arg))
				{
					args.Add(item.Name, item);
				}
				else
				{
					if (arg is IList<object> list)
						list.Add(item);
					else if (arg is XmlLiteNode)
						args[item.Name] = new List<object> { arg, item };
					else
						(extra ??= new List<string>()).Add(item.Name);
				}
			}
			return (extra, args);
		}

		static string? ItemName(IReadOnlyCollection<string>? collection, string fieldName)
		{
			if (collection == null)
				return null;
			if (collection.FirstOrDefault(o => String.Equals(o, fieldName, StringComparison.OrdinalIgnoreCase)) != null)
				return fieldName;

			var itemName = Lingua.Singular(fieldName);
			if (itemName != fieldName && collection.FirstOrDefault(o => String.Equals(o, itemName, StringComparison.OrdinalIgnoreCase)) != null)
				return itemName;

			itemName = Lingua.Plural(fieldName);
			if (itemName != fieldName && collection.FirstOrDefault(o => String.Equals(o, itemName, StringComparison.OrdinalIgnoreCase)) != null)
				return itemName;

			return null;
		}
	}

	private static void SetFieldValue(XmlLiteNode node, string itemName, Type itemType, Func<object?> getter, Action<object?>? setter, bool attributes)
	{
		var type = GetCollectionType(itemType);
		if (type == null)
		{
			if (setter != null)
				SetSimpleValue(node, itemName, itemType, setter, attributes);
			return;
		}

		List<XmlLiteNode>? elems = null;
		foreach (var item in node.Elements)
		{
			if (String.Equals(item.Name, itemName, StringComparison.OrdinalIgnoreCase))
				(elems ??= new()).Add(item);
		}
		var items = new List<object?>();
		if (attributes)
		{
			var attrib = node.Attributes.FirstOrDefault(o => String.Equals(o.Key, itemName, StringComparison.OrdinalIgnoreCase));
			if (attrib.Value != null && TryGetValue(attrib.Value, type.Item, out var x))
				items.Add(x);
		}

		var args = elems == null ? null: CollectItems(itemName, type, elems, items);
		if (items.Count == 0)
			return;

		var value = getter();
		if (CreateCollection(type, args, items, ref value))
			setter?.Invoke(value);

		return;

		static void SetSimpleValue(XmlLiteNode node, string itemName, Type itemType, Action<object?> setter, bool attributes)
		{
			if (attributes)
			{
				var attrib1 = node.Attributes.FirstOrDefault(o => String.Equals(o.Key, itemName, StringComparison.OrdinalIgnoreCase));
				if (attrib1.Value != null)
				{
					if (TryGetValue(attrib1.Value, itemType, out var x1))
						setter(x1);
				}
			}

			var e = node.Element(itemName, StringComparer.OrdinalIgnoreCase);
			if (!e.IsEmpty && TryGetValue(e, itemType, out var x))
				setter(x);
		}

		static IReadOnlyCollection<KeyValuePair<string, string>>? CollectItems(string itemName, CollectionType type, List<XmlLiteNode> elems, List<object?> items)
		{
			if (elems.Count > 1)
			{
				foreach (var item in elems)
				{
					if (TryGetValue(item, type.Item, out var i))
						items.Add(i);
				}
				return null;
			}

			// apples/item*
			// apples/apple*
			// apples

			var elem = elems[0];
			string singular;
			var nm = elem.FirstOrDefault("item", StringComparer.OrdinalIgnoreCase) != null ? "item":
				(singular = Lingua.Singular(itemName)) != itemName && elem.FirstOrDefault(singular, StringComparer.OrdinalIgnoreCase) != null ? singular: null;

			if (nm == null) // apples
			{
				if (TryGetValue(elem, type.Item, out var i))
					items.Add(i);
				return null;
			}

			// apples/item*
			// apples/apple*
			var args = elem.Attributes;
			foreach (var item in elem.Where(nm, StringComparer.OrdinalIgnoreCase))
			{
				if (TryGetValue(item, type.Item, out var i))
					items.Add(i);
			}
			return args;
		}
	}

	/// <summary>
	/// Create an instance of the specified <paramref name="type"/>.
	/// </summary>
	/// <param name="type">Type of the class to be created.</param>
	/// <param name="arguments">List of arguments of type <see cref="String"/>, <see cref="XmlLiteNode"/>, or <see cref="List{XmlLiteNode}"/>.</param>
	/// <returns></returns>
	private static (IReadOnlyCollection<string>? Missings, object? Value) TryConstruct(Type type, Dictionary<string, object> arguments)
	{
		if (arguments.Count == 0)
			return (Array.Empty<string>(), Factory.TryConstruct(type));

		var key = new ConstructorKey(type, arguments);
		if (!__attributedConstructors.TryGetValue(key, out var constructor))
			constructor = __attributedConstructors.GetOrAdd(key, AttributedConstructor.Create(type, arguments));
		return (constructor?.Missing, constructor?.Invoke(arguments));
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
			_key = arguments == null || arguments.Count == 0 ? String.Empty : String.Join(":", arguments.Keys.Select(o => o.ToUpperInvariant()));
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

		private AttributedConstructor(Type type, Func<object?[], object?> constructor, (string Name, Type Info)[] parameters, IReadOnlyList<string>? missing)
		{
			_type = type ?? throw new ArgumentNullException(nameof(type));
			_constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
			_parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
			Missing = missing;
		}

		public IReadOnlyList<string>? Missing { get; }

		/// <summary>
		/// Tries to invoke the constructor and create a required object.
		/// </summary>
		/// <param name="arguments">List of arguments of type String, XmlLiteNode, or List&lt;String or XmlLiteNode&gt;.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
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
				if (!arguments.TryGetValue(_parameters[i].Name, out var value) && !arguments.TryGetValue(Lingua.Plural(_parameters[i].Name), out value))
					return null;
				if (!TrySetParameterValue(value, _parameters[i].Name, _parameters[i].Type, ref values[i]))
					return null;
			}
			return _constructor.Invoke(values);
		}

		private bool TrySetParameterValue(object? value, string name, Type type, ref object? parameter)
		{
			if (value is null)
			{
				parameter = Factory.DefaultValue(type);
				return true;
			}
			var argType = value.GetType();
			if (type.IsAssignableFrom(argType))
			{
				parameter = value;
				return true;
			}
			if (value is string s)
			{
				if (TryGetValue(s, type, out parameter))
					return true;

				var types = GetCollectionType(type);
				if (types != null && TryGetValue(s, types.Item, out var p) && CreateSimpleCollection(types, p, ref parameter))
					return true;

				Log?.Trace($"{nameof(AttributedConstructor)}.{nameof(Invoke)}: Cannot parse parameter {name} of {type.Name} used to construct {_type.Name}.");
				return false;
			}
			if (value is XmlLiteNode x)
			{
				if (TryGetValue(x, type, out parameter))
					return true;

				var types = GetCollectionType(type);
				if (types != null && TryGetValue(x, types.Item, out var p) && CreateSimpleCollection(types, p, ref parameter))
					return true;

				Log?.Trace($"{nameof(AttributedConstructor)}.{nameof(Invoke)}: Cannot convert parameter {name} to type {type.Name} used to construct {_type.Name}.");
				return false;
			}
			if (value is IList<object?> list)
			{
				var types = GetCollectionType(type);
				if (types != null)
				{
					var args = new List<object?>();
					foreach (var item in list)
					{
						if (TryParseValue(item, types.Item, out var v))
							args.Add(v);
					}
					if (CreateCollection(types, null, args, ref parameter))
						return true;
				}
			}

			Log?.Trace($"{nameof(AttributedConstructor)}.{nameof(Invoke)}: Cannot convert parameter {name} to type {type.Name} from {value.GetType().Name} used to construct {_type.Name}.");
			return false;

			static bool TryParseValue(object? value, Type type, out object? result)
			{
				if (value == null)
				{
					result = Factory.DefaultValue(type);
					return true;
				}
				if (type.IsInstanceOfType(value))
				{
					result = value;
					return true;
				}
				if (value is string s)
					return TryGetValue(s, type, out result);
				if (value is XmlLiteNode x)
					return TryGetValue(x, type, out result);
				result = null;
				return false;
			}
		}

		private static bool CreateSimpleCollection(CollectionType types, object? value, ref object? result)
		{
			if (types.Collection.IsArray)
			{
				var arr = Array.CreateInstance(types.Item, 1);
				arr.SetValue(value, 0);
				result = arr;
				return true;
			}
			var obj = Factory.Construct(types.Entity);
			if (!AppendItems(obj, types, new[] { value }))
				return false;

			result = obj;
			return true;
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
			List<string>? selected = null;
			BitArray? optional = null;
			for (int i = 0; i < constructors.Length; ++i)
			{
				var prm = parametersSet[i];
				if (prm.Length < selected?.Count)
					break;
				var opt = new BitArray(prm.Length);
				var names = new List<string>(prm.Length);
				for (int j = 0; j < prm.Length; ++j)
				{
					var name = prm[j].Name;
					if (name == null)
						goto skip;

					if (!arguments.ContainsKey(name))
					{
						var plural = Lingua.Plural(name);
						if (plural != name && arguments.ContainsKey(plural) && !prm.Any(x => String.Equals(x.Name, plural, StringComparison.OrdinalIgnoreCase)))
						{
							name = plural;
						}
						else
						{
							var singular = Lingua.Singular(name);
							if (singular != name && arguments.ContainsKey(singular) && !prm.Any(x => String.Equals(x.Name, singular, StringComparison.OrdinalIgnoreCase)))
							{
								name = singular;
							}
							else
							{
								if (!prm[j].IsOptional)
									goto skip;
								opt[j] = true;
							}
						}
					}
					if (!opt[j])
						names.Add(name);

					//idx[j] = arguments.ContainsKey(prm[j].Name!);
					//if (idx[j])
					//{
					//	names.Add(prm[j].Name!);
					//}
					//else
					//{
					//	var plural = Lingua.Plural(prm[j].Name!);
					//	if (plural != prm[j].Name && arguments.ContainsKey(plural) && prm.All(x => x.Name != plural))
					//	{
					//		names.Add(plural);
					//		idx[j] = true;
					//	}
					//	else if (!prm[j].IsOptional)
					//		goto skip;
					//}
				}
				if (optional == null || names.Count >= selected!.Count)
				{
					optional = opt;
					selected = names;
					constructor = constructors[i];
					parameters = prm;
				}
			skip:;
			}

			if (constructor == null)
			{
				if (!type.IsValueType)
				{
					Log?.Trace($"{nameof(AttributedConstructor)}.{nameof(Create)}: Cannot find a constructor for {type.Name} and arguments: {String.Join(", ", arguments.Keys)}");
					return null;
				}
				parameters = Array.Empty<ParameterInfo>();
			}
			Debug.Assert(parameters != null);

			ParameterExpression arg = Expression.Parameter(typeof(object[]));
			Expression ctor;
			var parms = new List<(string Name, Type Type)>();
			if (selected!.Count == 0 && type.IsValueType)
			{
				ctor = Expression.TypeAs(Expression.Default(type), typeof(object));
			}
			else if (parameters!.Length == 0)
			{
				ctor = Expression.New(type);
			}
			else
			{
				Debug.Assert(optional != null);
				Debug.Assert(constructor != null);

				var args = new Expression[optional!.Length];
				for (int i = 0; i < optional.Length; ++i)
				{
					if (optional[i])
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

			var missing = arguments.Count == selected.Count ? null: new List<string>(arguments.Keys.Except(selected, StringComparer.OrdinalIgnoreCase));
			return new AttributedConstructor(type, Expression.Lambda<Func<object?[], object?>>(ctor, arg).Compile(), parms.ToArray(), missing);

			static object? DefaultParameterValue(ParameterInfo parameter)
			{
				object? value = null;
				if (parameter.ParameterType == typeof(DateTime))
					return default(DateTime);
				#pragma warning disable CA1031 // Do not catch general exception types
				try { value = parameter.DefaultValue; } catch { }
				#pragma warning restore CA1031 // Do not catch general exception types
				return value ?? Factory.DefaultValue(parameter.ParameterType);
			}
		}
	}

	private static bool AppendItems(object collection, CollectionType type, IReadOnlyList<object?> items)
	{
		MethodInfo? add = type.Collection.GetMethod("Add", new[] { type.Item });
		if (add == null)
			return false;

		foreach (var item in items)
		{
			Factory.Invoke(collection, add, item);
		}
		return true;
	}

	private static bool CreateCollection(CollectionType type, IReadOnlyCollection<KeyValuePair<string, string>>? args, IReadOnlyList<object?> items, ref object? result)
	{
		if (type.Collection.IsArray)
		{
			if (result != null)
				return false;
			var arr = Array.CreateInstance(type.Item, items.Count);
			for (int i = 0; i < items.Count; ++i)
			{
				arr.SetValue(items[i], i);
			}
			result = arr;
			return true;
		}

		var created = result == null;
		var obj = result ?? CreateCollectionInstance(type.Entity, args, type.Collection);
		if (obj == null)
			return false;
		if (!AppendItems(obj, type, items))
			return false;

		result = obj;
		return created;

		static object? CreateCollectionInstance(Type instanceType, IReadOnlyCollection<KeyValuePair<string, string>>? args, Type collectionType)
		{
			var instance = args == null || args.Count == 0 ? Factory.Construct(instanceType) : TryConstruct(instanceType, Fold(args)).Value;

			return instance == null || IsReadOnlyCollection(instance, GetReadOnlyGetter(collectionType)) ? null : instance;

			static MethodInfo? GetReadOnlyGetter(Type collectionType)
			{
				var ro = collectionType.GetProperty("IsReadOnly");
				return ro != null && ro.CanRead ? ro.GetGetMethod() : null;
			}

			static bool IsReadOnlyCollection(object value, MethodInfo? getter)
			{
				return getter != null && Object.Equals(Factory.Invoke(value, getter), true);
			}

			static Dictionary<string, object> Fold(IReadOnlyCollection<KeyValuePair<string, string>> args)
			{
				var d = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
				foreach (var arg in args)
				{
					d[arg.Key] = arg.Value;
				}
				return d;
			}
		}
	}

	private static bool TryCollection(XmlLiteNode node, CollectionType collectionType, ref object? result)
	{
		var itemName = GetItemName(node);
		if (itemName == null)
		{
			if (collectionType.IsDictionary && node.Elements.Count > 0)
			{
				var pairs = new List<object>();
				foreach (var item in node.Elements)
				{
					if (TryKeyValuePair(item, collectionType.Item, out var i) && i != null)
						pairs.Add(i);
				}
				return CreateCollection(collectionType, node.Attributes, pairs, ref result);
			}

			if (TryGetValue(node, collectionType.Item, out var single))
				return CreateCollection(collectionType, null, new[] { single }, ref result);

			return CreateCollection(collectionType, node.Attributes, Array.Empty<object?>(), ref result);
		}

		var items = new List<object?>();
		foreach (var item in node.Elements)
		{
			if (TryGetValue(item, collectionType.Item, out var i))
				items.Add(i);
		}

		return CreateCollection(collectionType, node.Attributes, items, ref result);

		static string? GetItemName(XmlLiteNode node)
		{
			if (node.Elements.Count == 0)
				return null;

			var itemName = "item";
			if (!node.Element(itemName, StringComparer.OrdinalIgnoreCase).IsEmpty)
				return node.Elements.All(o => String.Equals(itemName, o.Name, StringComparison.OrdinalIgnoreCase)) ? itemName: null;

			itemName = Lingua.Singular(node.Name);
			if (!node.Element(itemName, StringComparer.OrdinalIgnoreCase).IsEmpty)
				return node.Elements.All(o => String.Equals(itemName, o.Name, StringComparison.OrdinalIgnoreCase)) ? itemName: null;

			return null;
		}
	}

	//private static bool TryCollection(XmlLiteNode node, IEnumerable<XmlLiteNode>? items, CollectionType collectionType, ref object? result)
	//{
	//	if (items is null)
	//	{
	//		if (node.Elements.Count == 0)
	//		{
	//			return TryGetValue(node, collectionType.Item, out var single) ?
	//				CreateCollection(collectionType, null, new[] { single }, ref result) :
	//				CreateCollection(collectionType, node.Attributes, Array.Empty<object?>(), ref result);
	//		}
	//		items = node.Elements;
	//	}

	//	var convertedItems = new List<object?>();
	//	foreach (var item in items)
	//	{
	//		if (TryGetValue(item, collectionType.Item, out var i))
	//			convertedItems.Add(i);
	//	}
	//	return CreateCollection(collectionType, node.Attributes, convertedItems, ref result);
	//}

	#region Collection Type

	private class CollectionType
	{
		public Type Entity { get; }
		public Type Collection { get; }
		public Type Item { get; }
		public bool IsDictionary { get; }

		public CollectionType(Type entity, Type collection, Type item, bool isDictionary = false)
		{
			Entity = entity;
			Collection = collection;
			Item = item;
			IsDictionary = isDictionary;
		}
	}

	private static CollectionType? GetCollectionType(Type type) => __collectionType.GetOrAdd(type, GetCollectionType_);
	private static readonly ConcurrentDictionary<Type, CollectionType?> __collectionType = new ConcurrentDictionary<Type, CollectionType?>();

	private static CollectionType? GetCollectionType_(Type type)
	{
		if (type.IsArray)
			return new CollectionType(type, type, type.GetElementType()!);

		if (type.IsGenericType)
		{
			var tt = type.GenericTypeArguments;
			if (tt.Length == 1)
			{
				var collectionType = tt[0].MakeArrayType();
				if (type.IsAssignableFrom(collectionType))
					return new CollectionType(collectionType, collectionType, tt[0]);

				collectionType = typeof(List<>).MakeGenericType(tt);
				if (type.IsAssignableFrom(collectionType))
					return new CollectionType(collectionType, collectionType, tt[0]);

				collectionType = typeof(HashSet<>).MakeGenericType(tt);
				if (type.IsAssignableFrom(collectionType))
					return new CollectionType(collectionType, collectionType, tt[0]);
			}
			else if (tt.Length == 2)
			{
				var dictionaryType = typeof(Dictionary<,>).MakeGenericType(tt);
				if (type.IsAssignableFrom(dictionaryType))
				{
					var itemType = typeof(KeyValuePair<,>).MakeGenericType(tt);
					return new CollectionType(dictionaryType, typeof(ICollection<>).MakeGenericType(itemType), itemType, true);
				}
			}
		}

		var collectionInterface = type.GetInterfaces().FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ICollection<>));
		if (collectionInterface != null)
			return new CollectionType(type, collectionInterface, collectionInterface.GetGenericArguments()[0]);
		return null;
	}

	#endregion
}
