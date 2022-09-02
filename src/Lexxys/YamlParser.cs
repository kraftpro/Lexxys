// Lexxys Infrastructural library.
// file: YmlParser.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.IO;

namespace Lexxys
{
	using Tokenizer;

	public class YamlParser
	{
		private const int OBJBEG = 1;
		private const int OBJEND = 2;
		private const int ARRBEG = 3;
		private const int ARREND = 4;
		private const int PRMBEG = 5;
		private const int PRMEND = 6;
		private const int COLON = 7;
		private const int COMMA = 8;
		private const int EQUAL = 9;
		private const int DASH = 10;
		private const int THREEDASHES = 11;
		private const int THREEDOTS = 12;
		private const int LITERAL = 13;
		private const int FOLDED = 14;

		private YamlParser(CharStream stream, string? sourceName)
		{
			SourceName = sourceName;
			Scanner = new YamlTokenScanner(stream,
				new WhiteSpaceTokenRule(),
				new StringTokenRule(),
				new PythonCommentsTokenRule(LexicalTokenType.IGNORE),
				new NumericTokenRule(),
				new NameTokenRule().WithNameRecognition(
					nameStart: o => o == '@' || o == '$' || Char.IsLetter(o),
					namePart: o => o == '@' || o == '$' || o == '.' || o == '-' || Char.IsLetterOrDigit(o),
					nameEnd: o => o == '@' || o == '$' || Char.IsLetterOrDigit(o),
					beginning: "@$" + NameTokenRule.DefaultBeginning,
					extra: true
					),

				new SequenceTokenRule("{", "}", "[", "]", "(", ")")
					.Add(COMMA, ",")
					.Add(EQUAL, "=")
					.Add(COLON, ":", ':', (o, _) => o[0] != ':' ? 0 : o[1] switch { ' ' or '\t' => 2, '\n' or '\r' => 1, _ => 0 })
					.Add(DASH, "-", '-', (o, _) => o[0] != '-' ? 0: o[1] switch { ' ' or '\t' => 2, '\n' or '\r' => 1, _ => 0 })
					.Add(THREEDASHES, "---")
					.Add(THREEDOTS, "...")
					.Add(LITERAL, "|")
					.Add(FOLDED, ">")
					,
				new WhiteSpaceTokenRule());
		}

		class YamlTokenScanner: TokenScanner
		{
			private readonly OneBackFilter _push;
			public YamlTokenScanner(CharStream stream, params LexicalTokenRule[] rules) : base(stream, rules)
			{
				_push = new OneBackFilter();
				SetFilter(new IndentFilter());
				SetFilter(_push);
			}

			public void Back()
			{
				_push.Back();
			}
		}


		private YamlTokenScanner Scanner { get; }
		private string? SourceName { get; }

		private SyntaxException SyntaxException(string? message)
		{
			return Scanner.SyntaxException(message, SourceName);
		}


		public static JsonItem Parse(string text, string? sourceName = null)
		{
			if (text == null)
				throw new ArgumentNullException(nameof(text));

			var converter = new YamlParser(new CharStream(text), sourceName);
			converter.Scanner.Next();
			return converter.ParseItem();
		}

		public static JsonItem Parse(TextReader text, string? sourceName = null)
		{
			if (text == null)
				throw new ArgumentNullException(nameof(text));

			var converter = new YamlParser(new CharStream(text), sourceName);
			converter.Scanner.Next();
			return converter.ParseItem();
		}

		// jsonExValue := ['(' args ')'] jsonValue

		private JsonItem ParseItem(bool allowColon = false)
		{
			var token = Scanner.Current;
			List<JsonPair>? args = null;
			// [ '(' args ')' [:] ]
			if (token.Is(LexicalTokenType.SEQUENCE, PRMBEG))
			{
				args = ParseJsonArg();
				if (Scanner.Next().Is(LexicalTokenType.SEQUENCE, COLON) && allowColon)
					Scanner.Next();
				token = Scanner.Current;
			}

			if (token.Is(LexicalTokenType.SEQUENCE, OBJBEG))
				return ParseMap(args);
			if (token.Is(LexicalTokenType.SEQUENCE, ARRBEG))
				return ParseArray(args);
			return ParseScalar(args);
		}

		private JsonScalar ParseScalar(List<JsonPair>? args)
		{
			var token = Scanner.Current;
			if (token.Is(LexicalTokenType.NUMERIC))
				return new JsonScalar(token.Value, token.Text, args);
			if (token.Is(LexicalTokenType.STRING))
				return new JsonScalar(token.Value, token.Text, args);
			if (token.Is(LexicalTokenType.IDENTIFIER))
				return new JsonScalar(
					token.Text == "null" ? null:
					token.Text == "true" ? true:
					token.Text == "false" ? false: (object)token.Text,
					token.Text, args
					);
			Scanner.Back();
			return new JsonScalar(null, args);
		}

		private JsonMap ParseMap(List<JsonPair>? args)
		{
			var result = new List<JsonPair>();

			for (; ; )
			{
				if (Scanner.Next().Is(LexicalTokenType.SEQUENCE, OBJEND))
					return new JsonMap(result, args);
				if (Scanner.EOF)
					throw SyntaxException("Unexpected end of stream found.");

				// pair :=  (name | string) [':' value]
				// pair :=  (name | string) '(' args ')' [':'] [value]
				// pair :=  (name | string) ':' '(' args ')' [value]

				LexicalToken token = Scanner.Current;
				if (!(token.Is(LexicalTokenType.IDENTIFIER) ||token.Is(LexicalTokenType.STRING)))
					throw SyntaxException("Expected name of the element.");

				string name = token.Text;
				JsonItem value = JsonItem.Empty;
				if (Scanner.Next().Is(LexicalTokenType.SEQUENCE, COLON, PRMBEG))
				{
					bool colon = Scanner.Current.Is(LexicalTokenType.SEQUENCE, COLON);
					if (colon)
						Scanner.Next();
					value = ParseItem(!colon);
				}
				result.Add(new JsonPair(name, value));

				if (Scanner.Next().Is(LexicalTokenType.SEQUENCE, OBJEND))
					return new JsonMap(result, args);
				if (!Scanner.Current.Is(LexicalTokenType.SEQUENCE, COMMA))
					throw SyntaxException("Expected comma.");
			}
		}

		private List<JsonPair> ParseJsonArg()
		{
			// args :=  '(' [ arg [',' arg]* ] ')'
			// args :=  '(' [ [arg ',']* arg ] ')'
			// arg  :=  name eq value | value
			// eq   :=  ':' | '='

			if (!Scanner.Current.Is(LexicalTokenType.SEQUENCE, PRMBEG))
				throw SyntaxException("Expected begin of arguments.");

			var args = new List<JsonPair>();
			bool comma = false;
			for (; ; )
			{
				// name
				var token = Scanner.Next();
				if (token.Is(LexicalTokenType.SEQUENCE, PRMEND))
					return args;
				if (comma)
				{
					if (!token.Is(LexicalTokenType.SEQUENCE, COMMA))
						throw SyntaxException("Expected end of parameters or comma.");
					token = Scanner.Next();
				}
				if (!(token.Is(LexicalTokenType.IDENTIFIER) || token.Is(LexicalTokenType.STRING)))
					throw SyntaxException("Expected name of parameter.");
				string name = token.Text;
				if (!Scanner.Next().Is(LexicalTokenType.SEQUENCE, COLON, EQUAL))
					throw SyntaxException("Expected equal or colon symbol for parameter value.");
				Scanner.Next();
				args.Add(new JsonPair(name, ParseScalar(null)));
				comma = true;
			}
		}

		private JsonArray ParseArray(List<JsonPair>? args)
		{
			var result = new List<JsonItem>();

			for (; ; )
			{
				if (Scanner.Next().Is(LexicalTokenType.SEQUENCE, ARREND))
					return new JsonArray(result, args);
				if (Scanner.EOF)
					throw SyntaxException("Unexpected end of stream found.");

				// item0 := jsonValue
				// item0 := jsonObject
				// item0 := jsonArray
				// item := item0
				// item := '(' args ')' item0
				// [ (2, 'A') { here, there }, 

				JsonItem value = ParseItem();
				result.Add(value);

				if (Scanner.Next().Is(LexicalTokenType.SEQUENCE, ARREND))
					return new JsonArray(result, args);
				if (!Scanner.Current.Is(LexicalTokenType.SEQUENCE, COMMA))
					throw SyntaxException("Expected comma.");
			}
		}
	}
}


