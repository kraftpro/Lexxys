// Lexxys Infrastructural library.
// file: JsonParser.cs
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

	public class JsonParser
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

		private JsonParser(CharStream stream, string? sourceName)
		{
			SourceName = sourceName;
			Scanner = new TokenScannerWithBack(stream,
				new StringTokenRule(),
				new CppCommentsTokenRule(LexicalTokenType.IGNORE),
				new NumericTokenRule(),
				new NameTokenRule().WithNameRecognition(
					nameStart: o => o == '@' || o == '$' || Char.IsLetter(o),
					namePart: o => o == '@' || o == '$' || o == '.' || o == '-' || Char.IsLetterOrDigit(o),
					beginning: "@$" + NameTokenRule.DefaultBeginning,
					extra: true
					),
				new SequenceTokenRule("{", "}", "[", "]", "(", ")", ":", ",", "="),
				new WhiteSpaceTokenRule());
		}

		class TokenScannerWithBack: TokenScanner
		{
			private readonly OneBackFilter _push;
			public TokenScannerWithBack(CharStream stream, params LexicalTokenRule[] rules) : base(stream, rules)
			{
				_push = new OneBackFilter();
				SetFilter(_push);
			}

			public void Back()
			{
				_push.Back();
			}
		}


		private TokenScannerWithBack Scanner { get; }
		private string? SourceName { get; }

		private SyntaxException SyntaxException(string message)
		{
			return Scanner.SyntaxException(message, SourceName);
		}


		public static JsonItem Parse(string text, string? sourceName = null)
		{
			if (text == null)
				throw new ArgumentNullException(nameof(text));

			var converter = new JsonParser(new CharStream(text), sourceName);
			converter.Scanner.Next();
			return converter.ParseItem();
		}

		public static JsonItem Parse(TextReader text, string? sourceName = null)
		{
			if (text == null)
				throw new ArgumentNullException(nameof(text));

			var converter = new JsonParser(new CharStream(text), sourceName);
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
			if (token.Is(LexicalTokenType.NUMERIC) || token.Is(LexicalTokenType.STRING))
				return new JsonScalar(token.Value, args);
			if (token.Is(LexicalTokenType.IDENTIFIER))
				return (token.Text) switch
				{
					"null" => args?.Count > 0 ? new JsonScalar(null, args): JsonScalar.Null,
					"true" => args?.Count > 0 ? new JsonScalar(true, args): JsonScalar.True,
					"false" => args?.Count > 0 ? new JsonScalar(false, args): JsonScalar.False,
					_ => new JsonScalar(token.Value, args)
				};
			Scanner.Back();
			return args?.Count > 0 ? new JsonScalar(null, args): JsonScalar.Null;
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
				JsonItem? value = null;
				if (Scanner.Next().Is(LexicalTokenType.SEQUENCE, COLON, PRMBEG))
				{
					bool colon = Scanner.Current.Is(LexicalTokenType.SEQUENCE, COLON);
					if (colon)
						Scanner.Next();
					value = ParseItem(!colon);
				}
				result.Add(new JsonPair(name, value ?? JsonScalar.True));

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


