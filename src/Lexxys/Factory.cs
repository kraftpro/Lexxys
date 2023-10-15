// Lexxys Infrastructural library.
// file: Factory.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Lexxys;

using Configuration;
using Tokenizer;

public static class Factory
{
	public const string ConfigurationRoot = "lexxys.factory";
	public const string ConfigurationImport = ConfigurationRoot + ".import";
	public const string ConfigurationSkip = ConfigurationRoot + ".ignore";
	public const string ConfigurationSynonyms = ConfigurationRoot + ".synonyms.*";

	public static readonly object Void = new object();
	public static readonly object[] NoArgs = [];
#if NETFRAMEWORK && DEBUG
	public static readonly DebugInfoGenerator DebugInfo = DebugInfoGenerator.CreatePdbGenerator();
#endif

	private static readonly ConcurrentDictionary<Func<Type, bool>, IEnumerable<Type>> __foundTypesP = new ConcurrentDictionary<Func<Type, bool>, IEnumerable<Type>>();
	private static readonly ConcurrentDictionary<Type, IEnumerable<Type>> __foundTypesC = new ConcurrentDictionary<Type, IEnumerable<Type>>();
	private static readonly ConcurrentDictionary<Type, IEnumerable<Type>> __foundTypesA = new ConcurrentDictionary<Type, IEnumerable<Type>>();
	private static volatile ConcurrentBag<Assembly>? __assemblies;
	private static volatile ConcurrentBag<Assembly>? __systemAssemblies;
	private static volatile bool __assembliesImported;
	private static string[]? __systemAssemblyNames;
	private static readonly object SyncRoot = new object();

	private static readonly ConcurrentDictionary<MemberInfo, Func<object?, object?[], object?>?> __compiledMethods = new ConcurrentDictionary<MemberInfo, Func<object?, object?[], object?>?>();

	public static IEnumerable<Assembly> DomainAssemblies
	{
		get
		{
			CollectOrImportAssemblies();
			return __assemblies!;
		}
	}

	public static IEnumerable<Assembly> SystemAssemblies
	{
		get
		{
			CollectAssemblies();
			return __systemAssemblies!;
		}
	}

	private static void OnAssemblyLoad(Assembly asm)
	{
		AssemblyLoad?.Invoke(null, new AssemblyLoadEventArgs(asm));
	}

	public static event EventHandler<AssemblyLoadEventArgs>? AssemblyLoad;

	#region Assemblies

	private static string[] SystemAssemblyNames()
	{
		if (!Statics.Instance.IsInitialized)
			return DefaultSystemAssemblyNames;
		var systemNamesConfig = Statics.TryGetService<IConfigSection>()?.GetCollection<string>(ConfigurationSkip);
		var systemNames = systemNamesConfig?.Value;
		if (systemNames == null || systemNames.Count == 0)
			return DefaultSystemAssemblyNames;

		var ss = new List<string>(systemNames
			.SelectMany(o => o.Split(',', ';'))
			.Select(o => o.TrimToNull())
			.Where(o => o != null)!);
		if (ss.Count == 0)
			return DefaultSystemAssemblyNames;

		for (int i = 0; i < DefaultSystemAssemblyNames.Length; ++i)
		{
			if (string.IsNullOrEmpty(ss[i]))
				continue;

			if (!ss.Contains(DefaultSystemAssemblyNames[i]))
				ss.Add(DefaultSystemAssemblyNames[i]);
		}
		return ss.ToArray();
	}
	private static readonly string[] DefaultSystemAssemblyNames = ["CppCodeProvider", "WebDev.", "SMDiagnostics", "mscor", "vshost", "System", "Microsoft", "Windows", "Presentation", "netstandard"];

	private static bool IsSystemAssembly(Assembly asm)
	{
#if !NETCOREAPP
		if (asm.GlobalAssemblyCache)
			return true;
#endif
		string? name = asm.FullName;
		return name != null && name.IndexOf("Version=0.0.0.0", StringComparison.OrdinalIgnoreCase) >= 0 || Array.FindIndex(__systemAssemblyNames!, s => name != null && name.StartsWith(s, StringComparison.Ordinal)) >= 0;
	}

	private static void CollectAssemblies()
	{
		if (__assemblies == null)
		{
			lock (SyncRoot)
			{
				if (__assemblies == null)
					CollectAssembliesInternal();
			}
		}
	}

	public static bool AssembliesImported => __assembliesImported;

	private static void CollectOrImportAssemblies()
	{
		if (__assemblies == null)
		{
			lock (SyncRoot)
			{
				if (__assemblies == null)
					CollectAssembliesInternal();
				if (!__assembliesImported)
					ImportAssembliesInternal();
			}
		}
		else if (!__assembliesImported)
		{
			lock (SyncRoot)
			{
				if (!__assembliesImported)
					ImportAssembliesInternal();
			}
		}
	}

	private static void CollectAssembliesInternal()
	{
		__systemAssemblyNames = SystemAssemblyNames();
		AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
		AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
		var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
		__assemblies = new ConcurrentBag<Assembly>(assemblies.Where(a => !IsSystemAssembly(a)));
		__systemAssemblies = new ConcurrentBag<Assembly>(assemblies.Where(IsSystemAssembly));
	}

	private static void ImportAssembliesInternal()
	{
		if (!__assembliesImported)
		{
			ImportRestAssemblies();
			Lxx.ConfigurationChanged += OnConfigChanged;
			__assembliesImported = true;
		}
	}

	private static void CurrentDomain_AssemblyLoad(object? sender, AssemblyLoadEventArgs args)
	{
		Assembly asm = args.LoadedAssembly;
		CollectAssemblies();
		if (IsSystemAssembly(asm))
		{
			__systemAssemblies!.Add(asm);
		}
		else
		{
			__assemblies!.Add(asm);
			__foundTypesP.Clear();
			__foundTypesC.Clear();
			__foundTypesA.Clear();
			OnAssemblyLoad(asm);
		}
	}

	private static Assembly? CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
	{
		if (sender is not AppDomain domain)
			return null;
		foreach (var assembly in domain.GetAssemblies())
		{
			if (assembly.FullName == args.Name)
				return assembly;
		}
		return null;
	}

	private static void ImportRestAssemblies()
	{
		if (!Statics.Instance.IsInitialized)
			return;
		var importConfig = Statics.TryGetService<IConfigSection>()?.GetCollection<string>(ConfigurationImport);
		var assemblies = importConfig?.Value;
		if (assemblies == null)
			return;

		foreach (string assemblyName in assemblies)
		{
			TryLoadAssembly(assemblyName, false);
		}
	}

	private static void OnConfigChanged(object? sender, ConfigurationEventArgs e)
	{
		ImportRestAssemblies();
		ResetSynonyms();
	}


	public static Assembly? TryLoadAssembly(string? assemblyName, bool throwOnError)
	{
		if (assemblyName is not { Length: > 0 })
			if (throwOnError)
				throw new ArgumentNullException(nameof(assemblyName));
			else
				return null;

		string? file = null;
		try
		{
			file = Path.Combine(Lxx.HomeDirectory, assemblyName);
			return File.Exists(file) ? Assembly.LoadFrom(file): Assembly.Load(assemblyName);
		}
		catch (Exception flaw)
		{
			SystemLog.WriteErrorMessage("Lexxys.Factory.TryLoadAssembly", flaw, new OrderedBag<string, object?> { { "assemblyName", assemblyName }, { "file", file } });
			if (throwOnError)
				throw;
			return null;
		}
	}

	public static Assembly LoadAssembly(string assemblyName)
	{
		return TryLoadAssembly(assemblyName, true)!;
	}

	#endregion

	public static IEnumerable<Type> Types(Func<Type, bool> predicate)
	{
		return __foundTypesP.GetOrAdd(predicate, p => ReadOnly.WrapCopy(DomainAssemblies.SelectMany(asm => asm.SelectTypes(p)))!);
	}

	public static IEnumerable<Type> Types(Type type, bool cacheResults = false)
	{
		if (type == null) throw new ArgumentNullException(nameof(type));
		return cacheResults ? __foundTypesA.GetOrAdd(type, t => ReadOnly.WrapCopy(
			DomainAssemblies.SelectMany(asm => asm.SelectTypes(o => !o.IsInterface && !o.IsAbstract && t.IsAssignableFrom(o))))!):
			DomainAssemblies.SelectMany(asm => asm.SelectTypes(o => !o.IsInterface && !o.IsAbstract && type.IsAssignableFrom(o)));
	}

	private static IEnumerable<Type> SelectTypes(this Assembly assembly, Func<Type, bool>? predicate)
	{
		IEnumerable<Type> list;
		try
		{
			list = assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException flaw)
		{
			list = flaw.Types.Where(t => t != null)!;
		}
		return predicate == null ? list: list.Where(predicate);
	}

	public static IEnumerable<Type> Classes(Type type, bool cacheResults = false)
	{
		if (type == null)
			throw new ArgumentNullException(nameof(type));

		return cacheResults ?
			__foundTypesC.GetOrAdd(type, key => ReadOnly.WrapCopy(Classes(key, DomainAssemblies))!):
			Classes(type, DomainAssemblies);
	}

	public static IEnumerable<Type> Classes(Type type, params Assembly[] assemblies)
	{
		return Classes(type, (IEnumerable<Assembly>)assemblies);
	}

	public static IEnumerable<Type> Classes(Type type, IEnumerable<Assembly> assemblies)
	{
		if (type is null) throw new ArgumentNullException(nameof(type));
		if (assemblies is null) throw new ArgumentNullException(nameof(assemblies));

		return type.IsInterface ?
			assemblies.SelectMany(asm => asm.SelectTypes(t => t is { IsInterface: false, IsAbstract: false } && Array.IndexOf(t.GetInterfaces(), type) >= 0)):
			assemblies.SelectMany(asm => asm.SelectTypes(t => t is { IsInterface: false, IsAbstract: false } && type.IsAssignableFrom(t)));
	}

	public static IEnumerable<MethodInfo> Constructors(Type type, string? methodName, params Type[]? types)
	{
		if (type is null) throw new ArgumentNullException(nameof(type));
		if (methodName is not { Length: > 0 }) throw new ArgumentNullException(nameof(methodName));

		types ??= Type.EmptyTypes;
		return Factory.Types(type)
			.Select(t => t.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null)!)
			.Where(m => m != null && type.IsAssignableFrom(m.ReturnType));
	}

	public static Type? GetType(string? typeName)
	{
		return typeName == null || (typeName = typeName.Trim()).Length == 0 ? null: GetSynonym(typeName) ?? GetTypeInternal(typeName);
	}

	public static Type? ParseTypeName(string typeName) => TypeNameParser.Parse(typeName);

	public static void ResetSynonyms()
	{
		__synonymsLoaded = false;
	}

	public static void SetSynonym(string? name, Type? type)
	{
		string? key = name?.Replace(" ", "");
		if (key is not { Length: >0 })
			return;
		lock (SyncRoot)
		{
			if (type == null)
			{
				__typesSynonyms.Remove(key);
				__typesSynonyms.Remove(key + "?");
			}
			else
			{
				__typesSynonyms[key] = type;
				if (!IsNullableType(type) && type != typeof(void))
					__typesSynonyms[key + "?"] = typeof(Nullable<>).MakeGenericType(type);
			}
		}
	}

	private static Type? GetSynonym(string name)
	{
		if (!__synonymsLoaded)
		{
			lock (SyncRoot)
			{
				if (!__synonymsLoading)
				{
					__synonymsLoading = true;
					var synonymsConfig = Statics.TryGetService<IConfigSection>()?.GetCollection<KeyValuePair<string?, string?>>(ConfigurationSynonyms);
					var synonyms = synonymsConfig?.Value;
					if (synonyms != null)
					{
						foreach (var item in synonyms)
						{
							if (item.Key == null || item.Value == null)
								continue;
							var key = item.Key.Replace(" ", "");
							if (key.Length == 0)
								continue;
							var value = item.Value.Replace(" ", "");
							if (value.Length == 0)
								continue;
							if (!__typesSynonyms.TryGetValue(value, out var type))
							{
								type = GetTypeInternal(item.Value);
								if (type == null)
									continue;
							}
							__typesSynonyms[key] = type;
							if (!IsNullableType(type) && type != typeof(void))
								__typesSynonyms[key + "?"] = typeof(Nullable<>).MakeGenericType(type);
						}
					}
					__synonymsLoaded = true;
				}
			}
		}
		return __typesSynonyms.GetValueOrDefault(name.Replace(" ", ""));
	}

	#region Types synonyms table
	private static bool __synonymsLoaded;
	private static bool __synonymsLoading;
	private static readonly Dictionary<string, Type> __typesSynonyms = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
		{
			{ "bool",		typeof(bool) },
			{ "byte",		typeof(byte) },
			{ "sbyte",		typeof(sbyte) },
			{ "short",		typeof(short) },
			{ "ushort",		typeof(ushort) },
			{ "integer",	typeof(int) },
			{ "int",		typeof(int) },
			{ "id",			typeof(int) },
			{ "uint",		typeof(uint) },
			{ "long",		typeof(long) },
			{ "ulong",		typeof(ulong) },
			{ "decimal",	typeof(decimal) },
			{ "fixed",		typeof(decimal) },
			{ "float",		typeof(double) },
			{ "double",		typeof(double) },
			{ "single",		typeof(float) },
			{ "string",		typeof(string) },
			{ "date"	,	typeof(DateTime) },
			{ "datetime",	typeof(DateTime) },
			{ "timespan",	typeof(TimeSpan) },
			{ "time",		typeof(TimeSpan) },
			{ "guid",		typeof(Guid) },
			{ "uuid",		typeof(Guid) },
			{ "type",		typeof(Type) },
			{ "void",		typeof(void) },

			{ "bool?",		typeof(bool?) },
			{ "byte?",		typeof(byte?) },
			{ "sbyte?",		typeof(sbyte?) },
			{ "short?",		typeof(short?) },
			{ "ushort?",	typeof(ushort?) },
			{ "integer?",	typeof(int?) },
			{ "int?",		typeof(int?) },
			{ "id?",		typeof(int?) },
			{ "uint?",		typeof(uint?) },
			{ "long?",		typeof(long?) },
			{ "ulong?",		typeof(ulong?) },
			{ "decimal?",	typeof(decimal?) },
			{ "fixed?",		typeof(decimal?) },
			{ "float?",		typeof(double?) },
			{ "double?",	typeof(double?) },
			{ "single?",	typeof(float?) },
			{ "string?",	typeof(string) },
			{ "datetime?",	typeof(DateTime?) },
			{ "date?",		typeof(DateTime?) },
			{ "timespan?",	typeof(TimeSpan?) },
			{ "time?",		typeof(TimeSpan?) },
			{ "guid?",		typeof(Guid?) },
			{ "uuid?",		typeof(Guid?) },
		};
	#endregion

	#region Type name parser

	private readonly ref struct TypeNameParser
	{
		/*

		full_name			:= short_name ('@' assembly | [ ',' assembly [ ',' option ]* ])

		short_name			:= dotted_name [ generic_definition ] [ nullable_suffix ] pointer_suffix* array_suffix*
		dotted_name			:= NAME ('.' NAME)*
		nullable_suffix		:= '?'
		pointer_suffix		:= '*'
		array_suffix		:= '[' (',')+ ']'

		generic_definition	:= generic_suffix
							|  generic_suffix? '[' param_name (',' param_name)* ']'
							|  generic_suffix? '<' param_name (',' param_name)* '>'
		generic_suffix		:= '^' NUMBER

		param_name			:= '[' full_name ']'
							|	short_name
		assembly			:= dotted_name
		option				:= NAME '=' VALUE
		*/

		private const int COMMA = 1;
		private const int ASTERISK = 3;
		private const int OPEN_SQRBRC = 4;
		private const int CLOSE_SQRBRC = 5;
		private const int BEGIN_GENERICS = 6;
		private const int END_GENERICS = 7;
		private const int GENERIC_SUFFIX = 8;
		private const int QUESTION = 9;
		private const int EQUAL = 10;
		private const int AT = 11;

		private static readonly LexicalTokenRule[] __rules =
		[
			new WhiteSpaceTokenRule(),
			new NameTokenRule().WithNameRecognition(
				nameStart: o => o == '$' || o == '_' || Char.IsLetter(o),
				namePart: o => o == '$' || o == '_' || o == '.' || o == '+' || Char.IsLetterOrDigit(o),
				nameEnd: o => o == '$' || o == '_' || Char.IsLetterOrDigit(o),
				beginning: "@$" + NameTokenRule.DefaultBeginning,
				extra: true
				),
			new NumericTokenRule(NumericTokenStyles.Ordinal),
			new SequenceTokenRule()
				.Add(COMMA, ",")
				.Add(ASTERISK, "*")
				.Add(OPEN_SQRBRC, "[")
				.Add(CLOSE_SQRBRC, "]")
				.Add(BEGIN_GENERICS, "<")
				.Add(END_GENERICS, ">")
				.Add(GENERIC_SUFFIX, "`")
				.Add(QUESTION, "?")
				.Add(EQUAL, "=")
				.Add(AT, "@"),
		];

		public static Type? Parse(string? value)
		{
			if (value == null)
				return null;
			var stream = new CharStream(value);
			return new TypeNameParser(false).Parse(ref stream, true)?.MakeType();
		}

		private readonly TokenScanner _scanner;
		private readonly OneBackFilter _back;

		private TypeNameParser(bool _)
		{
			_back = new OneBackFilter();
			_scanner = new TokenScanner([_back], __rules);
		}

		private TypePartsTree? Parse(ref CharStream stream, bool fullName)
		{
			var t = _scanner.Next(ref stream);
			if (!t.Is(LexicalTokenType.IDENTIFIER))
				return null;

			string name = t.GetString(stream);
			int generics = 0;
			bool nullable = false;
			int pointer = 0;
			List<int>? rank = null;
			List<TypePartsTree>? parameters = null;

			t = _scanner.Next(ref stream);
			if (t.Is(LexicalTokenType.SEQUENCE, GENERIC_SUFFIX))
			{
				t = _scanner.Next(ref stream);
				if (!t.Is(LexicalTokenType.NUMERIC))
					return null;
				if (!Int32.TryParse(t.GetString(stream), out generics))
					return null;
				t = _scanner.Next(ref stream);
			}
			if (t.Is(LexicalTokenType.SEQUENCE, BEGIN_GENERICS, OPEN_SQRBRC))
			{
				int terminal = t.TokenType.Item + 1;
				var token = _scanner.Next(ref stream);
				if (token.Is(LexicalTokenType.SEQUENCE, COMMA, terminal))
				{
					int count = 1;
					while (!token.Is(LexicalTokenType.SEQUENCE, terminal))
					{
						if (!_scanner.Next(ref stream).Is(LexicalTokenType.SEQUENCE, COMMA, terminal))
							return null;
						++count;
					}
					if (terminal == BEGIN_GENERICS + 1)
						generics = count;
					else
						(rank = new List<int>()).Add(count);
				}
				else
				{
					_back.Back();
					parameters = ParseParameters(ref stream, terminal);
					if (parameters == null)
						return null;
				}
				t = _scanner.Next(ref stream);
			}
			if (t.Is(LexicalTokenType.SEQUENCE, QUESTION))
			{
				if (rank != null)
					return null;
				nullable = true;
				t = _scanner.Next(ref stream);
			}
			while (t.Is(LexicalTokenType.SEQUENCE, ASTERISK))
			{
				if (rank != null)
					return null;
				++pointer;
				t = _scanner.Next(ref stream);
			}
			while (t.Is(LexicalTokenType.SEQUENCE, OPEN_SQRBRC))
			{
				int count = 0;
				do
				{
					++count;
					t = _scanner.Next(ref stream);
					if (!t.Is(LexicalTokenType.SEQUENCE, COMMA, CLOSE_SQRBRC))
						return null;
				} while (t.Is(LexicalTokenType.SEQUENCE, COMMA));
				(rank ??= new List<int>()).Add(count);
				t = _scanner.Next(ref stream);
			}

			string? assembly = null;
			OrderedBag<string, string>? options = null;

			if (fullName && t.Is(LexicalTokenType.SEQUENCE, COMMA))
			{
				t = _scanner.Next(ref stream);
				if (!t.Is(LexicalTokenType.IDENTIFIER))
					return null;
				assembly = t.GetString(stream);
				t = _scanner.Next(ref stream);
				while (t.Is(LexicalTokenType.SEQUENCE, COMMA))
				{
					do
					{
						t = _scanner.Next(ref stream);
					} while (t.Is(LexicalTokenType.SEQUENCE, COMMA));
					if (!t.Is(LexicalTokenType.IDENTIFIER))
						return null;
					string n = t.GetString(stream);
					if (!_scanner.Next(ref stream).Is(LexicalTokenType.SEQUENCE, EQUAL))
						return null;
					int i = stream.IndexOf(c => !Char.IsWhiteSpace(c));
					int j = stream.IndexOf(c => c != '.' && c != '_' && c != '-' && !Char.IsLetterOrDigit(c), i);
					if (j == i)
						return null;
					(options ??= new OrderedBag<string, string>(StringComparer.OrdinalIgnoreCase))[n] = stream.Substring(i, j - i);
					stream.Forward(j);
					t = _scanner.Next(ref stream);
				}
			}
			else if (t.Is(LexicalTokenType.SEQUENCE, AT))
			{
				t = _scanner.Next(ref stream);
				if (!t.Is(LexicalTokenType.IDENTIFIER))
					return null;
				assembly = t.GetString(stream);
				_scanner.Next(ref stream);
			}
			var tree = new TypePartsTree(
				name: name,
				assembly: assembly,
				pointerCount: pointer,
				isNullable: nullable,
				arrayRank: rank,
				genericsCount: generics,
				parameters: parameters,
				options: options);
			return tree;
		}

		private List<TypePartsTree>? ParseParameters(ref CharStream stream, int terminal)
		{
			bool fullName = _scanner.Next(ref stream).Is(LexicalTokenType.SEQUENCE, OPEN_SQRBRC);
			if (!fullName)
				_back.Back();
			TypePartsTree? item = Parse(ref stream, fullName);
			if (item == null)
				return null;
			if (fullName)
				if (!_back.Value.Is(LexicalTokenType.SEQUENCE, CLOSE_SQRBRC))
					return null;
				else
					_scanner.Next(ref stream);

			List<TypePartsTree> parameters = [item];

			while (_back.Value.Is(LexicalTokenType.SEQUENCE, COMMA))
			{
				fullName = _scanner.Next(ref stream).Is(LexicalTokenType.SEQUENCE, OPEN_SQRBRC);
				if (!fullName)
					_back.Back();
				item = Parse(ref stream, fullName);
				if (item == null)
					return null;
				if (fullName)
					if (!_back.Value.Is(LexicalTokenType.SEQUENCE, CLOSE_SQRBRC))
						return null;
					else
						_scanner.Next(ref stream);
				parameters.Add(item);
			}

			if (!_back.Value.Is(LexicalTokenType.SEQUENCE, terminal))
				return null;
			
			return parameters;
		}

		// type
		// type[]
		// type*
		// type*[]
		// type?
		// type?[]
		// type?*
		// type?*[]
		public class TypePartsTree
		{
			private string Name { get; }
			private string? Assembly { get; }
			private int PointerCount { get; }
			private bool IsNullable { get; }
			private IReadOnlyList<int> ArrayRank { get; }
			private int GenericsCount { get; }
			private IReadOnlyList<TypePartsTree> Parameters { get; }
			private IReadOnlyDictionary<string, string> Options { get; }

			public TypePartsTree(string name, string? assembly = null, int pointerCount = 0, bool isNullable = false, IReadOnlyList<int>? arrayRank = null, int genericsCount = 0, IReadOnlyList<TypePartsTree>? parameters = null, IReadOnlyDictionary<string, string>? options = null)
			{
				Name = name;
				Assembly = assembly;
				PointerCount = pointerCount;
				IsNullable = isNullable;
				ArrayRank = arrayRank ?? Array.Empty<int>();
				GenericsCount = genericsCount;
				Parameters = parameters ?? Array.Empty<TypePartsTree>();
				Options = options ?? ReadOnly.Empty<string, string>();
			}

			private bool IsGeneric => Parameters.Count > 0 || GenericsCount > 0;
			private int GenericParametersCount => Parameters.Count > 0 ? Parameters.Count: GenericsCount;
			private bool HasAssemblyReference => !String.IsNullOrEmpty(Assembly);

			private StringBuilder BaseName(StringBuilder text, bool alter = false)
			{
				if (text is null)
					throw new ArgumentNullException(nameof(text));

				if (IsNullable && !alter)
					text.Append("System.Nullable`1[[");
				text.Append(Name);
				if (IsGeneric)
				{
					if (!alter)
						text.Append('`').Append(GenericParametersCount);
					if (Parameters.Count > 0)
					{
						text.Append(alter ? '<': '[');
						string prefix = "";
						foreach (var item in Parameters)
						{
							text.Append(prefix);
							if (!alter)
								text.Append('[');
							item.BaseName(text, alter);
							if (!alter)
								text.Append(']');
							prefix = ", ";
						}
						text.Append(alter ? '>' : ']');
					}
					else if (alter)
					{
						text.Append('<').Append(',', GenericsCount - 1).Append('>');
					}
				}
				if (IsNullable)
					text.Append(alter ? "?": "]]");
				text.Append('*', PointerCount);
				foreach (var item in ArrayRank)
				{
					text.Append('[').Append(',', Math.Max(0, item - 1)).Append(']');
				}
				if (!HasAssemblyReference)
					return text;

				if (alter)
					return text.Append('@').Append(Assembly);

				text.Append(", ").Append(Assembly);
				foreach (var item in Options)
				{
					text.Append(", ").Append(item.Key).Append('=').Append(item.Value);
				}
				return text;
			}

			public string BaseName(bool alter = false) => BaseName(new StringBuilder(), alter).ToString();

			public override string ToString()
			{
				return BaseName(true);
			}

			public Type? MakeType()
			{
				Type? type = Type.GetType(BaseName());
				if (type != null)
					return type;
				type = FindType();
				if (type == null || type == typeof(void))
					return type;
				if (IsNullable && !Factory.IsNullableType(type))
					type = typeof(Nullable<>).MakeGenericType(type);
				for (int i = PointerCount; i > 0; --i)
				{
					type = type.MakePointerType();
				}
				foreach (var n in ArrayRank)
				{
					type = type.MakeArrayType(n);
				}
				return type;
			}

			private Type? FindType()
			{
				var name = IsGeneric ? Name + "`" + GenericParametersCount.ToString(): Name;
				var type = GetSynonym(name) ?? (Assembly == null ?
					Factory.FindType(name, DomainAssemblies) ?? Factory.FindType(name, SystemAssemblies):
					DomainAssemblies.FirstOrDefault(o => String.Equals(o.GetName().Name, Assembly, StringComparison.OrdinalIgnoreCase))
					?.GetType(name, false, true));

				if (type == null)
					return null;

				if (Parameters.Count == 0)
					return type;
				Type[] parameters = new Type[Parameters.Count];
				for (int i = 0; i < parameters.Length; ++i)
				{
					var p = Parameters[i].MakeType();
					if (p == null || p == typeof(void))
						return null;
					parameters[i] = p;
				}

				try { return type.MakeGenericType(parameters); } catch { return null; }
			}
		}
	}

	#endregion

	private static Type? GetTypeInternal(string typeName) => Type.GetType(typeName, false, true) ?? TypeNameParser.Parse(typeName);

	public static Type? GetType(string? typeName, IEnumerable<Assembly>? assemblies)
	{
		if (typeName is not { Length: >0 })
			return null;

		var type = Type.GetType(typeName, false, true);
		return type != null || assemblies == null ? type: FindType(typeName, assemblies);
	}

	private static Type? FindType(string typeName, IEnumerable<Assembly> assemblies)
	{
		Debug.Assert(assemblies != null);
		return typeName.IndexOf('.') >= 0 ?
			assemblies.Select(o => o.GetType(typeName, false, true)).FirstOrDefault(o => o != null):
			assemblies.SelectMany(a => a.SelectTypes(o => String.Equals(o.Name, typeName, StringComparison.OrdinalIgnoreCase)))
			.FirstOrDefault();
	}

	public static bool IsPublicType(Type? type)
	{
		while (type is { IsNestedPublic: true })
		{
			type = type.DeclaringType;
		}
		return type is { IsPublic: true };
	}

	public static bool IsNullableType(Type type)
	{
		if (type == null)
			throw new ArgumentNullException(nameof(type));

		return type.IsClass || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
	}

	public static Type NullableTypeBase(Type type)
	{
		if (type == null)
			throw new ArgumentNullException(nameof(type));

		return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) ? type.GetGenericArguments()[0]: type;
	}

	public static object? DefaultValue(Type type)
	{
		if (type == null)
			throw new ArgumentNullException(nameof(type));

		if (!type.IsValueType || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)))
			return null;
		object? value = __defaults[type];
		if (value != null)
			return value;
		return Activator.CreateInstance(type);
	}
	private static readonly Hashtable __defaults = new Hashtable
	{
		{typeof(byte), default(byte)},
		{typeof(sbyte), default(sbyte)},
		{typeof(short), default(short)},
		{typeof(ushort), default(ushort)},
		{typeof(int), default(int)},
		{typeof(uint), default(uint)},
		{typeof(long), default(long)},
		{typeof(ulong), default(ulong)},
		{typeof(decimal), default(decimal)},
		{typeof(float), default(float)},
		{typeof(double), default(double)},
		{typeof(DateTime), default(DateTime)},
		{typeof(TimeSpan), default(TimeSpan)},
		{typeof(Guid), default(Guid)},
	};

	#region Helpers

	public static T Construct<T>()
	{
		return (T)Construct(typeof(T));
	}

	public static T Construct<T>(params object?[] arguments)
	{
		return (T)Construct(typeof(T), arguments);
	}

	public static T? TryConstruct<T>()
	{
		return TryConstruct(typeof(T)) is T result ? result: default;
	}

	public static T? TryConstruct<T>(params object?[] arguments)
	{
		return TryConstruct(typeof(T), arguments) is T result ? result: default;
	}

	public static object Construct(string typeName)
	{
		if (typeName is not { Length: >0 })
			throw new ArgumentNullException(nameof(typeName));
		Type? type = GetType(typeName);
		if (type == null)
			throw new ArgumentOutOfRangeException(nameof(typeName), typeName, null);

		return Construct(type);
	}

	public static object Construct(string typeName, params object?[] parameters)
	{
		if (typeName is not { Length: >0 })
			throw new ArgumentNullException(nameof(typeName));
		Type? type = GetType(typeName);
		if (type == null)
			throw new ArgumentOutOfRangeException(nameof(typeName), typeName, null);

		return Construct(type, parameters);
	}

	public static object? TryConstruct(string typeName)
	{
		if (typeName is not { Length: >0 })
			throw new ArgumentNullException(nameof(typeName));
		Type? type = GetType(typeName);
		if (type == null)
			return null;

		return TryConstruct(type);
	}

	public static object? TryConstruct(string typeName, params object?[] parameters)
	{
		if (typeName is not { Length: >0 })
			throw new ArgumentNullException(nameof(typeName));
		Type? type = GetType(typeName);
		if (type == null)
			return null;

		return TryConstruct(type, parameters);
	}
	#endregion


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static object Construct(Type type)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));
		return Activator.CreateInstance(type, true)!;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Func<object> GetConstructor(Type type)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));

		return TryGetConstructor(type) ?? throw new ArgumentException(SR.Factory_CannotFindConstructor(type, 0));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static object Construct(Type type, params object?[]? args)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));

		return TryConstruct(type, args) ?? throw new ArgumentException(SR.Factory_CannotFindConstructor(type, args?.Length ?? 0));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Func<object?[], object> GetConstructor(Type type, params Type?[]? args)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));

		return TryGetConstructor(type, args) ?? throw new ArgumentException(SR.Factory_CannotFindConstructor(type, args?.Length ?? 0));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static object? TryConstruct(Type type)
	{
		if (type == null)
			throw new ArgumentNullException(nameof(type));

		Func<object>? f = TryGetConstructor(type);
		return f?.Invoke();
	}

	public static object? TryConstruct(Type type, params object?[]? args)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));

		if (args == null || args.Length == 0)
			return TryConstruct(type);

		Type?[] argType = new Type[args.Length];
		for (int i = 0; i < args.Length; ++i)
		{
			argType[i] = args[i]?.GetType();
		}

		Func<object?[], object>? f = TryGetConstructor(type, argType);
		if (f == null)
			return null;

		try
		{
			return f(args);
		}
		catch (TypeInitializationException)
		{
			return null;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Func<object>? TryGetConstructor(Type type)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));

		return __constructors0.GetOrAdd(type, TryCreateConstructor);
	}
	private static readonly ConcurrentDictionary<Type, Func<object>?> __constructors0 = new ConcurrentDictionary<Type, Func<object>?>();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Func<object>? TryCreateConstructor(Type type)
	{
		if (type.IsValueType)
		{
			if (type == typeof(void))
				return null;
			return Expression.Lambda<Func<object>>(Expression.TypeAs(Expression.Default(type), typeof(object)))
#if NETFRAMEWORK && DEBUG
				.Compile(DebugInfo);
#else
				.Compile();
#endif
		}

		ConstructorInfo? ci = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
		if (ci == null)
			return null;
		return Expression.Lambda<Func<object>>(Expression.New(ci))
#if NETFRAMEWORK && DEBUG
			.Compile(DebugInfo);
#else
			.Compile();
#endif
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Func<object?, object>? TryGetConstructor(Type type, Type argType)
	{
		if (type == null)
			throw new ArgumentNullException(nameof(type));

		Func<object?[], object>? c = TryGetConstructor(type, new[] { argType });
		return c == null ? null: o => c(new[] {o});
	}

	//public static Func<object[], object> TryGetConstructor(Type type, int parametersCount)
	//{
	//	return __constructors2.GetOrAdd(Tuple.Create(type, parametersCount), o => TryCreateConstructor(o.Item1, o.Item2));
	//}
	//private static ConcurrentDictionary<Tuple<Type, int>, Func<object[], object>> __constructors2 = 
	//	new ConcurrentDictionary<Tuple<Type, int>, Func<object[], object>>();

	//private static Func<object[], object> TryCreateConstructor(Type type, int parametersCount)
	//{
	//	if (type == null)
	//		return null;

	//	ConstructorInfo constructor = null;
	//	foreach (var item in type.GetConstructors())
	//	{
	//		ParameterInfo[] pi = item.GetParameters();
	//		if (pi.Length == parametersCount)
	//		{
	//			if (constructor != null)
	//				return null;
	//			constructor = item;
	//		}
	//	}
	//	if (constructor == null)
	//		return null;

	//	return CompileParameterizedConstructor(constructor);
	//}

	public static Func<object?[], object>? TryGetConstructor(Type? type, Type?[]? argType)
	{
		if (type == null)
			return null;
		Type?[] args;
		if (argType == null || argType.Length == 0)
		{
			args = Type.EmptyTypes;
		}
		else
		{
			if (argType.Any(o => o == null))
				return null;
			args = new Type[argType.Length];
			Array.Copy(argType, 0, args, 0, argType.Length);
		}
		return __constructors3.GetOrAdd((type, args), o => TryCreateConstructor(o.Ret, o.Args));
	}
	private static readonly ConcurrentDictionary<(Type Ret, Type?[] Args), Func<object?[], object>?> __constructors3 = 
		new ConcurrentDictionary<(Type Ret, Type?[] Args), Func<object?[], object>?>(new ConstructorTypesComparer());

	private class ConstructorTypesComparer: IEqualityComparer<(Type Ret, Type?[] Args)>
	{
		public bool Equals((Type Ret, Type?[] Args) x, (Type Ret, Type?[] Args) y)
		{
			if (x.Ret != y.Ret)
				return false;

			Type?[] xa = x.Args;
			Type?[] ya = y.Args;

			if (Object.ReferenceEquals(xa, ya))
				return true;
			if (ya.Length != xa.Length)
				return false;

			for (int i = 0; i < xa.Length; ++i)
			{
				if (xa[i] != ya[i])
					return false;
			}
			return true;
		}

		public int GetHashCode((Type Ret, Type?[] Args) obj)
		{
			return HashCode.Join(obj.Ret.GetHashCode(), obj.Args.Length.GetHashCode());
		}
	}

	private static Func<object?[], object>? TryCreateConstructor(Type type, Type?[] argType)
	{
		ConstructorInfo? constructor = argType.All(o => o != null) ? type.GetConstructor(argType!): null;
		if (constructor != null)
			return CompileParameterizedConstructor(constructor, argType.Length);

		foreach (var cc in type.GetConstructors())
		{
			ParameterInfo[] pp = cc.GetParameters();
			if (pp.Length < argType.Length)
				continue;
			if (pp.Length > argType.Length && !pp.Skip(argType.Length).All(o => o.HasDefaultValue))
				continue;
			for (int i = 0; i < argType.Length; ++i)
			{
				var p = pp[i];
				if (argType[i] == null)
				{
					if (p is { HasDefaultValue: false, ParameterType.IsClass: false } && !(p.ParameterType.IsGenericType && p.ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>)))
						goto next;
				}
				else
				{
					if (!p.ParameterType.IsAssignableFrom(argType[i]))
						goto next;
				}
			}
			constructor = cc;
			break;

			next:
			;
		}
		if (constructor != null)
			return CompileParameterizedConstructor(constructor, argType.Length);

		if (argType.Length == 1 && argType[0] == type)
			return CompileCopyConstructor(type);

		return null;
	}

	private static Func<object?[], object> CompileCopyConstructor(Type parameterType)
	{
		ParameterExpression args = Expression.Parameter(typeof(object[]), "args");
		return Expression.Lambda<Func<object?[], object>>(
			ConvertParameter(Expression.ArrayAccess(args, Expression.Constant(0)), parameterType),
			args)
#if NETFRAMEWORK && DEBUG
			.Compile(DebugInfo);
#else
			.Compile();
#endif
		
	}

	private static Func<object?[], object> CompileParameterizedConstructor(ConstructorInfo constructor, int parametersCount)
	{
		ParameterInfo[] parameters = constructor.GetParameters();
		ParameterExpression args = Expression.Parameter(typeof(object[]), "args");

		var pp = parameters.Take(parametersCount).Select((p, i) => ConvertParameter(Expression.ArrayAccess(args, Expression.Constant(i)), p.ParameterType));
		if (parameters.Length < parametersCount)
			pp = pp.Union(parameters.Skip(parametersCount).Select(o => Expression.Constant(o.DefaultValue, o.ParameterType)));
		Expression call = Expression.New(constructor, pp);
		if (constructor.DeclaringType is { IsValueType: true })
			call = Expression.TypeAs(call, typeof(object));

		return Expression.Lambda<Func<object?[], object>>(call, args)
#if NETFRAMEWORK && DEBUG
			.Compile(DebugInfo);
#else
			.Compile();
#endif

	}

	private static Expression ConvertParameter(Expression parameter, Type type)
	{
		Type parameterType = NullableTypeBase(type);
		if (parameterType == typeof(object))
			return parameter;
		MethodInfo? method;
		Expression cvt;
		if (parameterType.IsEnum)
		{
			method = __conversionMethod[Type.GetTypeCode(Enum.GetUnderlyingType(parameterType))];
			cvt = Expression.Convert(
				Expression.Call(
					typeof(Enum).GetMethod("ToObject", [typeof(Type), method.ReturnType])!,
					Expression.Constant(parameterType, typeof(Type)),
					Expression.Convert(parameter, method.ReturnType, method)),
				parameterType);
		}
		else if (__conversionMethod.TryGetValue(Type.GetTypeCode(parameterType), out method))
		{
			cvt = Expression.Convert(parameter, parameterType, method);
		}
		else
		{
			method = parameterType.GetMethod("FromObject", [typeof(object)]);
			cvt = method == null
				? Expression.Convert(parameter, parameterType)
				: Expression.Convert(Expression.Call(method, parameter), parameterType);
		}

		if (parameterType != type)
			cvt = Expression.Convert(cvt, type);
		if (parameterType.IsValueType || parameterType == typeof(string))
			cvt = Expression.Condition(
				Expression.Equal(parameter, Expression.Constant(null, typeof(object))),
				Expression.Default(type),
				cvt);
		return cvt;
	}

	#region Conversion table

	private static readonly Dictionary<TypeCode, MethodInfo> __conversionMethod = new Dictionary<TypeCode, MethodInfo>
		{
			{ TypeCode.Boolean, ((Func<object, bool>)Convert.ToBoolean).Method },
			{ TypeCode.Byte, ((Func<object, Byte>)Convert.ToByte).Method },
			{ TypeCode.Char, ((Func<object, Char>)Convert.ToChar).Method },
			{ TypeCode.DateTime, ((Func<object, DateTime>)Convert.ToDateTime).Method },
			{ TypeCode.Decimal, ((Func<object, Decimal>)Convert.ToDecimal).Method },
			{ TypeCode.Double, ((Func<object, Double>)Convert.ToDouble).Method },
			{ TypeCode.Int16, ((Func<object, Int16>)Convert.ToInt16).Method },
			{ TypeCode.Int32, ((Func<object, Int32>)Convert.ToInt32).Method },
			{ TypeCode.Int64, ((Func<object, Int64>)Convert.ToInt64).Method },
			{ TypeCode.SByte, ((Func<object, SByte>)Convert.ToSByte).Method },
			{ TypeCode.Single, ((Func<object, Single>)Convert.ToSingle).Method },
			{ TypeCode.String, ((Func<object, String?>)Convert.ToString).Method },
			{ TypeCode.UInt16, ((Func<object, UInt16>)Convert.ToUInt16).Method },
			{ TypeCode.UInt32, ((Func<object, UInt32>)Convert.ToUInt32).Method },
			{ TypeCode.UInt64, ((Func<object, UInt64>)Convert.ToUInt64).Method },
			//{ TypeCode.DBNull, ((Func<object, DBNull>)Convert.ToDBNull).Method },
			//{ TypeCode.Empty, ((Func<object, Empty>)Convert.ToEmpty).Method },
			//{ TypeCode.Object, ((Func<object, Object>)Convert.ToObject).Method },
		};

	#endregion

	public static MethodInfo? GetGenericMethod(Type classType, string methodName, Type[] arguments)
	{
		if (classType is null)
			throw new ArgumentNullException(nameof(classType));
		if (methodName is null)
			throw new ArgumentNullException(nameof(methodName));
		if (arguments is null)
			throw new ArgumentNullException(nameof(arguments));

		foreach (var item in classType.GetMember(methodName, BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public))
		{
			var mi = item as MethodInfo;
			if (mi == null)
				continue;
			ParameterInfo[] pp = mi.GetParameters();
			if (pp.Length != arguments.Length)
				continue;
			bool found = true;
			for (int i = 0; i < pp.Length; ++i)
			{
				Type t = pp[i].ParameterType.IsGenericType ? pp[i].ParameterType.GetGenericTypeDefinition() : pp[i].ParameterType;
				if (t.UnderlyingSystemType != arguments[i].UnderlyingSystemType)
				{
					found = false;
					break;
				}
			}
			if (found)
				return mi;
		}
		return null;
	}

#if false

	///////////////////////////////////////////////////////////////////////////////////////////////
	
	public static IEnumerable<MethodBase> FindConstructors(Type type, string methodName, Type[] argTypes)
	{
		if (argTypes == null)
			argTypes = Type.EmptyTypes;
		if (methodName == null)
			return FindTypes(type)
				.Select(t => (MethodBase)t.GetConstructor(argTypes))
				.Where(m => m != null);
		else
			return FindTypes(type)
				.Select(t => (MethodBase)t.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public, null, argTypes, null) ?? (MethodBase)t.GetConstructor(argTypes))
				.Where(m => m != null);
	}

	public static object New(Type type, string methodName, Type[] argTypes)
	{
		if (argTypes == null)
			argTypes = Type.EmptyTypes;
		if (methodName == null)
		{
			return FindTypes(type)
				.Select(t => (MethodBase)t.GetConstructor(argTypes))
				.Where(m => m != null);

		}
		else
			return FindTypes(type)
				.Select(t => (MethodBase)t.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public, null, types, null) ?? (MethodBase)t.GetConstructor(types))
				.Where(m => m != null);
		return GetConstructor(type)();
	}

	public static T Create<T>()
	{
		return (T)Create(typeof(T));
	}

	public static object Create(Type type)
	{
		IList<Func<object>> cc = Constructors(type);
		return cc.Count == 1 ? cc[0](): null;
	}

	public static IList<Func<object>> Constructors(Type type)
	{
		return __constructors.GetOrAdd(type, t =>
		{
			List<Func<object>> r = null;
			foreach (var item in ConstructedTypes(t))
			{
				ConstructorInfo c = item.GetConstructor(Type.EmptyTypes);
				if (c != null)
				{
					DynamicMethod m = new DynamicMethod(string.Empty, item, null, typeof(Factory).Module);
					ILGenerator il = m.GetILGenerator();
					il.Emit(OpCodes.Newobj, c);
					il.Emit(OpCodes.Ret);
					if (r == null)
						r = new List<Func<object>>();
					r.Add((Func<object>)m.CreateDelegate(typeof(Func<object>)));
				}
			}
			return r == null ? NoResults<Func<object>>.Items: ReadOnly.Wrap(r);
		});
	}
	private static ConcurrentDictionary<Type, IList<Func<object>>> __constructors = new ConcurrentDictionary<Type, IList<Func<object>>>();

	public static Func<TObj, TArg> Create<TObj, TArg>(string staticMethod)
	{
		return __constructors.GetOrAdd(type, t =>
		{
			foreach (var item in Classes(t))
			{
				ConstructorInfo c = item.GetConstructor(Type.EmptyTypes);
				if (c != null)
				{
					DynamicMethod m = new DynamicMethod(string.Empty, item, null, MethodBase.GetCurrentMethod().DeclaringType.Module);
					ILGenerator il = m.GetILGenerator();
					il.Emit(OpCodes.Newobj, c);
					il.Emit(OpCodes.Ret);
					return (Func<object>)m.CreateDelegate(typeof(Func<object>));
				}
			}
			return () => null;
		});
	}

	public static T New<T>()
		where T: new()
	{
		return Constructor<T>.New();
	}

	private static class Constructor<T>
		where T: new()
	{
		private static Func<T> _emptyConstructor = () => default(T);
		private static Func<T> _defaultConstructor = GetDefaultConstructor();
		private static IList<Func<T>> _allConstructors = GetAllConstructors();

		public Func<T> Empty
		{
			get { return _emptyConstructor; }
		}

		public Func<T> Default
		{
			get { return _defaultConstructor; }
		}

		public bool HasDefault
		{
			get { return _defaultConstructor != _emptyConstructor; }
		}

		public IList<Func<T>> All
		{
			get { return _allConstructors; }
		}

		public IList<Func<T>> GetAllConstructors(string methodName)
		{
			List<Func<T>> r = null;
			foreach (Type type in ConstructedTypes(typeof(T)))
			{
				MethodInfo m = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public, null, Type.EmptyTypes, null)
				if (m != null && m.ReturnType == type)
				{
					if (r == null)
						r = new List<Func<T>>();
					r.Add(Expression.Lambda<Func<T>>(Expression.Call(m)).Compile());
				}
			}
			return r == null ? NoResults<Func<T>>.Items: ReadOnly.Wrap(r);


		}
		

		private static IList<Func<T>> GetAllConstructors()
		{
			List<Func<T>> r = null;
			foreach (Type type in ConstructedTypes(typeof(T)))
			{
				ConstructorInfo c = type.GetConstructor(Type.EmptyTypes);
				if (c != null)
				{
					if (r == null)
						r = new List<Func<T>>();
					r.Add(Expression.Lambda<Func<T>>(Expression.New(c)).Compile());
				}
			}
			return r == null ? NoResults<Func<T>>.Items: ReadOnly.Wrap(r);
		}

		private static Func<T> GetDefaultConstructor()
		{
			ConstructorInfo c = typeof(T).GetConstructor(Type.EmptyTypes);
			return c == null ? _emptyConstructor: Expression.Lambda<Func<T>>(Expression.New(c)).Compile();
		}
	}

	private static class Constructor<T, Targ>
		where T: new()
	{
		private static Func<T> _emptyConstructor = () => default(T);
		private static Func<T> _defaultConstructor = GetDefaultConstructor();
		private static IList<Func<T>> _allConstructors = GetAllConstructors();

		public static T New()
		{
			return _defaultConstructor();
		}

		public Func<T> Empty
		{
			get { return _emptyConstructor; }
		}

		public Func<T> Default
		{
			get { return _defaultConstructor; }
		}

		public IList<Func<T>> All
		{
			get { return _allConstructors; }
		}

		public Func<T> GetMethod()
		{
			return null;
		}

		private static IList<Func<T>> GetAllConstructors()
		{
			List<Func<T>> r = null;
			foreach (Type type in ConstructedTypes(typeof(T)))
			{
				ConstructorInfo c = type.GetConstructor(Type.EmptyTypes);
				if (c != null)
				{
					if (r == null)
						r = new List<Func<T>>();
					r.Add(Expression.Lambda<Func<T>>(Expression.New(c)).Compile());
				}
			}
			return r == null ? NoResults<Func<T>>.Items: ReadOnly.Wrap(r);
		}

		private static Func<T> GetDefaultConstructor()
		{
			ConstructorInfo c = typeof(T).GetConstructor(Type.EmptyTypes);
			return c == null ? _emptyConstructor: Expression.Lambda<Func<T>>(Expression.New(c)).Compile();
		}
	}

	public static T Create<T>(string methodName)
	{
		return default(T);
	}

	public static Type GetType(string typeName, string assemblyName)
	{
		if (typeName == null || typeName.Length == 0)
			throw new ArgumentNullException("typeName");

		Type type = TryGetType(typeName, assemblyName);
		if (type == null)
			throw new InvalidOperationException(SR.TLS_CannotCreateType(typeName, assemblyName));
		return type;
	}

	public static Type TryGetType(string typeName, string assemblyName)
	{
		if (typeName == null || typeName.Length == 0)
			return null;

		Type type = Type.GetType(typeName, false, true);
		if (type == null && assemblyName != null && assemblyName.Length > 0)
		{
			Assembly asm = TryLoadAssembly(assemblyName);
			if (asm != null)
				type = asm.GetType(typeName);
		}
		return type;
	}

	public static Type GetType(string typeName)
	{
		if (typeName == null || typeName.Length == 0)
			throw new ArgumentNullException("typeName");

		Type type = TryGetType(typeName);
		if (type == null)
			throw new InvalidOperationException(SR.TLS_CannotCreateType(typeName, null));
		return type;
	}

	public static Type TryGetType(string typeName)
	{
		if (typeName == null || typeName.Length == 0)
			return null;

		int i = typeName.IndexOf(',');
		if (i < 0)
			return TryGetType(typeName, null);
		else
			return TryGetType(typeName.Substring(0, i).TrimEnd(), typeName.Substring(i+1).TrimStart());
	}
#endif

	public static object? GetUnderlyingValue(object? value)
	{
		if (value == null)
			return null;
		if (value is IValue iv)
			value = iv.Value;
		if (value == null)
			return null;
		if (DBNull.Value.Equals(value))
			return null;
		if (!value.GetType().IsEnum)
			return value;

		return (Type.GetTypeCode(Enum.GetUnderlyingType(value.GetType()))) switch
		{
			TypeCode.Byte => (byte)value,
			TypeCode.Char => (char)value,
			TypeCode.Int16 => (short)value,
			TypeCode.Int32 => (int)value,
			TypeCode.Int64 => (long)value,
			TypeCode.SByte => (sbyte)value,
			TypeCode.UInt16 => (ushort)value,
			TypeCode.UInt32 => (uint)value,
			TypeCode.UInt64 => (ulong)value,
			_ => (int)value,
		};
	}


	public static object? Invoke(object? instance, MethodInfo method, params object?[] parameters)
	{
		if (method == null)
			throw new ArgumentNullException(nameof(method));
		if (instance == null && !method.IsStatic)
			throw new ArgumentNullException(nameof(instance));

		Func<object?, object?[], object?>? f = __compiledMethods.GetOrAdd(method, o => Compile((MethodInfo)o));
		return f?.Invoke(instance, parameters);
	}

	public static object? Invoke(MethodInfo method, params object?[] parameters)
	{
		if (method == null)
			throw new ArgumentNullException(nameof(method));
		if (!method.IsStatic)
			throw new ArgumentOutOfRangeException(nameof(method), method, null);

		Func<object?, object?[], object?>? f = __compiledMethods.GetOrAdd(method, o => Compile((MethodInfo)o));
		return f?.Invoke(null, parameters);
	}

	public static object? Invoke(ConstructorInfo constructor, params object?[] parameters)
	{
		if (constructor == null)
			throw new ArgumentNullException(nameof(constructor));

		Func<object?, object?[], object?>? f = __compiledMethods.GetOrAdd(constructor, o => Compile((ConstructorInfo)o));
		return f?.Invoke(null, parameters);
	}

	//public static Func<object, object[], object> TryCompile(MemberInfo member)
	//{
	//	if (member == null)
	//		throw new ArgumentNullException("member");

	//	MethodInfo method = member as MethodInfo;
	//	if (method != null)
	//		return __compiledMethods.GetOrAdd(method, o => Compile((MethodInfo)o));
	//	ConstructorInfo constructor = member as ConstructorInfo;
	//	if (constructor != null)
	//		return __compiledMethods.GetOrAdd(constructor, o => Compile((ConstructorInfo)o));
	//	throw EX.ArgumentWrongType("member", member.GetType());
	//}

	private static Func<object?, object?[], object?> Compile(MethodInfo method)
	{
		if (method == null)
			throw new ArgumentNullException(nameof(method));

		ParameterExpression arg1 = Expression.Parameter(typeof(object));
		ParameterExpression args = Expression.Parameter(typeof(object[]), "args");
		Expression[] pp = CompileParameters(method, args);
		Expression? instance = method.IsStatic || method.DeclaringType == null ? null: Expression.Convert(arg1, method.DeclaringType);
		Expression call = method.IsStatic ? Expression.Call(method, pp): Expression.Call(instance, method, pp);
		if (method.ReturnType != typeof(void) && method.ReturnType.IsValueType)
			call = Expression.TypeAs(call, typeof(object));

		if (method.ReturnType == typeof(void))
			call = Expression.Block(call, Expression.Constant(null));

		return Expression.Lambda<Func<object?, object?[], object?>>(call, arg1, args)
#if NETFRAMEWORK && DEBUG
			.Compile(DebugInfo);
#else
			.Compile();
#endif

	}

	private static Func<object?, object?[], object?> Compile(ConstructorInfo constructor)
	{
		if (constructor == null)
			throw new ArgumentNullException(nameof(constructor));

		ParameterExpression instance = Expression.Parameter(typeof(object));
		ParameterExpression args = Expression.Parameter(typeof(object[]), "args");
		Expression[] pp = CompileParameters(constructor, args);
		Expression call = Expression.New(constructor, pp);
		if (constructor.ReflectedType is { IsValueType: true })
			call = Expression.TypeAs(call, typeof(object));

		return Expression.Lambda<Func<object?, object?[], object?>>(call, instance, args)
#if NETFRAMEWORK && DEBUG
			.Compile(DebugInfo);
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
