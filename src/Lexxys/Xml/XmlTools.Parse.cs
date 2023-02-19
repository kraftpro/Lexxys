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

		public static bool TryGetValue(XmlLiteNode? node, Type returnType, [NotNullWhen(true)] out object? result)
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
				return nodeParser == null ? false: nodeParser(node, returnType, out result);

			if (returnType.IsGenericType && __nodeGenericConstructor.TryGetValue(returnType.GetGenericTypeDefinition(), out nodeParser))
			{
				if (nodeParser == null)
					return false;
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
			result = null;
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

		#region Try Reflection

		private static bool TryReflection(XmlLiteNode node, Type returnType, [MaybeNullWhen(false)] out object? result)
		{
			result = null;
			if (node == null)
				return false;

			if (node.Elements.Count == 0 && node.Attributes.Count == 0)
				return TryGetValue(node.Value, returnType, out result);

			if (TryCollection(node, node.Elements, returnType, ref result))
				return true;

			var args = CollectArguments(node);

			var (missings, obj) = TryConstruct(returnType, args);
			if (obj == null)
				return false;

			result = obj;

			if (missings.Count > 0)
			{
				foreach (FieldInfo item in returnType.GetFields(BindingFlags.Instance | BindingFlags.Public))
				{
					SetFieldValue(node, item.Name, item.FieldType, missings, () => item.GetValue(obj), o => item.SetValue(obj, o));
				}
				foreach (PropertyInfo item in returnType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
				{
					if (item.CanWrite && item.GetSetMethod() != null && item.GetIndexParameters().Length == 0)
						SetFieldValue(node, item.Name, item.PropertyType, missings, () => item.GetValue(obj), o => item.SetValue(obj, o));
				}
			}

			return true;

			static Dictionary<string, object> CollectArguments(XmlLiteNode node)
			{
				var args = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
				// Collect attribues.
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
						else
							args[item.Name] = new List<object> { arg, item };
					}
				}
				return args;
			}

			static void SetFieldValue(XmlLiteNode node, string name, Type itemType, IReadOnlyCollection<string> missings, Func<object?> getter, Action<object?> setter)
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

		/// <summary>
		/// Create an instanse of the specified <paramref name="type"/>.
		/// </summary>
		/// <param name="type">Type of the class to be created.</param>
		/// <param name="arguments">List of arguments of type <see cref="String"/>, <see cref="XmlLiteNode"/>, or <see cref="List{XmlLiteNode}"/>.</param>
		/// <returns></returns>
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
				_key = arguments == null || arguments.Count == 0 ? String.Empty: String.Join(":", arguments.Keys.Select(o => o.ToUpperInvariant()));
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

			/// <summary>
			/// TRyes to invoke the constructor and create a required object.
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
					if (types.Entity != null && TryGetValue(s, types.Item, out var p) && CreateSimpleCollection(types!, p, ref parameter))
						return true;

					Log?.Trace($"{nameof(AttributedConstructor)}.{nameof(Invoke)}: Cannot parse parameter {name} of {type.Name} used to construct {_type.Name}.");
					return false;
				}
				if (value is XmlLiteNode x)
				{
					if (TryGetValue(x, type, out parameter))
						return true;

					var types = GetCollectionType(type);
					if (types.Entity != null && TryGetValue(x, types.Item, out var p) && CreateSimpleCollection(types!, p, ref parameter))
						return true;

					Log?.Trace($"{nameof(AttributedConstructor)}.{nameof(Invoke)}: Cannot convert parameter {name} to type {type.Name} used to construct {_type.Name}.");
					return false;
				}
				if (value is IList<object?> list)
				{
					var types = GetCollectionType(type);
					if (types.Entity != null && CreateCollection(types!, null, list.Select(o => ParseValue(o, types.Item)), ref parameter))
						return true;
				}

				Log?.Trace($"{nameof(AttributedConstructor)}.{nameof(Invoke)}: Cannot convert parameter {name} to type {type.Name} from {value.GetType().Name} used to construct {_type.Name}.");
				return false;

				(bool Success, object? Value) ParseValue(object? value, Type type)
				{
					if (value == null)
						return (true, Factory.DefaultValue(type));
					if (type.IsAssignableFrom(value.GetType()))
						return (true, value);
					if (value is string s)
						return (TryGetValue(s, type, out var v), v);
					if (value is XmlLiteNode x)
						return (TryGetValue(x, type, out var v), v);

					return default;
				}

				static bool CreateSimpleCollection((Type Entity, Type Collection, Type Item) types, object? value, ref object? parameter)
				{
					if (types.Collection.IsArray)
					{
						var arr = Array.CreateInstance(types.Item, 1);
						arr.SetValue(value, 0);
						parameter = arr;
						return true;
					}
					var obj = Factory.Construct(types.Entity!);
					MethodInfo? add = types.Collection.GetMethod("Add", new[] { types.Item });
					if (add == null)
						return false;
					Factory.Invoke(obj, add, value);
					parameter = obj;
					return true;
				}
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
				BitArray? index = null;
				for (int i = 0; i < constructors.Length; ++i)
				{
					var prm = parametersSet[i];
					if (prm.Length < selected?.Count)
						break;
					var idx = new BitArray(prm.Length);
					var names = new List<string>(prm.Length);
					for (int j = 0; j < prm.Length; ++j)
					{
						if (prm[j].Name == null)
							goto skip;
						idx[j] = arguments.ContainsKey(prm[j].Name!);
						if (idx[j])
						{
							names.Add(prm[j].Name!);
						}
						else
						{
							var plurar = Lingua.Plural(prm[j].Name!);
							if (plurar != prm[j].Name && arguments.ContainsKey(plurar) && !prm.Any(x => x.Name == plurar))
								names.Add(plurar);
							else if (!prm[j].IsOptional)
								goto skip;
						}
					}
					if (index == null || names.Count >= selected!.Count)
					{
						index = idx;
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
					Debug.Assert(index != null);
					Debug.Assert(constructor != null);

					var args = new Expression[index!.Length];
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

				var missings = new List<string>(arguments.Keys.Except(selected, StringComparer.OrdinalIgnoreCase));
				return new AttributedConstructor(type, Expression.Lambda<Func<object?[], object?>>(ctor, arg).Compile(), parms.ToArray(), missings);

				static object? DefaultParameterValue(ParameterInfo parameter)
				{
					object? value = null;
					if (parameter.ParameterType == typeof(DateTime))
						return default(DateTime);
					try { value = parameter.DefaultValue; } catch { }
					return value ?? Factory.DefaultValue(parameter.ParameterType);
				}
			}
		}

		private static (Type? Entity, Type Collection, Type Item) GetCollectionType(Type type) => __collectionType.GetOrAdd(type, t => GetCollectionType_(t));
		private static ConcurrentDictionary<Type, (Type? Entity, Type Collection, Type Item)> __collectionType = new ConcurrentDictionary<Type, (Type? Entity, Type Collection, Type Item)>();

		private static (Type? Entity, Type Collection, Type Item) GetCollectionType_(Type type)
		{
			if (type.IsArray)
				return (type, type, type.GetElementType()!);

			if (type.IsGenericType)
			{
				var tt = type.GenericTypeArguments;
				if (tt.Length == 1)
				{
					var collectionType = tt[0].MakeArrayType();
					if (type.IsAssignableFrom(collectionType))
						return (collectionType, collectionType, tt[0]);

					collectionType = typeof(List<>).MakeGenericType(tt);
					if (type.IsAssignableFrom(collectionType))
						return (collectionType, collectionType, tt[0]);

					collectionType = typeof(HashSet<>).MakeGenericType(tt);
					if (type.IsAssignableFrom(collectionType))
						return (collectionType, collectionType, tt[0]);
				}
				else if (tt.Length == 2)
				{
					var dictionaryType = typeof(Dictionary<,>).MakeGenericType(tt);
					if (type.IsAssignableFrom(dictionaryType))
					{
						var itemType = typeof(KeyValuePair<,>).MakeGenericType(tt);
						return (dictionaryType, typeof(ICollection<>).MakeGenericType(itemType), itemType);
					}
				}
			}

			var collectionInterface = type.GetInterfaces().FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ICollection<>));
			if (collectionInterface != null)
				return (type, collectionInterface, collectionInterface.GetGenericArguments()[0]);
			return (null, typeof(void), typeof(void));
		}

		private static bool CreateCollection((Type Entity, Type Collection, Type Item) types, IReadOnlyCollection<KeyValuePair<string, string>>? args, IEnumerable<(bool Success, object? Value)> items, ref object? parameter)
		{
			if (types.Collection.IsArray)
			{
				var list = new List<object?>(items.Where(o => o.Success).Select(o => o.Value));
				var arr = Array.CreateInstance(types.Item, list.Count);
				for (int i = 0; i < list.Count; ++i)
				{
					arr.SetValue(list[i], i);
				}
				parameter = arr;
				return true;
			}

			var obj = CreateCollectionInstance(types.Entity, args, types.Collection);
			MethodInfo? add = types.Collection.GetMethod("Add", new[] { types.Item });
			if (add == null)
				return false;

			foreach (var item in items)
			{
				if (item.Success)
					Factory.Invoke(obj, add, item.Value);
			}
			parameter = obj;
			return true;

			static object? CreateCollectionInstance(Type instanceType, IReadOnlyCollection<KeyValuePair<string, string>>? args, Type collectionType)
			{
				var instance = args == null || args.Count == 0 ? Factory.Construct(instanceType) : TryConstruct(instanceType, Fold(args));

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

		private static bool TryCollection(XmlLiteNode node, IEnumerable<XmlLiteNode> items, Type returnType, ref object? result)
		{
			var types = GetCollectionType(returnType);
			if (types.Entity == null)
				return false;

			return CreateCollection(types!, node.Attributes, items.Select(o => (TryGetValue(o, types.Item, out var v), v)), ref result);
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
}
