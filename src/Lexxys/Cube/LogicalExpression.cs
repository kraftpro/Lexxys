// Lexxys Infrastructural library.
// file: LogicalExpression.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Lexxys.Cube
{
	using Tokenizer;

	/// <summary>Represents logical expression.</summary>
	public class LogicalExpression
	{
		Polish? _polish;
		readonly Dictionary<string, int> _dictionary;

		/// <summary>Create new <see cref="LogicalExpression"/></summary>
		public LogicalExpression()
		{
			_dictionary = new Dictionary<string, int>();
		}

		/// <summary>Create new <see cref="LogicalExpression"/></summary>
		/// <param name="expression">Text of logical expression</param>
		public LogicalExpression(string expression)
		{
			_dictionary = new Dictionary<string, int>();
			Parse(expression);
		}

		public LogicalExpression(string expression, Dictionary<string, int> dictionary)
		{
			_dictionary = dictionary;
			Parse(expression);
		}

		/// <summary>Create new <see cref="LogicalExpression"/> based on parsed text of <paramref name="tokenizer"/></summary>
		/// <param name="tokenizer">Text of expression</param>
		public LogicalExpression(TokenScanner tokenizer)
		{
			if (tokenizer is null)
				throw new ArgumentNullException(nameof(tokenizer));
			_dictionary = new Dictionary<string, int>();
			Parse(tokenizer);
		}

		/// <summary>Create new <see cref="LogicalExpression"/> based on parsed text of <paramref name="tokenizer"/></summary>
		/// <param name="tokenizer">Text of expression</param>
		/// <param name="dictionary">Known parameters</param>
		public LogicalExpression(TokenScanner tokenizer, Dictionary<string, int> dictionary)
		{
			if (tokenizer is null)
				throw new ArgumentNullException(nameof(tokenizer));
			_dictionary = dictionary;
			Parse(tokenizer);
		}

		/// <summary>Map of known parameters</summary>
		public Dictionary<string, int> Dictionary
		{
			get { return _dictionary; }
		}

		/// <summary>Parse text to <see cref="LogicalExpression"/>.</summary>
		/// <param name="expression">Text of expression to be parsed.</param>
		private void Parse(string expression)
		{
			var scanner = new TokenScanner(new Lexxys.Tokenizer.CharStream(expression),
				new WhiteSpaceTokenRule(),
				new CommentsTokenRule(LexicalTokenType.IGNORE, ("/*", "*/")),
				new SequenceTokenRule(LexicalTokenType.SEQUENCE, "|", "^", "&", "~", "(", ")")
					.Add(NOT, "!"),
				new IdentifierTokenRule(LexicalTokenType.IDENTIFIER, LexicalTokenType.SEQUENCE, true, "OR", "XOR", "AND", "NOT")
				);
            Parse(scanner);
		}

		private const int OR  = 1;
		private const int XOR = 2;
		private const int AND = 3;
		private const int NOT = 4;
		private const int OPENBR = 5;
		private const int CLOSEBR = 6;

		/// <summary>Parse stream of tokens into <see cref="LogicalExpression"/>.</summary>
		/// <param name="tokenizer">Stream of tokens to be parsed.</param>
		/// <remarks>The complexity 27 is fine for such type of functions</remarks>
		private void Parse(TokenScanner tokenizer)
		{
			_polish = new Polish();
			LexicalToken t;
			while (tokenizer.MoveNext())
			{
				t = tokenizer.Current;
				PolishToken? lt;
				if (t.TokenType.Is(LexicalTokenType.SEQUENCE))
				{
					switch (t.Item)
					{
						case OPENBR:
							lt = new OpenBraceToken();
							break;
						case CLOSEBR:
							lt = new ClosedBraceToken();
							break;
						case AND:
							lt = new LogicalAnd();
							break;
						case OR:
							lt = new LogicalOr();
							break;
						case XOR:
							lt = new LogicalXor();
							break;
						case NOT:
							lt = new LogicalNot();
							break;
						default:
							throw new FormatException(SR.EXP_UnknownSymbol(t.Text));
					}
				}
				else if (t.TokenType.Is(LexicalTokenType.IDENTIFIER))
				{
					if (t.Text == "TRUE" || t.Text == "GRANT" || t.Text == "1")
					{
						lt = LogicalValue.True;
					}
					else if (t.Text == "FALSE" || t.Text == "DENY" || t.Text == "0")
					{
						lt = LogicalValue.False;
					}
					else
					{
						if (!_dictionary.ContainsKey(t.Text))
							_dictionary.Add(t.Text, _dictionary.Count);
						lt = new LogicalVariable(_dictionary[t.Text]);
					}
				}
				else
				{
					throw new FormatException(SR.EXP_UnknownSymbol(t.Text));
				}
				_polish.Add(lt);
			}
		}

		/// <summary>Evaluate the <see cref="LogicalExpression"/></summary>
		/// <param name="mapper">Mapper to map parameters by index</param>
		/// <returns>Result of expression.</returns>
		public bool Evaluate(Predicate<int> mapper)
		{
			if (_polish == null)
				throw new InvalidOperationException(SR.ParserFirst());
			PolishToken? token = _polish.Evaluate(mapper);
			if (token == null)
				return true;
			if (token is LogicalValue result)
				return result.Evaluate(mapper);
			throw ParameterTypeException("token", token);
		}

		/// <summary>Convert logical <paramref name="expression"/> to minimal DNF form.</summary>
		/// <param name="expression">Logical expression to be converted to minimal DNF form</param>
		/// <returns>Expression in minimal DNF form.</returns>
		public static string ConvertToMinDnf(string expression)
		{
			return new LogicalExpression(expression).ConvertToMinDnf();
		}

		/// <summary>Convert logical expression to minimal DNF form.</summary>
		/// <returns>Expression in DNF.</returns>
		public string ConvertToMinDnf()
		{
			BitArray result = EvaluateForAllVariants();
			if (result.Length < 2)
				return result.Length == 0 ? "FALSE":
					result[0] ? "TRUE": "FALSE";
			string[] names = new string[_dictionary.Count];
			foreach (string name in _dictionary.Keys)
			{
				names[_dictionary[name]] = name;
			}
			var cube = new BooleanCube(result, names.Length);
			return cube.BuildMinimalDnf(names);
		}

		public static string ConvertBitsToMinDnf(BitArray mustResult, string[] names)
		{
			if (mustResult == null)
				return "FALSE";
			if (mustResult.Length <= 2)
				return mustResult.Length == 0 ? "FALSE":
					mustResult[0] ? "TRUE": "FALSE";
			if (names == null || names.Length == 0)
				return "FALSE";

			var dnf = new StringBuilder();
			var result = new BitArray(mustResult);
			int width = names.Length;
			int height = result.Count;

			uint[] skipped = new uint[height];
			uint[,] skipped2 = new uint[height, width];
			var useful = new BitArray(height);
			var ignore = new BitArray(height);
			bool optimized;
			char delim = '|';

			do
			{
				optimized = false;
				useful.SetAll(false);
				ignore.SetAll(false);
				for (int i = 0; i < height; i++)
				{
					if (result[i])	// Active desunctant
					{
						for (uint m = 1u << (width-1); m != 0; m >>= 1)
						{
							if ((i & m) == 0 && result[i ^ (int)m])
							{
								int i2 = i ^ (int)m;
								optimized = true;

								for (int j = 0; j < width; ++j)
								{
									if ((skipped2[i, j] & m) == 0)
									{
										skipped2[i, j] |= m;
										skipped[i] |= m;
										goto foundSI;
									}
								}
								throw new InvalidOperationException();
							foundSI:

								for (int j = 0; j < width; ++j)
								{
									if ((skipped2[i2, j] & m) == 0)
									{
										skipped2[i2, j] |= m;
										skipped[i2] |= m;
										goto foundSI2;
									}
								}
								throw new InvalidOperationException();
							foundSI2:

								useful[i] = true;
								ignore[i] = false;
								useful[i2] = true;
								ignore[i2] = true;
							}
						}

						if (ignore[i])
							result[i] = false;
						else if (!useful[i])
						{
							uint mask = 0;
							for (int j = 0; j < width; ++j)
							{
								mask |= skipped2[i, j];
							}
							for (int j = 0; j < width; j++)
							{
								if ((mask & (1u << j)) == 0)	// bit j is active
								{
									dnf.Append(delim);
									if ((i & (1u << j)) == 0)
										dnf.Append('~');
									dnf.Append(names[j]);
									delim = '&';
								}
							}
							delim = '|';
							result[i] = false;
						}
					}
				}
				if (optimized)
				{
					for (int i = 0; i < height; i++)
					{
						skipped[i] = skipped2[i, 0];
						for (int j = 1; j < width && skipped2[i, j] != 0; j++)
						{
							int i2 = (int)(i ^ skipped2[i, j]);
							if (ignore[i2])
							{
								result[i2] = true;
								for (int j2 = 1; j2 < width; j2++)
									skipped2[i2, j2] = 0;
								skipped2[i2, 0] = skipped2[i, j];
							}
							else
							{
							}
						}
					}
				}
			} while (optimized);

			return dnf.Length > 1 ? dnf.ToString(1, dnf.Length-1): "FALSE";
		}

		private BitArray EvaluateForAllVariants()
		{
			int width = Dictionary.Count;
			if (width < 0 || width > 16)
				throw new ArgumentOutOfRangeException(nameof(Dictionary.Count), width, null);
			int n = (1 << width);
			var result = new BitArray(n);
			for (int bits = 0; bits < n; ++bits)
			{
				var b = bits;
				result[bits] = Evaluate(i => (b & (1 << i)) != 0);
			}
			return result;
		}


		/// <summary>Evaluate expression for all posible combination of argument value.</summary>
		public BitArray EvaluateForAllVariants(string[] args)
		{
			if (args is null)
				throw new ArgumentNullException(nameof(args));
			int width = Math.Max(_dictionary.Count, args.Length);
			if (width < 0 || width > 16)
				throw new ArgumentOutOfRangeException(nameof(args), width, null);
			int height = (1 << width);
			var result = new BitArray(height);
			int[] map = new int[_dictionary.Count];
			for (int i = 0; i < map.Length; ++i)
			{
				map[i] = -1;
			}
			for (int i = 0; i < args.Length; ++i)
			{
				if (_dictionary.TryGetValue(args[i], out int j) && j < map.Length)
					map[j] = i;
			}
			for (int bits = 0; bits < height; ++bits)
			{
				var b = bits;
				result[bits] = Evaluate(i => map[i] >= 0 && ((1 << map[i]) & b) != 0);
			}
			return result;
		}


		/// <summary>Convert logical <paramref name="expression"/> to DNF form.</summary>
		/// <param name="expression">Logical expression to be converted to DNF form</param>
		/// <returns>Expression in DNF form.</returns>
		public static string ConvertToDnf(string expression)
		{
			if (expression is null)
				throw new ArgumentNullException(nameof(expression));
			if (__singleNameRex.IsMatch(expression))
			{
				expression = expression.Trim().ToUpperInvariant();
				if (expression == "GRANT")
					return "TRUE";
				if (expression == "DENY")
					return "FALSE";
				return expression;
			}
			return new LogicalExpression(expression).ConvertToDnf();
		}
		private static readonly Regex __singleNameRex = new Regex(@"^\s*([a-z][0-9a-z_]*)?\s*$", RegexOptions.IgnoreCase);

		/// <summary>Convert logical expression to DNF form.</summary>
		/// <returns>Expression in DNF form.</returns>
		public string ConvertToDnf()
		{
			return ConvertToMinDnf();
		}

		public static string ConvertBitsToDnf(BitArray result, string[] names)
		{
			if (result == null)
				throw new ArgumentNullException(nameof(result));
			if (names is null)
				throw new ArgumentNullException(nameof(names));

			var dnf = new StringBuilder();
			if (result.Length < 2)
				return result.Length == 0 ? "FALSE":
					result[0] ? "TRUE": "FALSE";

			int width = names.Length;
			int height = result.Count;

			int[] mask = new int[width];
			int m = 1;
			for (int i = 0; i < width; ++i)
			{
				mask[i] = m;
				m <<= 1;
			}
			char delim = '|';

			for (int i = 0; i < height; ++i)
			{
				if (result[i])
				{
					for (int j = 0; j < width; ++j)
					{
						dnf.Append(delim);
						if ((i & mask[j]) == 0)
							dnf.Append('~');
						dnf.Append(names[j]);
						delim = '&';
					}
					delim = '|';
				}
			}

			return dnf.Length > 1 ? dnf.ToString(1, dnf.Length-1): "FALSE";
		}

		private static string CombineAndByAnd(string exp1, string exp2)
		{
			Debug.Assert(exp1.IndexOf('|') < 0);
			Debug.Assert(exp2.IndexOf('|') < 0);
			string[] ss = (exp1 + "&" + exp2).Split('&');
			Array.Sort(ss, StringComparer.OrdinalIgnoreCase);
			var text = new StringBuilder();
			string? s = null;
			for (int i = 0; i < ss.Length; i++)
			{
				if (s != ss[i])
				{
					s = ss[i];
					text.Append(s).Append('&');
				}
			}
			return text.ToString(0, text.Length - 1);
		}

		private static string CombineByAndHelper(string exp1, string exp2)
		{
			if (exp1 == exp2)
				return exp1;
			if (exp1 == "FALSE" || exp2 == "FALSE")
				return "FALSE";
			if (exp2 == "TRUE" || exp2.Length == 0)
				return exp1;
			if (exp1 == "TRUE" || exp1.Length == 0)
				return exp2;
			Debug.Assert(exp1.IndexOf('|') < 0);
			if (exp2.IndexOf('|') < 0)
			{
				return CombineAndByAnd(exp1, exp2);
			}
			else
			{
				string[] ss = exp2.Split('|');
				Array.Sort(ss, StringComparer.OrdinalIgnoreCase);
				var text = new StringBuilder();
				string? s = null;
				for (int i = 0; i < ss.Length; i++)
				{
					if (s != ss[i])
					{
						s = ss[i];
						text.Append(CombineAndByAnd(exp1, s)).Append('|');
					}
				}
				return text.ToString(0, text.Length - 1);
			}
		}

		/// <summary>Combine two expressions in DNF form by AND operation</summary>
		/// <param name="exp1">Left expression to combine</param>
		/// <param name="exp2">Right expression to combine</param>
		/// <returns>DNF form equalent to (<paramref name="exp1"/>) and (<paramref name="exp2"/>).</returns>
		public static string CombineDnfByAnd(string exp1, string exp2)
		{
			if (exp1 == null)
				throw new ArgumentNullException(nameof(exp1));
			if (exp2 == null)
				throw new ArgumentNullException(nameof(exp2));

			if (exp1.Length == 0)
				return exp2.Length == 0 ? "TRUE": exp2;
			if (exp2.Length == 0)
				return exp1;

			if (exp1 == exp2)
				return exp1;

			if (exp1 == "TRUE")
				return exp2;
			if (exp2 == "TRUE")
				return exp1;
			if (exp1 == "FALSE" || exp2 == "FALSE")
				return "FALSE";
			if (exp1.IndexOf('|') >= 0)
			{
				if (exp2.IndexOf('|') >= 0)
				{
					string[] ss = exp1.Split('|');
					Array.Sort(ss, StringComparer.OrdinalIgnoreCase);
					var text = new StringBuilder();
					string? s = null;
					for (int i = 0; i < ss.Length; i++)
					{
						if (s != ss[i])
						{
							s = ss[i];
							text.Append(CombineByAndHelper(s, exp2)).Append('|');
						}
					}
					return text.ToString(0, text.Length - 1);
				}
				return CombineByAndHelper(exp2, exp1);
			}
			return CombineByAndHelper(exp1, exp2);
		}

		/// <summary>Combine to expression in DNF form by OR operation</summary>
		/// <param name="exp1">Left expression to combine</param>
		/// <param name="exp2">Right expression to combine</param>
		/// <returns>DNF form equalent to (<paramref name="exp1"/>) | (<paramref name="exp2"/>).</returns>
		public static string CombineDnfByOr(string exp1, string exp2)
		{
			if (exp1 == null)
				throw new ArgumentNullException(nameof(exp1));
			if (exp2 == null)
				throw new ArgumentNullException(nameof(exp2));

			if (exp1.Length == 0)
				return exp2.Length == 0 ? "TRUE": exp2;
			if (exp2.Length == 0)
				return exp1;

			if (exp1 == exp2)
				return exp1;

			if (exp1 == "FALSE" || exp2 == "FALSE")
				return exp1 == "FALSE" ? exp2: exp1;
			if (exp1 == "TRUE" || exp2 == "TRUE")
				return "TRUE";
			return exp1 + "|" + exp2;
		}



		public static string MinimizeDnf(string expression)
		{
			try
			{
				expression = __removeSpaceRex.Replace(expression, "").ToUpperInvariant();
				if (expression.Length == 0)
					return "";
				var nameSet = new HashSet<string>();
				foreach (string name in expression.Split(new [] {'|', '&' }, StringSplitOptions.RemoveEmptyEntries))
				{
					nameSet.Add(name[0] == '~' ? name.Substring(1): name);
				}
				string[] names = new string[nameSet.Count];
				nameSet.CopyTo(names);
				Array.Sort(names);
				string[] dzs = expression.Split(new [] {'|'}, StringSplitOptions.RemoveEmptyEntries);
				int[][] matrix = new int[dzs.Length][];
				for (int i = 0; i < matrix.Length; i++)
				{
					matrix[i] = new int[names.Length];
					string[] ss = dzs[i].Split('&');
					for (int j = 0; j < ss.Length; j++)
					{
						string name = ss[j];
						int k = Array.IndexOf(names, name[0] == '~' ? name.Substring(1): name);
						matrix[i][k] = name[0] == '~' ? -1: 1;
					}
				}
				var skip = new BitArray(matrix.Length);
				for (int i = 0; i < matrix.Length; i++)
				{
					if (skip[i])
						continue;
					for (int j = i + 1; j < matrix.Length; j++)
					{
						List<int> dif = Diff(matrix[i], matrix[j]);
						if (dif.Count == 1 && matrix[i][dif[0]] == -matrix[j][dif[0]])
						{
							matrix[i][dif[0]] = 0;
							skip[j] = true;
						}
						else
						{
							bool sup1 = true;
							bool sup2 = true;
							for (int l = 0; l < dif.Count && (sup1 || sup2); l++)
							{
								int k = dif[l];
								if (matrix[i][k] != 0)
									sup1 = false;
								else if (matrix[j][k] != 0)
									sup2 = false;
							}
							if (sup1)
								skip[j] = true;
							else if (sup2)
								skip[i] = true;
						}
					}
				}

				var matrix2 = new List<int[]>();
				for (int i = 0; i < matrix.Length; i++)
				{
					if (!skip[i])
						matrix2.Add(matrix[i]);
				}
				matrix = matrix2.ToArray();

				Array.Sort(matrix, CompareRows); 

				var text = new StringBuilder();
				for (int i = 0; i < matrix.Length; i++)
				{
					bool first = true;
					for (int j = 0; j < matrix[i].Length; j++)
					{
						if (matrix[i][j] != 0)
						{
							if (!first)
								text.Append('&');
							if (matrix[i][j] < 0)
								text.Append('~');
							text.Append(names[j]);
							first = false;
						}
					}
					if (!first)
						text.Append('|');
				}
				return text.Length > 0 ? text.ToString(0, text.Length - 1): "";
			}
			catch(Exception e)
			{
				throw e.Add("expression", expression);
			}
		}

		private static int CompareRows(int[] left, int[] right)
		{
			for (int i = 0; i < left.Length; i++)
			{
				if (left[i] != right[i])
					return left[i] == 0 ? 1:
						right[i] == 0 ? -1:
						left[i] < 0 ? 1: -1;
			}
			return 0;
		}

		private static List<int> Diff(int[] a, int[] b)
		{
			var result = new List<int>(a.Length);
			for (int i = 0; i < a.Length; i++)
			{
				if (a[i] != b[i])
					result.Add(i);
			}
			return result;
		}

		private static readonly Regex __removeSpaceRex = new Regex(@"\s+");

		protected static Exception ParameterTypeException(string name, PolishToken token) => new ArgumentTypeException(name, token?.GetType() ?? typeof(void), typeof(LogicalValue));
	}
}
