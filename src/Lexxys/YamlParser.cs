//// Lexxys Infrastructural library.
//// file: YmlParser.cs
////
//// Copyright (c) 2001-2014, Kraft Pro Utilities.
//// You may use this code under the terms of the MIT license
////
//using System;
//using System.Collections.Generic;
//using System.IO;

//namespace Lexxys
//{
//	using Tokenizer;

//	public class YamlParser
//	{
//		private const int OBJBEG = 1;
//		private const int OBJEND = 2;
//		private const int ARRBEG = 3;
//		private const int ARREND = 4;
//		private const int COLON = 5;
//		private const int COMMA = 6;
//		private const int EQUAL = 7;
//		private const int DASH = 8;
//		private const int THREEDASHES = 9;
//		private const int THREEDOTS = 10;
//		private const int LITERAL = 11;
//		private const int FOLDED = 12;

//		private YamlParser(in CharStream stream, string? sourceName)
//		{
//			SourceName = sourceName;
//			Scanner = new YamlTokenScanner(stream,
//				new WhiteSpaceTokenRule(),
//				new StringTokenRule(),
//				new PythonCommentsTokenRule(LexicalTokenType.IGNORE),
//				new NumericTokenRule(),
//				new NameTokenRule().WithNameRecognition(
//					nameStart: o => o == '@' || o == '$' || Char.IsLetter(o),
//					namePart: o => o == '@' || o == '$' || o == '.' || o == '-' || Char.IsLetterOrDigit(o),
//					nameEnd: o => o == '@' || o == '$' || Char.IsLetterOrDigit(o),
//					beginning: "@$" + NameTokenRule.DefaultBeginning,
//					extra: true),
//				new SequenceTokenRule("{", "}", "[", "]")
//					.Add(COMMA, ",")
//					.Add(EQUAL, "=")
//					.Add(COLON, ":", ':', (o, _) => o[0] != ':' ? 0 : o[1] switch { ' ' or '\t' => 2, '\n' or '\r' => 1, _ => 0 })
//					.Add(DASH, "-", '-', (o, _) => o[0] != '-' ? 0: o[1] switch { ' ' or '\t' => 2, '\n' or '\r' => 1, _ => 0 })
//					.Add(THREEDASHES, "---")
//					.Add(THREEDOTS, "...")
//					.Add(LITERAL, "|")
//					.Add(FOLDED, ">"),
//				new WhiteSpaceTokenRule());
//		}

//		class YamlTokenScanner: TokenScanner
//		{
//			private readonly OneBackFilter _push;
//			public YamlTokenScanner(in CharStream stream, params LexicalTokenRule[] rules) : base(stream, rules)
//			{
//				_push = new OneBackFilter();
//				SetFilter(new IndentFilter());
//				SetFilter(_push);
//			}

//			public void Back()
//			{
//				_push.Back();
//			}
//		}


//		private YamlTokenScanner Scanner { get; }

//		private string? SourceName { get; }

//		private SyntaxException SyntaxException(string? message)
//		{
//			return Scanner.SyntaxException(message, SourceName);
//		}


//		public static JsonItem Parse(string text, string? sourceName = null)
//		{
//			if (text == null)
//				throw new ArgumentNullException(nameof(text));

//			var converter = new YamlParser(new CharStream(text), sourceName);
//			converter.Scanner.Next();
//			return converter.ParseItem();
//		}

//		public static JsonItem Parse(TextReader text, string? sourceName = null)
//		{
//			if (text == null)
//				throw new ArgumentNullException(nameof(text));

//			var converter = new YamlParser(new CharStream(text), sourceName);
//			converter.Scanner.Next();
//			return converter.ParseItem();
//		}

//		private JsonItem ParseItem()
//		{
//			var token = Scanner.Current;
//			if (token.Is(LexicalTokenType.SEQUENCE, OBJBEG))
//				return ParseMap();
//			if (token.Is(LexicalTokenType.SEQUENCE, ARRBEG))
//				return ParseArray();
//			return ParseScalar();
//		}

//		private JsonScalar ParseScalar()
//		{
//			var token = Scanner.Current;
//			if (token.Is(LexicalTokenType.NUMERIC))
//				return new JsonScalar(token.GetValue(Scanner.Stream));
//			if (token.Is(LexicalTokenType.STRING))
//				return new JsonScalar(token.GetValue(Scanner.Stream));
//			if (token.Is(LexicalTokenType.IDENTIFIER))
//				return new JsonScalar(token.GetString(Scanner.Stream) switch
//				{
//					"null" => null,
//					"true" => true,
//					"false" => false,
//					_ => token.GetString(Scanner.Stream)
//				});
//			Scanner.Back();
//			return new JsonScalar(null);
//		}

//		private JsonMap ParseMap()
//		{
//			var result = new List<JsonPair>();

//			for (;;)
//			{
//				if (Scanner.Next().Is(LexicalTokenType.SEQUENCE, OBJEND))
//					return new JsonMap(result);
//				if (Scanner.EOF)
//					throw SyntaxException("Unexpected end of stream found.");

//				// pair :=  (name | string) ':' [value]

//				LexicalToken token = Scanner.Current;
//				if (!(token.Is(LexicalTokenType.IDENTIFIER) || token.Is(LexicalTokenType.STRING)))
//					throw SyntaxException("Expected name of the element.");

//				string name = token.GetString(Scanner.Stream);
//				if (!Scanner.Next().Is(LexicalTokenType.SEQUENCE, COLON))
//					throw SyntaxException("Expected colon.");
//				Scanner.Next();
//				var value = ParseItem();
//				result.Add(new JsonPair(name, value));

//				if (Scanner.Next().Is(LexicalTokenType.SEQUENCE, OBJEND))
//					return new JsonMap(result);
//				if (!Scanner.Current.Is(LexicalTokenType.SEQUENCE, COMMA))
//					throw SyntaxException("Expected comma.");
//			}
//		}

//		private JsonArray ParseArray()
//		{
//			var result = new List<JsonItem>();

//			for (;;)
//			{
//				if (Scanner.Next().Is(LexicalTokenType.SEQUENCE, ARREND))
//					return new JsonArray(result);
//				if (Scanner.EOF)
//					throw SyntaxException("Unexpected end of stream found.");

//				// item0 := jsonValue
//				// item0 := jsonObject
//				// item0 := jsonArray
//				// item := item0

//				JsonItem value = ParseItem();
//				result.Add(value);

//				if (Scanner.Next().Is(LexicalTokenType.SEQUENCE, ARREND))
//					return new JsonArray(result);
//				if (!Scanner.Current.Is(LexicalTokenType.SEQUENCE, COMMA))
//					throw SyntaxException("Expected comma.");
//			}
//		}
//	}
//}


