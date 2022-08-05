// Lexxys Infrastructural library.
// file: Factory.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;


namespace Lexxys
{
	using Configuration;
	using Tokenizer;

	public static class Factory
	{
		public const string ConfigurationRoot = "lexxys.factory";
		public const string ConfigurationImport = ConfigurationRoot + ".import";
		public const string ConfigurationSkip = ConfigurationRoot + ".ignore";
		public const string ConfigurationSynonyms = ConfigurationRoot + ".synonyms.*";

		public static readonly object Void = new object();
		public static readonly object[] NoArgs = Array.Empty<object>();
#if NETFRAMEWORK && DEBUG
		public static readonly DebugInfoGenerator DebugInfo = DebugInfoGenerator.CreatePdbGenerator();
#endif

		private static readonly ConcurrentDictionary<Func<Type, bool>, IEnumerable<Type>> __foundTypesP = new ConcurrentDictionary<Func<Type, bool>, IEnumerable<Type>>();
		private static readonly ConcurrentDictionary<Type, IEnumerable<Type>> __foundTypesC = new ConcurrentDictionary<Type, IEnumerable<Type>>();
		private static readonly ConcurrentDictionary<Type, IEnumerable<Type>> __foundTypesA = new ConcurrentDictionary<Type, IEnumerable<Type>>();
		private static volatile ConcurrentBag<Assembly> __assemblies;
		private static volatile ConcurrentBag<Assembly> __systemAssemblies;
		private static string[] __systemAssemblyNames;
		private static readonly object SyncRoot = new object();

		private static readonly ConcurrentDictionary<MemberInfo, Func<object, object[], object>> __compiledMethods = new ConcurrentDictionary<MemberInfo, Func<object, object[], object>>();

		public static IEnumerable<Assembly> DomainAssemblies
		{
			get
			{
				CollectOrImportAssemblies();
				return __assemblies;
			}
		}

		public static IEnumerable<Assembly> SystemAssemblies
		{
			get
			{
				CollectAssemblies();
				return __systemAssemblies;
			}
		}

		private static void OnAssemblyLoad(Assembly asm)
		{
			AssemblyLoad?.Invoke(null, new AssemblyLoadEventArgs(asm));
		}

		public static event EventHandler<AssemblyLoadEventArgs> AssemblyLoad;

		#region Assemblies

		private static string[] SystemAssemblyNames()
		{
			var systemNamesConfig = StaticServices.TryCreate<IConfigSection>()?.GetCollection<string>(ConfigurationSkip);
			var systemNames = systemNamesConfig?.Value;
			if (systemNames == null || systemNames.Count == 0)
				return DefaultSystemAssemblyNames;

			var ss = new List<string>(systemNames
				.SelectMany(o => o?.Split(',', ';'))
				.Select(o => o.TrimToNull())
				.Where(o => o != null));
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
		private static readonly string[] DefaultSystemAssemblyNames = { "CppCodeProvider", "WebDev.", "SMDiagnostics", "mscor", "vshost", "System", "Microsoft", "Windows", "Presentation", "netstandard" };

		private static bool IsSystemAssembly(Assembly asm)
		{
#if !NETCOREAPP
			if (asm.GlobalAssemblyCache)
				return true;
#endif
			string name = asm.FullName;
			return name == null || name.IndexOf("Version=0.0.0.0", StringComparison.OrdinalIgnoreCase) >= 0 || Array.FindIndex(__systemAssemblyNames!, s => name.StartsWith(s, StringComparison.Ordinal)) >= 0;
		}

		private static void CollectAssemblies()
		{
			if (__assemblies == null)
			{
				lock (SyncRoot)
				{
					#pragma warning disable CA1508 // Avoid dead conditional code
					if (__assemblies == null)
					{
						CollectAssembliesInternal();
					}
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
					#pragma warning disable CA1508 // Avoid dead conditional code
					if (__assemblies == null)
					{
						CollectAssembliesInternal();
						ImportAssembliesInternal();
					}
				}
			}
			else if (!__assembliesImported)
			{
				lock (SyncRoot)
				{
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
		private static bool __assembliesImported;

		private static void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
		{
			Assembly asm = args.LoadedAssembly;
			CollectAssemblies();
			if (IsSystemAssembly(asm))
			{
				__systemAssemblies.Add(asm);
			}
			else
			{
				__assemblies.Add(asm);
				__foundTypesP.Clear();
				__foundTypesC.Clear();
				__foundTypesA.Clear();
				OnAssemblyLoad(asm);
			}
		}

		private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
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
			var importConfig = StaticServices.TryCreate<IConfigSection>()?.GetCollection<string>(ConfigurationImport);
			var assemblies = importConfig?.Value;
			if (assemblies == null)
				return;

			foreach (string assemblyName in assemblies)
			{
				TryLoadAssembly(assemblyName, false);
			}
		}

		private static void OnConfigChanged(object sender, ConfigurationEventArgs e)
		{
			ImportRestAssemblies();
			ResetSynonyms();
		}


		public static Assembly TryLoadAssembly(string assemblyName, bool throwOnError)
		{
			if (assemblyName == null || assemblyName.Length == 0)
				if (throwOnError)
					throw EX.ArgumentNull(nameof(assemblyName));
				else
					return null;

			string file = null;
			try
			{
				file = Lxx.HomeDirectory == null ? assemblyName: Path.Combine(Lxx.HomeDirectory, assemblyName);
				return File.Exists(file) ? Assembly.LoadFrom(file): Assembly.Load(assemblyName);
			}
			catch (Exception flaw)
			{
                SystemLog.WriteErrorMessage("Lexxys.Factory.TryLoadAssembly", flaw, new OrderedBag<string, object> { { "assemblyName", assemblyName }, { "file", file } });
				if (throwOnError)
					throw;
				return null;
			}
		}

		public static Assembly LoadAssembly(string assemblyName)
		{
			return TryLoadAssembly(assemblyName, true);
		}

		#endregion

		public static IEnumerable<Type> Types(Func<Type, bool> predicate)
		{
			return __foundTypesP.GetOrAdd(predicate, p => ReadOnly.WrapCopy(DomainAssemblies.SelectMany(asm => asm.SelectTypes(p))));
		}

		public static IEnumerable<Type> Types(Type type, bool cacheResults = false)
		{
			if (type == null)
				throw EX.ArgumentNull(nameof(type));

			return cacheResults ?
				type.IsInterface ?
					__foundTypesA.GetOrAdd(type, key =>
						ReadOnly.WrapCopy(DomainAssemblies.SelectMany(asm => asm.SelectTypes(t => !t.IsInterface && Array.IndexOf(t.GetInterfaces(), key) >= 0)))
						):
					__foundTypesA.GetOrAdd(type, key =>
						ReadOnly.WrapCopy(DomainAssemblies.SelectMany(asm => asm.SelectTypes(t => !t.IsInterface && key.IsAssignableFrom(t))))
						):

				type.IsInterface ?
					DomainAssemblies.SelectMany(asm => asm.SelectTypes(t => !t.IsInterface && Array.IndexOf(t.GetInterfaces(), type) >= 0)):
					DomainAssemblies.SelectMany(asm => asm.SelectTypes(t => !t.IsInterface && type.IsAssignableFrom(t)));
		}

		private static IEnumerable<Type> SelectTypes(this Assembly assembly, Func<Type, bool> predicate)
		{
			IEnumerable<Type> list;
			try
			{
				list = assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException e)
			{
				list = e.Types.Where(t => t != null);
			}
			return predicate == null ? list: list.Where(predicate);
		}

		public static IEnumerable<Type> Classes(Type type, bool cacheResults = false)
		{
			if (type == null)
				throw EX.ArgumentNull(nameof(type));

			return cacheResults ?
				__foundTypesC.GetOrAdd(type, key => ReadOnly.WrapCopy(Classes(key, DomainAssemblies))):
				Classes(type, DomainAssemblies);
		}

		public static IEnumerable<Type> Classes(Type type, params Assembly[] assemblies)
		{
			return Classes(type, (IEnumerable<Assembly>)assemblies);
		}

		public static IEnumerable<Type> Classes(Type type, IEnumerable<Assembly> assemblies)
		{
			if (type == null)
				throw EX.ArgumentNull(nameof(type));
			if (assemblies == null)
				throw EX.ArgumentNull(nameof(assemblies));

			return type.IsInterface ?
				assemblies.SelectMany(asm => asm.SelectTypes(t => !t.IsInterface && !t.IsAbstract && Array.IndexOf(t.GetInterfaces(), type) >= 0)):
				assemblies.SelectMany(asm => asm.SelectTypes(t => !t.IsInterface && !t.IsAbstract && type.IsAssignableFrom(t)));
		}

		private static bool ConstructorPredicate(MethodInfo method, Type returnType, Type[] parameterType)
		{
			if (!returnType.IsAssignableFrom(method.ReturnType))
				return false;
			ParameterInfo[] pp = method.GetParameters();
			if (parameterType == null)
				return pp.Length == 0;
			if (pp.Length != parameterType.Length)
				return false;
			for (int i = 0; i < pp.Length; ++i)
			{
				if (!pp[i].ParameterType.IsAssignableFrom(parameterType[i]))
					return false;
			}
			return true;
		}

		public static List<MethodInfo> Constructors(Type type, string methodName, params Type[] types)
		{
			if (type == null)
				throw EX.ArgumentNull(nameof(type));

			if (types == null)
				types = Type.EmptyTypes;

			return String.IsNullOrEmpty(methodName) ?
				Factory.Types(type)
					.Select(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault(m => ConstructorPredicate(m, type, types)))
					.Where(m => m != null)
					.ToList():
				Factory.Types(type)
					.Select(t => t.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null))
					.Where(m => m != null && type.IsAssignableFrom(m.ReturnType))
					.ToList();
		}

		public static Type GetType(string typeName)
		{
			return typeName == null || (typeName = typeName.Trim()).Length == 0 ? null: GetSynonym(typeName) ?? GetTypeInternal(typeName);
		}

		public static void ResetSynonyms()
		{
			__synonymsLoaded = false;
		}

		public static void SetSynonym(string name, Type type)
		{
			if (String.IsNullOrEmpty(name))
				return;
			lock (SyncRoot)
			{
				string key = name.Replace(" ", "");
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

		private static Type GetSynonym(string name)
		{
			if (!__synonymsLoaded)
			{
				lock (SyncRoot)
				{
					if (!__synonymsLoading)
					{
						__synonymsLoading = true;
						var synonymsConfig = StaticServices.TryCreate<IConfigSection>()?.GetCollection<KeyValuePair<string, string>>(ConfigurationSynonyms);
						var synonyms = synonymsConfig?.Value;
						if (synonyms != null)
						{
							foreach (var item in synonyms)
							{
								var key = (item.Key ?? "").Replace(" ", "");
								if (key.Length == 0)
									continue;
								var value = (item.Value ?? "").Replace(" ", "");
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
				{ "ternary",	typeof(Ternary) },
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
				{ "ternary?",	typeof(Ternary?) },
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

		public class TypeNameParser
		{
			/*

			fullname			:= shortname ('@' assembly | [ ',' assembly [ ',' option ]* ])

			shortname			:= dottedname [ generic_definition ] [ nullable_suffix ] pointer_suffix* array_suffix*
			dottedname			:= NAME ('.' NAME)*
			nullable_suffix		:= '?'
			pointer_suffix		:= '*'
			array_suffix		:= '[' (',')+ ']'

			generic_definition	:= generic_sufix
								|  generic_sufix? '[' param_name (',' param_name)* ']'
								|  generic_sufix? '<' param_name (',' param_name)* '>'
			generic_suffix		:= '^' NUMBER

			param_name			:= '[' fullname ']'
								|	shortname
			assembly			:= dottedname
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

			private static readonly LexicalTokenRule[] __rules = new LexicalTokenRule[]
			{
				new WhiteSpaceTokenRule(),
				new NameTokenRule().WithNameRecognition(
					nameStart: o => o == '$' || o == '_' || Char.IsLetter(o),
					namePart: o => o == '$' || o == '_' || o == '.' || o == '+' || Char.IsLetterOrDigit(o),
					nameEnd: o => o == '$' || o == '_' || Char.IsLetterOrDigit(o),
					beginning: "@$" + NameTokenRule.DefaultBeginning,
					extra: true
					),
				new NumericTokenRule(Tokenizer.NumericTokenStyles.Ordinal),
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
			};

			class TokenScanner2: TokenScanner
			{
				private readonly OneBackFilter _push;
				public TokenScanner2(CharStream stream, params LexicalTokenRule[] rules)
					: base(stream, rules)
				{
					_push = new OneBackFilter();
					SetFilter(_push);
				}

				public void Back()
				{
					_push.Back();
				}
			}

			public static Type Parse(string value)
			{
				var tree = Parse(new TokenScanner2(new CharStream(value), __rules), true);
				return tree?.MakeType();
			}

			public static TypeTree ParseType(string value)
			{
				return Parse(new TokenScanner2(new CharStream(value), __rules), true);
			}

			private static TypeTree Parse(TokenScanner2 scanner, bool fullName)
			{
				var t = scanner.Next();
				if (!t.Is(LexicalTokenType.IDENTIFIER))
					return null;

				string name = t.Text;
				int generics = 0;
				bool nullable = false;
				int pointer = 0;
				List<int> rank = null;
				List<TypeTree> parameters = null;

				t = scanner.Next();
				if (t.Is(LexicalTokenType.SEQUENCE, GENERIC_SUFFIX))
				{
					t = scanner.Next();
					if (!t.Is(LexicalTokenType.NUMERIC))
						return null;
					if (!Int32.TryParse(t.Text, out generics))
						return null;
					t = scanner.Next();
				}
				if (t.Is(LexicalTokenType.SEQUENCE, BEGIN_GENERICS, OPEN_SQRBRC))
				{
					int terminal = t.TokenType.Item + 1;
					if (scanner.Next().Is(LexicalTokenType.SEQUENCE, COMMA, terminal))
					{
						int count = 1;
						while (!scanner.Current.Is(LexicalTokenType.SEQUENCE, terminal))
						{
							if (!scanner.Next().Is(LexicalTokenType.SEQUENCE, COMMA, terminal))
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
						scanner.Back();
						parameters = ParseParameters(scanner, terminal);
						if (parameters == null)
							return null;
					}
					t = scanner.Next();
				}
				if (t.Is(LexicalTokenType.SEQUENCE, QUESTION))
				{
					if (rank != null)
						return null;
					nullable = true;
					t = scanner.Next();
				}
				while (t.Is(LexicalTokenType.SEQUENCE, ASTERISK))
				{
					if (rank != null)
						return null;
					++pointer;
					t = scanner.Next();
				}
				while (t.Is(LexicalTokenType.SEQUENCE, OPEN_SQRBRC))
				{
					int count = 0;
					do
					{
						++count;
						t = scanner.Next();
						if (!t.Is(LexicalTokenType.SEQUENCE, COMMA, CLOSE_SQRBRC))
							return null;
					} while (t.Is(LexicalTokenType.SEQUENCE, COMMA));
					(rank ??= new List<int>()).Add(count);
					t = scanner.Next();
				}

				string assembly = null;
				OrderedBag<string, string> options = null;

				if (fullName && t.Is(LexicalTokenType.SEQUENCE, COMMA))
				{
					t = scanner.Next();
					if (!t.Is(LexicalTokenType.IDENTIFIER))
						return null;
					assembly = t.Text;
					t = scanner.Next();
					while (t.Is(LexicalTokenType.SEQUENCE, COMMA))
					{
						do
						{
							t = scanner.Next();
						} while (t.Is(LexicalTokenType.SEQUENCE, COMMA));
						if (!t.Is(LexicalTokenType.IDENTIFIER))
							return null;
						string n = t.Text;
						if (!scanner.Next().Is(LexicalTokenType.SEQUENCE, EQUAL))
							return null;
						int i = scanner.Stream.IndexOf(c => !Char.IsWhiteSpace(c));
						int j = scanner.Stream.IndexOf(c => c != '.' && c != '_' && c != '-' && !Char.IsLetterOrDigit(c), i);
						if (j == i)
							return null;
						(options ??= new OrderedBag<string, string>(StringComparer.OrdinalIgnoreCase))[n] = scanner.Stream.Substring(i, j - i);
						scanner.Stream.Forward(j);
						t = scanner.Next();
					}
				}
				else if (t.Is(LexicalTokenType.SEQUENCE, AT))
				{
					t = scanner.Next();
					if (!t.Is(LexicalTokenType.IDENTIFIER))
						return null;
					assembly = t.Text;
					t = scanner.Next();
				}
				var tree = new TypeTree
				{
					Name = name,
					PointerCount = pointer,
					Assembly = assembly,
					IsNullable = nullable,
					Generics = generics
				};
				if (rank != null)
					tree.ArrayRank = rank;
				if (parameters != null)
					tree.Parameters = parameters;
				if (options != null)
					tree.Options = options;

				return tree;
			}

			private static List<TypeTree> ParseParameters(TokenScanner2 scanner, int terminal)
			{
				bool fullName = scanner.Next().Is(LexicalTokenType.SEQUENCE, OPEN_SQRBRC);
				if (!fullName)
					scanner.Back();
				TypeTree item = Parse(scanner, fullName);
				if (item == null)
					return null;
				if (fullName)
					if (!scanner.Current.Is(LexicalTokenType.SEQUENCE, CLOSE_SQRBRC))
						return null;
					else
						scanner.Next();

				List<TypeTree> parameters = new List<TypeTree> { item };

				while (scanner.Current.Is(LexicalTokenType.SEQUENCE, COMMA))
				{
					fullName = scanner.Next().Is(LexicalTokenType.SEQUENCE, OPEN_SQRBRC);
					if (!fullName)
						scanner.Back();
					item = Parse(scanner, fullName);
					if (item == null)
						return null;
					if (fullName)
						if (!scanner.Current.Is(LexicalTokenType.SEQUENCE, CLOSE_SQRBRC))
							return null;
						else
							scanner.Next();
					parameters.Add(item);
				}

				if (!scanner.Current.Is(LexicalTokenType.SEQUENCE, terminal))
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
			public class TypeTree
			{
				public string Name { get; set; }
				public string Assembly { get; set; }
				public int PointerCount { get; set; }
				public bool IsNullable { get; set; }
				public IReadOnlyList<int> ArrayRank { get; set; } = Array.Empty<int>();
				public int Generics { get; set; }
				public IReadOnlyList<TypeTree> Parameters { get; set; } = Array.Empty<TypeTree>();
				public IReadOnlyDictionary<string, string> Options { get; set; } = ReadOnly.Empty<string, string>();

				public bool IsArray => ArrayRank.Count > 0;
				public bool IsGeneric => Parameters.Count > 0 || Generics > 0;
				public int GenericParametersCount => Parameters.Count > 0 ? Parameters.Count: Generics;
				public bool HasAssemblyReference => !String.IsNullOrEmpty(Assembly);

				public StringBuilder BaseName(StringBuilder text, bool alter = false)
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
							text.Append('<').Append(',', Generics - 1).Append('>');
						}
					}
					if (IsNullable)
						text.Append(alter ? "?": "]]");
					text.Append('*', PointerCount);
					foreach (var item in ArrayRank)
					{
						text.Append('[').Append(',', item - 1).Append(']');
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

				public Type MakeType()
				{
					Type type = Type.GetType(BaseName(false));
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
					for (int i = 0; i < ArrayRank.Count; ++i)
					{
						type = type.MakeArrayType(ArrayRank[i]);
					}
					return type;
				}

				private Type FindType()
				{
					var name = IsGeneric ? Name + "`" + GenericParametersCount.ToString(CultureInfo.InvariantCulture): Name;
					var type = GetSynonym(name) ?? (Assembly == null ?
						Factory.FindType(name, DomainAssemblies) ?? Factory.FindType(name, SystemAssemblies):
						DomainAssemblies.FirstOrDefault(o => String.Equals(o.GetName().Name, Assembly, StringComparison.OrdinalIgnoreCase))
						?.GetType(name, false, true));

					if (type == null)
						return null;

					if (Parameters.Count == 0)
						return type;
					var parameters = new Type[Parameters.Count];
					for (int i = 0; i < parameters.Length; ++i)
					{
						parameters[i] = Parameters[i].MakeType();
						if (parameters[i] == null || parameters[i] == typeof(void))
							return null;
					}
					try
					{
						return type.MakeGenericType(parameters);
					}
					#pragma warning disable CA1031 // Ignore errors.
					catch
					{
						return null;
					}
				}
			}
		}

		#endregion

		private static Type GetTypeInternal(string typeName)
		{
			Contract.Requires(!String.IsNullOrEmpty(typeName));
			return Type.GetType(typeName, false, true) ?? TypeNameParser.Parse(typeName);
		}

		public static Type GetType(string typeName, IEnumerable<Assembly> assemblies)
		{
			if (typeName == null)
				return null;

			var type = Type.GetType(typeName, false, true);
			return type != null || assemblies == null ? type: FindType(typeName, assemblies);
		}

		private static Type FindType(string typeName, IEnumerable<Assembly> assemblies)
		{
			Contract.Requires(assemblies != null);
			return typeName.IndexOf('.') >= 0 ?
				assemblies.Select(o => o.GetType(typeName, false, true)).FirstOrDefault(o => o != null):
				assemblies.SelectMany(a => a.SelectTypes(o => String.Equals(o.Name, typeName, StringComparison.OrdinalIgnoreCase)))
				.FirstOrDefault();
		}

		public static bool IsPublicType(Type type)
		{
			while (type != null && type.IsNestedPublic)
			{
				type = type.DeclaringType;
			}
			return type != null && type.IsPublic;
		}

		public static bool IsNullableType(Type type)
		{
			return type == null || type.IsClass || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
		}

		public static Type NullableTypeBase(Type type)
		{
			if (type == null)
				throw EX.ArgumentNull(nameof(type));

			return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) ? type.GetGenericArguments()[0]: type;
		}

		public static object DefaultValue(Type type)
		{
			if (type == null || !type.IsValueType || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)))
				return null;
			object value = __defaults[type];
			if (value != null)
				return value;
			Func<object> constructor = TryGetConstructor(type, false);
			return constructor?.Invoke();
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

		public static T Construct<T>(params object[] arguments)
		{
			return (T)Construct(typeof(T), arguments);
		}

		public static T TryConstruct<T>(bool throwOnError)
		{
			return TryConstruct(typeof(T), throwOnError) is T result ? result: default;
		}

		public static T TryConstruct<T>(bool throwOnError, params object[] arguments)
		{
			return TryConstruct(typeof(T), throwOnError, arguments) is T result ? result: default;
		}

		public static object Construct(string typeName)
		{
			if (typeName == null || typeName.Length == 0)
				throw EX.ArgumentNull(nameof(typeName));
			Type type = GetType(typeName);
			if (type == null)
				throw EX.ArgumentOutOfRange(nameof(typeName), typeName);

			return Construct(type);
		}

		public static object Construct(string typeName, params object[] parameters)
		{
			if (typeName == null || typeName.Length == 0)
				throw EX.ArgumentNull(nameof(typeName));
			Type type = GetType(typeName);
			if (type == null)
				throw EX.ArgumentOutOfRange(nameof(typeName), typeName);

			return Construct(type, parameters);
		}

		public static object TryConstruct(string typeName, bool throwOnError)
		{
			return TryConstruct(GetType(typeName), throwOnError);
		}

		public static object TryConstruct(string typeName, bool throwOnError, params object[] parameters)
		{
			return TryConstruct(GetType(typeName), throwOnError, parameters);
		}
		#endregion

		public static object Construct(Type type)
		{
			return TryConstruct(type, true);
		}

		public static Func<object> GetConstructor(Type type)
		{
			return TryGetConstructor(type, true);
		}

		public static object Construct(Type type, params object[] args)
		{
			return TryConstruct(type, true, args);
		}

		public static Func<object[], object> GetConstructor(Type type, params Type[] args)
		{
			return TryGetConstructor(type, true, args);
		}

		public static object TryConstruct(Type type, bool throwOnError)
		{
			if (type == null)
				if (throwOnError)
					throw EX.ArgumentNull(nameof(type));
				else
					return null;

			Func<object> f = TryGetConstructor(type, throwOnError);
			return f?.Invoke();
		}

		public static object TryConstruct(Type type, bool throwOnError, params object[] args)
		{
			if (type == null)
				if (throwOnError)
					throw EX.ArgumentNull(nameof(type));
				else
					return null;
			if (args == null || args.Length == 0)
				return TryConstruct(type, throwOnError);

			var argType = new Type[args.Length];
			for (int i = 0; i < args.Length; ++i)
			{
				argType[i] = args[i]?.GetType();
			}

			Func<object[], object> f = TryGetConstructor(type, throwOnError, argType);
			if (f == null)
				return null;

			try
			{
				return f(args);
			}
			catch (TypeInitializationException)
			{
				if (!throwOnError)
					return null;
				throw;
			}
		}

		public static Func<object> TryGetConstructor(Type type, bool throwOnError)
		{
			return __constructors0.GetOrAdd(type, o => TryCreateConstructor(o, throwOnError));
		}
		private static readonly ConcurrentDictionary<Type, Func<object>> __constructors0 = new ConcurrentDictionary<Type, Func<object>>();

		private static Func<object> TryCreateConstructor(Type type, bool throwOnError)
		{
			if (type == null)
				if (throwOnError)
					throw EX.ArgumentNull(nameof(type));
				else
					return null;

			if (type.IsValueType)
			{
				if (type == typeof(void))
					if (throwOnError)
						throw EX.ArgumentNull(nameof(type));
					else
						return null;
				return Expression.Lambda<Func<object>>(Expression.TypeAs(Expression.Default(type), typeof(object)))
#if NETFRAMEWORK && DEBUG
					.Compile(DebugInfo);
#else
					.Compile();
#endif
			}

			ConstructorInfo ci = !type.IsInterface ? type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) :
				Classes(type).Select(o => o.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null)).FirstOrDefault(o => o != null);
			if (ci == null)
				if (throwOnError)
					throw EX.Argument(SR.Factory_CannotFindConstructor(type));
				else
					return null;
			return Expression.Lambda<Func<object>>(Expression.New(ci))
#if NETFRAMEWORK && DEBUG
				.Compile(DebugInfo);
#else
				.Compile();
#endif
		}

		public static Func<object, object> TryGetConstructor(Type type, bool throwOnError, Type argType)
		{
			if (type == null)
				if (throwOnError)
					throw EX.ArgumentNull(nameof(type));
				else
					return null;
			Func<object[], object> c = TryGetConstructor(type, throwOnError, new[] { argType });
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

		public static Func<object[], object> TryGetConstructor(Type type, bool throwOnError, Type[] argType)
		{
			if (type == null)
				if (throwOnError)
					throw EX.ArgumentNull(nameof(type));
				else
					return null;
			return __constructors3.GetOrAdd((type, argType), o => TryCreateConstructor(o.Ret, throwOnError, o.Args));
		}
		private static readonly ConcurrentDictionary<(Type Ret, Type[] Args), Func<object[], object>> __constructors3 = 
			new ConcurrentDictionary<(Type Ret, Type[] Args), Func<object[], object>>(new ConstructorTypesComparer());

		private class ConstructorTypesComparer: IEqualityComparer<(Type Ret, Type[] Args)>
		{
			public bool Equals((Type Ret, Type[] Args) x, (Type Ret, Type[] Args) y)
			{
				if (x.Ret != y.Ret)
					return false;

				Type[] xa = x.Args;
				Type[] ya = y.Args;

				if (Object.ReferenceEquals(xa, ya))
					return true;
				if (xa == null || xa.Length == 0)
					return ya == null || ya.Length == 0;
				if (ya == null || ya.Length != xa.Length)
					return false;

				for (int i = 0; i < xa.Length; ++i)
				{
					if (xa[i] != ya[i])
						return false;
				}
				return true;
			}

			public int GetHashCode((Type Ret, Type[] Args) obj)
			{
				return HashCode.Join(obj.Ret.GetHashCode(), obj.Args);
			}
		}

		private static Func<object[], object> TryCreateConstructor(Type type, bool throwOnError, Type[] argType)
		{
			if (type == null)
				if (throwOnError)
					throw EX.ArgumentNull(nameof(type));
				else
					return null;
			argType ??= Type.EmptyTypes;

			ConstructorInfo constructor = Array.IndexOf(argType, null) < 0 ? type.GetConstructor(argType): null;
			if (constructor != null)
				return CompileParameterizedConstructor(constructor);

			foreach (var cc in type.GetConstructors())
			{
				ParameterInfo[] pi = cc.GetParameters();
				if (pi.Length == argType.Length)
				{
					for (int i = 0; i < pi.Length; ++i)
					{
						if (argType[i] == null)
						{
							if (!pi[i].ParameterType.IsClass && !(pi[i].ParameterType.IsGenericType && pi[i].ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>)))
								goto next;
						}
						else
						{
							if (!pi[i].ParameterType.IsAssignableFrom(argType[i]))
								goto next;
						}
					}
					constructor = cc;
					break;
				}
				next:
				;
			}
			if (constructor != null)
				return CompileParameterizedConstructor(constructor);

			if (argType.Length == 1 && argType[0] == type)
				return CompileCopyConstructor(type);

			if (throwOnError)
				throw EX.Argument(SR.Factory_CannotFindConstructor(type, argType.Length));
			return null;
		}

		private static Func<object[], object> CompileCopyConstructor(Type parameterType)
		{
			ParameterExpression args = Expression.Parameter(typeof(object[]), "args");
			return Expression.Lambda<Func<object[], object>>(
				ConvertParameter(Expression.ArrayAccess(args, Expression.Constant(0)), parameterType),
				args)
#if NETFRAMEWORK && DEBUG
				.Compile(DebugInfo);
#else
				.Compile();
#endif
			
		}

		private static Func<object[], object> CompileParameterizedConstructor(ConstructorInfo constructor)
		{
			ParameterInfo[] parameters = constructor.GetParameters();
			ParameterExpression args = Expression.Parameter(typeof(object[]), "args");

			Expression call = Expression.New(constructor, parameters.Select((p, i) => ConvertParameter(Expression.ArrayAccess(args, Expression.Constant(i)), p.ParameterType)));
			if (constructor.DeclaringType is { IsValueType: true })
				call = Expression.TypeAs(call, typeof(object));

			return Expression.Lambda<Func<object[], object>>(call, args)
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
			MethodInfo method;
			Expression cvt;
			if (parameterType.IsEnum)
			{
				method = __conversionMethod[Type.GetTypeCode(Enum.GetUnderlyingType(parameterType))];
				cvt = Expression.Convert(
					Expression.Call(
						typeof(Enum).GetMethod("ToObject", new[] { typeof(Type), method.ReturnType })!,
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
				method = parameterType.GetMethod("FromObject", new[] { typeof(object) });
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
				{ TypeCode.String, ((Func<object, String>)Convert.ToString).Method },
				{ TypeCode.UInt16, ((Func<object, UInt16>)Convert.ToUInt16).Method },
				{ TypeCode.UInt32, ((Func<object, UInt32>)Convert.ToUInt32).Method },
				{ TypeCode.UInt64, ((Func<object, UInt64>)Convert.ToUInt64).Method },
				//{ TypeCode.DBNull, ((Func<object, DBNull>)Convert.ToDBNull).Method },
				//{ TypeCode.Empty, ((Func<object, Empty>)Convert.ToEmpty).Method },
				//{ TypeCode.Object, ((Func<object, Object>)Convert.ToObject).Method },
			};

		#endregion

		public static MethodInfo GetGenericMethod(Type classType, string methodName, Type[] arguments)
		{
			if (classType is null)
				throw new ArgumentNullException(nameof(classType));
			if (arguments is null)
				throw new ArgumentNullException(nameof(arguments));

			foreach (var item in classType.GetMember(methodName, BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public))
			{
				var mi = item as MethodInfo;
				if (mi != null)
				{
					ParameterInfo[] pp = mi.GetParameters();
					if (pp.Length == arguments.Length)
					{
						for (int i = 0; i < pp.Length; ++i)
						{
							Type t = pp[i].ParameterType.IsGenericType ? pp[i].ParameterType.GetGenericTypeDefinition(): pp[i].ParameterType;
							if (t.UnderlyingSystemType != arguments[i].UnderlyingSystemType)
								goto NextMember;
						}
						return mi;
					}
				}
				NextMember: ;
			}
			return null;
		}

#if false

		//public static IEnumerable<MethodInfo> StaticMethods(Type type, string methodName, params Type[] types)
		//{
		//    if (methodName == null || methodName.Length == 0)
		//        throw EX.ArgumentNull("methodName");
		//    if (types == null)
		//        throw EX.ArgumentNull("types");

		//    return Factory.Types(type)
		//        .Select(t => t.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null))
		//        .Where(m => m != null);
		//}

		//public static IEnumerable<T> Create<T>(IEnumerable<MethodInfo> methods, object parameter)
		//    where T: class
		//{
		//    object[] parameters = new object[] { parameter };
		//    return methods
		//        .Select(m => m.Invoke(null, parameters) as T)
		//        .Where(o => o != null);
		//}

		//public static IEnumerable<T> Create<T>(IEnumerable<MethodInfo> methods, object parameter1, object parameter2)
		//    where T: class
		//{
		//    object[] parameters = new object[] { parameter1, parameter2 };
		//    return methods
		//        .Select(m => m.Invoke(null, parameters) as T)
		//        .Where(o => o != null);
		//}

		//public static IEnumerable<Func<T, T1>> Compile<T, T1>(IEnumerable<MethodInfo> methods)
		//    where T: class
		//{
		//    Expression[] parameters = new Expression[] { Expression.Parameter(typeof(T1)) };
		//    return methods
		//        .Select(m => Expression.Lambda<Func<T, T1>>(Expression.Call(m, parameters)).Compile());
		//}

		//public static IEnumerable<Func<T, T1, T2>> Compile<T, T1, T2>(IEnumerable<MethodInfo> methods)
		//    where T: class
		//{
		//    Expression[] parameters = new Expression[] { Expression.Parameter(typeof(T1)), Expression.Parameter(typeof(T2)) };
		//    return methods
		//        .Select(m => Expression.Lambda<Func<T, T1, T2>>(Expression.Call(m, parameters)).Compile());
		//}


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
				throw EX.ArgumentNull("typeName");

			Type type = TryGetType(typeName, assemblyName);
			if (type == null)
				throw EX.InvalidOperation(SR.TLS_CannotCreateType(typeName, assemblyName));
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
				throw EX.ArgumentNull("typeName");

			Type type = TryGetType(typeName);
			if (type == null)
				throw EX.InvalidOperation(SR.TLS_CannotCreateType(typeName, null));
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

		public static object GetUnderlyingValue(object value)
		{
			if (value == null)
				return null;
			if (value is IValue iv)
				value = iv.Value;
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


		public static object Invoke(object instance, MethodInfo method, params object[] parameters)
		{
			if (method == null)
				throw EX.ArgumentNull(nameof(method));
			if (instance == null && !method.IsStatic)
				throw EX.ArgumentNull(nameof(instance));

			Func<object, object[], object> f = __compiledMethods.GetOrAdd(method, o => Compile((MethodInfo)o));
			return f?.Invoke(instance, parameters);
		}

		public static object Invoke(MethodInfo method, params object[] parameters)
		{
			if (method == null)
				throw EX.ArgumentNull(nameof(method));
			if (!method.IsStatic)
				throw EX.ArgumentOutOfRange(nameof(method), method);

			Func<object, object[], object> f = __compiledMethods.GetOrAdd(method, o => Compile((MethodInfo)o));
			return f?.Invoke(null, parameters);
		}

		public static object Invoke(ConstructorInfo constructor, params object[] parameters)
		{
			if (constructor == null)
				throw EX.ArgumentNull(nameof(constructor));

			Func<object, object[], object> f = __compiledMethods.GetOrAdd(constructor, o => Compile((ConstructorInfo)o));
			return f?.Invoke(null, parameters);
		}

		//public static Func<object, object[], object> TryCompile(MemberInfo member)
		//{
		//	if (member == null)
		//		throw EX.ArgumentNull("member");

		//	MethodInfo method = member as MethodInfo;
		//	if (method != null)
		//		return __compiledMethods.GetOrAdd(method, o => Compile((MethodInfo)o));
		//	ConstructorInfo constructor = member as ConstructorInfo;
		//	if (constructor != null)
		//		return __compiledMethods.GetOrAdd(constructor, o => Compile((ConstructorInfo)o));
		//	throw EX.ArgumentWrongType("member", member.GetType());
		//}

		private static Func<object, object[], object> Compile(MethodInfo method)
		{
			if (method == null)
				return null;

			ParameterExpression arg1 = Expression.Parameter(typeof(object));
			ParameterExpression args = Expression.Parameter(typeof(object[]), "args");
			Expression[] pp = CompileParameters(method, args);
			Expression instance = method.IsStatic || method.DeclaringType == null ? null: Expression.Convert(arg1, method.DeclaringType);
			Expression call = method.IsStatic ? Expression.Call(method, pp): Expression.Call(instance, method, pp);
			if (method.ReturnType != typeof(void) && method.ReturnType.IsValueType)
				call = Expression.TypeAs(call, typeof(object));

			if (method.ReturnType == typeof(void))
				call = Expression.Block(call, Expression.Constant(null));

			return Expression.Lambda<Func<object, object[], object>>(call, arg1, args)
#if NETFRAMEWORK && DEBUG
				.Compile(DebugInfo);
#else
				.Compile();
#endif

		}

		private static Func<object, object[], object> Compile(ConstructorInfo constructor)
		{
			if (constructor == null)
				return null;

			ParameterExpression instance = Expression.Parameter(typeof(object));
			ParameterExpression args = Expression.Parameter(typeof(object[]), "args");
			Expression[] pp = CompileParameters(constructor, args);
			Expression call = Expression.New(constructor, pp);
			if (constructor.ReflectedType is { IsValueType: true })
				call = Expression.TypeAs(call, typeof(object));

			return Expression.Lambda<Func<object, object[], object>>(call, instance, args)
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

		public static object TryInherit(object obj, Type requiredType)
		{
			if (obj == null)
				throw EX.ArgumentNull(nameof(obj));
			if (requiredType == null)
				throw EX.ArgumentNull(nameof(requiredType));


			Type objType = obj.GetType();
			if (requiredType.IsAssignableFrom(objType))
				return obj;

			return __stubs.GetOrAdd(Tuple.Create(objType, requiredType), _ => null);

		}
		private static readonly ConcurrentDictionary<Tuple<Type, Type>, Func<object, object>> __stubs = new ConcurrentDictionary<Tuple<Type, Type>, Func<object, object>>();
	}
}
