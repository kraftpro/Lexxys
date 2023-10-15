// Lexxys Infrastructural library.
// file: JsonParser.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys;

using Tokenizer;

public static class JsonParser
{
	public static JsonItem Parse(string text)
	{
		if (text == null)
			throw new ArgumentNullException(nameof(text));

		var stream = new CharStream(text);
		var converter = new ActualParser();
		converter.Scanner.Next(ref stream);
		return converter.ParseItem(ref stream);
	}

	public static JsonItem Parse(TextReader text)
	{
		if (text == null)
			throw new ArgumentNullException(nameof(text));

		var stream = new CharStream(text);
		var converter = new ActualParser();
		converter.Scanner.Next(ref stream);
		return converter.ParseItem(ref stream);
	}

	readonly ref struct ActualParser
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

		private readonly OneBackFilter _back;

		public ActualParser()
		{
			_back = new OneBackFilter();
			Scanner = new TokenScanner([_back],
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

		public TokenScanner Scanner { get; }

		// jsonExValue := ['(' args ')'] jsonValue

		public JsonItem ParseItem(ref CharStream stream, bool allowColon = false)
		{
			var token = _back.Value;
			List<JsonPair>? args = null;
			// [ '(' args ')' [:] ]
			if (token.Is(LexicalTokenType.SEQUENCE, PRMBEG))
			{
				args = ParseJsonArg(ref stream);
				if (Scanner.Next(ref stream).Is(LexicalTokenType.SEQUENCE, COLON) && allowColon)
					Scanner.Next(ref stream);
				token = _back.Value;
			}

			if (token.Is(LexicalTokenType.SEQUENCE, OBJBEG))
				return ParseMap(ref stream, args);
			if (token.Is(LexicalTokenType.SEQUENCE, ARRBEG))
				return ParseArray(ref stream, args);
			return ParseScalar(ref stream, args);
		}

		private JsonScalar ParseScalar(ref CharStream stream, List<JsonPair>? args)
		{
			var token = _back.Value;
			if (token.Is(LexicalTokenType.NUMERIC) || token.Is(LexicalTokenType.STRING))
				return new JsonScalar(token.GetValue(stream), args);
			if (token.Is(LexicalTokenType.IDENTIFIER))
				return (token.GetString(stream)) switch
				{
					"null" => args?.Count > 0 ? new JsonScalar(null, args): JsonScalar.Null,
					"true" => args?.Count > 0 ? new JsonScalar(true, args): JsonScalar.True,
					"false" => args?.Count > 0 ? new JsonScalar(false, args): JsonScalar.False,
					"NaN" => args?.Count > 0 ? new JsonScalar(double.NaN, args): JsonScalar.NaN,
					_ => new JsonScalar(token.GetString(stream), args)
				};
			_back.Back();
			return args?.Count > 0 ? new JsonScalar(null, args): JsonScalar.Null;
		}

		private JsonMap ParseMap(ref CharStream stream, List<JsonPair>? args)
		{
			var result = new List<JsonPair>();

			for (; ; )
			{
				if (Scanner.Next(ref stream).Is(LexicalTokenType.SEQUENCE, OBJEND))
					return new JsonMap(result, args);
				if (_back.Value.IsEof)
					throw stream.SyntaxException("Unexpected end of stream found.");

				// pair :=  (name | string) [':' value]
				// pair :=  (name | string) '(' args ')' [':'] [value]
				// pair :=  (name | string) ':' '(' args ')' [value]

				LexicalToken token = _back.Value;
				if (!(token.Is(LexicalTokenType.IDENTIFIER) ||token.Is(LexicalTokenType.STRING)))
					throw stream.SyntaxException("Expected name of the element.");

				string name = token.GetString(stream);
				JsonItem? value = null;
				if (Scanner.Next(ref stream).Is(LexicalTokenType.SEQUENCE, COLON, PRMBEG))
				{
					bool colon = _back.Value.Is(LexicalTokenType.SEQUENCE, COLON);
					if (colon)
						Scanner.Next(ref stream);
					value = ParseItem(ref stream, !colon);
				}
				result.Add(new JsonPair(name, value ?? JsonScalar.True));

				if (Scanner.Next(ref stream).Is(LexicalTokenType.SEQUENCE, OBJEND))
					return new JsonMap(result, args);
				if (!_back.Value.Is(LexicalTokenType.SEQUENCE, COMMA))
					throw stream.SyntaxException("Expected comma.");
			}
		}

		private List<JsonPair> ParseJsonArg(ref CharStream stream)
		{
			// args :=  '(' [ arg [',' arg]* ] ')'
			// args :=  '(' [ [arg ',']* arg ] ')'
			// arg  :=  name eq value | value
			// eq   :=  ':' | '='

			if (!_back.Value.Is(LexicalTokenType.SEQUENCE, PRMBEG))
				throw stream.SyntaxException("Expected begin of arguments.");

			var args = new List<JsonPair>();
			bool comma = false;
			for (; ; )
			{
				// name
				var token = Scanner.Next(ref stream);
				if (token.Is(LexicalTokenType.SEQUENCE, PRMEND))
					return args;
				if (comma)
				{
					if (!token.Is(LexicalTokenType.SEQUENCE, COMMA))
						throw stream.SyntaxException("Expected end of parameters or comma.");
					token = Scanner.Next(ref stream);
				}
				if (!(token.Is(LexicalTokenType.IDENTIFIER) || token.Is(LexicalTokenType.STRING)))
					throw stream.SyntaxException("Expected name of parameter.");
				string name = token.GetString(stream);
				if (!Scanner.Next(ref stream).Is(LexicalTokenType.SEQUENCE, COLON, EQUAL))
					throw stream.SyntaxException("Expected equal or colon symbol for parameter value.");
				Scanner.Next(ref stream);
				args.Add(new JsonPair(name, ParseScalar(ref stream, null)));
				comma = true;
			}
		}

		private JsonArray ParseArray(ref CharStream stream, List<JsonPair>? args)
		{
			var result = new List<JsonItem>();

			for (; ; )
			{
				if (Scanner.Next(ref stream).Is(LexicalTokenType.SEQUENCE, ARREND))
					return new JsonArray(result, args);
				if (_back.Value.IsEof)
					throw stream.SyntaxException("Unexpected end of stream found.");

				// item0 := jsonValue
				// item0 := jsonObject
				// item0 := jsonArray
				// item := item0
				// item := '(' args ')' item0
				// [ (2, 'A') { here, there }, 

				JsonItem value = ParseItem(ref stream);
				result.Add(value);

				if (Scanner.Next(ref stream).Is(LexicalTokenType.SEQUENCE, ARREND))
					return new JsonArray(result, args);
				if (!_back.Value.Is(LexicalTokenType.SEQUENCE, COMMA))
					throw stream.SyntaxException("Expected comma.");
			}
		}
	}
	
}


