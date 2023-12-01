﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Lexxys.Tests;

public class TestAttrib
{
	public static void Go()
	{
		AttrubNamePair[] a = [new AttrubNamePair("class", new AttributeValue("zero", "one"))];
		a[0].Value.Add("two");
		a[0].Value.Add("three");
		a[0] = new AttrubNamePair("class", new AttributeValue("zero"));
		a[0].Value.Add("one");
		a[0].Value.Add("two");
		a[0].Value.Add("three");
	}
}

public struct AttributeValue
{
	private object? _value;

	public AttributeValue(string value) => _value = value.TrimToNull();

	public AttributeValue(string? value1, string? value2)
	{
		value1 = value1.TrimToNull();
		value2 = value2.TrimToNull();
		if (value1 is null)
			_value = Clean(value2);
		else if (value2 is null)
			_value = Clean(value1);
		else
			_value = new StringBuilder(value1.Length + value2.Length + 1).AppendAttributePart(value1).Append(' ').AppendAttributePart(value2);
	}

	public AttributeValue(string? value1, string? value2, string? value3)
	{
		value1 = value1.TrimToNull();
		value2 = value2.TrimToNull();
		value3 = value3.TrimToNull();
		if (value1 is null)
			if (value2 is null)
				_value = Clean(value3);
			else if (value3 is null)
				_value = Clean(value2);
			else
				_value = new StringBuilder(value2.Length + value3.Length + 1).AppendAttributePart(value2).Append(' ').AppendAttributePart(value3);
		else if (value2 is null)
			if (value3 is null)
				_value = Clean(value1);
			else
				_value = new StringBuilder(value1.Length + value3.Length + 1).AppendAttributePart(value1).Append(' ').AppendAttributePart(value3);
		else if (value3 is null)
			_value = new StringBuilder(value1.Length + value2.Length + 1).AppendAttributePart(value1).Append(' ').AppendAttributePart(value2);
		else
			_value = new StringBuilder(value1.Length + value2.Length + value3.Length + 2).AppendAttributePart(value1).Append(' ').AppendAttributePart(value2).Append(' ').AppendAttributePart(value3);
	}

	public AttributeValue(params string[] value)
	{
		if (value is null || value.Length == 0) return;
		if (value.Length == 1)
		{
			_value = Clean(value[0].TrimToNull());
			return;
		}
		var text = new StringBuilder(value.Length + value.Sum(o => o?.Length ?? 0));
		foreach (var item in value)
		{
			var v = item.AsSpan().Trim();
			if (v.Length == 0)
				continue;
			if (text.Length > 0)
				text.Append(' ');
			text.AppendAttributePart(v);
		}
		_value = text;
	}

	public AttributeValue(IEnumerable<string> value)
	{
		if (value is null) return;
		var text = value switch
		{
			ICollection<string> ic => new StringBuilder(ic.Count + value.Sum(o => o?.Length ?? 0)),
			IReadOnlyCollection<string> ic => new StringBuilder(ic.Count + value.Sum(o => o?.Length ?? 0)),
			_ => new StringBuilder()
		};

		foreach (var item in value)
		{
			var v = item.AsSpan().Trim();
			if (v.Length == 0)
				continue;
			if (text.Length > 0)
				text.Append(' ');
			text.AppendAttributePart(v);
		}
		_value = text;
	}

	public readonly bool IsEmpty => _value is null;

	public AttributeValue Add(string value)
	{
		var v = value.AsSpan().Trim();
		if (v.Length == 0) return this;
		if (_value is string s)
		{
			if (value != s)
				_value = new StringBuilder(s.Length + v.Length + 1).Append(s).Append(' ').AppendAttributePart(v);
		}
		else if (_value is null)
		{
			_value = Clean(value.Length == v.Length ? value : v.ToString());
		}
		else // if (_value is StringBuilder)
		{
			var l = Unsafe.As<StringBuilder>(_value);
			l.Append(' ').AppendAttributePart(v);
		}
		return this;
	}

	public AttributeValue Add(string value1, string value2)
	{
		var v1 = value1.AsSpan().Trim();
		if (v1.Length == 0)
			return Add(value2);
		var v2 = value2.AsSpan().Trim();
		if (v2.Length == 0)
			return Add(value1);

		StringBuilder text;
		if (_value is string s)
			_value = text = new StringBuilder(s.Length + v1.Length + v2.Length + 2).Append(s).Append(' ');
		else if (_value is null)
			_value = text = new StringBuilder(v1.Length + v2.Length + 1);
		else // if (_value is StringBuilder)
			text = Unsafe.As<StringBuilder>(_value).Append(' ');

		text.AppendAttributePart(v1).Append(' ').AppendAttributePart(v2);
		return this;
	}

	public AttributeValue Add(string value1, string value2, string value3)
	{
		var v1 = value1.AsSpan().Trim();
		if (v1.Length == 0)
			return Add(value2, value3);
		var v2 = value2.AsSpan().Trim();
		if (v2.Length == 0)
			return Add(value1, value3);
		var v3 = value3.AsSpan().Trim();
		if (v3.Length == 0)
			return Add(value1, value2);

		StringBuilder text;
		if (_value is string s)
			_value = text = new StringBuilder(s.Length + v1.Length + v2.Length + v3.Length + 3).Append(s).Append(' ');
		else if (_value is null)
			_value = text = new StringBuilder(v1.Length + v2.Length + v3.Length + 2);
		else // if (_value is StringBuilder)
			text = Unsafe.As<StringBuilder>(_value).Append(' ');
		text.AppendAttributePart(v1).Append(' ').AppendAttributePart(v2).Append(' ').AppendAttributePart(v3);
		return this;
	}

	public AttributeValue Add(string value1, string value2, string value3, string value4)
	{
		var v1 = value1.AsSpan().Trim();
		if (v1.Length == 0)
			return Add(value2, value3, value4);
		var v2 = value2.AsSpan().Trim();
		if (v2.Length == 0)
			return Add(value1, value3, value4);
		var v3 = value3.AsSpan().Trim();
		if (v3.Length == 0)
			return Add(value1, value2, value4);
		var v4 = value4.AsSpan().Trim();
		if (v4.Length == 0)
			return Add(value1, value2, value3);

		StringBuilder text;
		if (_value is string s)
			_value = text = new StringBuilder(s.Length + v1.Length + v2.Length + v3.Length + v4.Length + 4).Append(s).Append(' ');
		else if (_value is null)
			_value = text = new StringBuilder(v1.Length + v2.Length + v3.Length + v4.Length + 3);
		else // if (_value is StringBuilder)
			text = Unsafe.As<StringBuilder>(_value).Append(' ');
		text.AppendAttributePart(v1).Append(' ').AppendAttributePart(v2).Append(' ').AppendAttributePart(v3).Append(' ').AppendAttributePart(v4);
		return this;
	}

	public AttributeValue Add(params string[] value)
	{
		StringBuilder text;
		if (_value is string s)
			_value = text = new StringBuilder(s.Length + value.Length + value.Sum(o => o?.Length ?? 0)).Append(s).Append(' ');
		else if (_value is null)
			_value = text = new StringBuilder(value.Length + value.Sum(o => o?.Length ?? 0));
		else // if (_value is StringBuilder)
			text = Unsafe.As<StringBuilder>(_value).Append(' ');
		bool first = true;
		foreach (var item in value)
		{
			var v = item.AsSpan().Trim();
			if (v.Length == 0)
				continue;
			if (first)
				first = false;
			else
				text.Append(' ');
			text.AppendAttributePart(item);
		}
		return this;
	}

	public override readonly string ToString() => _value switch
	{
		string s => s,
		StringBuilder t => t.ToString(),
		_ => String.Empty
	};

	[return: NotNullIfNotNull(nameof(value))]
	private static string? Clean(string? value) => value is null ? null : value.Length > 2 && value[0] == value[^1] && value[0] is '"' or '\'' ? value[1..^1] : HttpUtility.HtmlAttributeEncode(value);
}

static class StrBuExtensions
{
	public static StringBuilder EncodeAttribute(this StringBuilder text, ReadOnlySpan<char> value, ReadOnlySpan<char> line)
	{
		int i;
		while ((i = value.IndexOfAny(line)) >= 0)
		{
			text.Append(value.Slice(0, i));
			var c = value[i];
			value = value.Slice(i + 1);
			if (c == '&')
			{
				var k = TestReEncoding(value);
				if (k == 0)
				{
					text.Append("&amp;");
				}
				else
				{
					text.Append('&').Append(value.Slice(0, k));
					value = value.Slice(k);
				}
			}
			else
			{
				text.Append(c switch
					{
						'<' => "&lt;",
						'>' => "&gt;",
						'"' => "&quot;",
						'\'' => "&apos;",
						'&' => "&amp;",
						_ => null
					});
			}
		}

		return text.Append(value);

		static int TestReEncoding(ReadOnlySpan<char> value)
		{
			if (value.Length < 3) return 0;
			return value[0] switch
			{
				'l' => (value[1] == 't' && value[2] == ';') ? 3: 0,
				'g' => (value[1] == 't' && value[2] == ';') ? 3: 0,
				'a' => (value.Length > 3 && value[1] == 'm') ? (value[2] == 'p' && value[3] == ';') ? 4: 0:
					   (value.Length > 4 && value[1] == 'p' && value[2] == 'o' && value[3] == 's' && value[4] == ';') ? 5: 0,
				'q' => (value.Length > 4 && value[1] == 'u' && value[2] == 'o' && value[3] == 't' && value[4] == ';') ? 5: 0,
				_ => 0,
			};
		}
	}

	public static StringBuilder AppendAttributePart(this StringBuilder text, ReadOnlySpan<char> value) =>
		value.Length < 2 || value[0] != value[^1] ? text.EncodeAttribute(value, "<\"&"):
		value[0] is '"' ? text.Append(value[1..^1]):
		value[0] is '\'' ? text.EncodeAttribute(value[1..^1], "<\""):
		text.EncodeAttribute(value, "<\"&");

	public static StringBuilder AppendAttribute(this StringBuilder text, ReadOnlySpan<char> value) =>
		value.Length < 2 || value[0] != value[^1] ? text.Append('"').EncodeAttribute(value, "<\"&").Append('"'):
		value[0] is '"' or '\'' ? text.Append(value): text.Append('"').EncodeAttribute(value, "<\"&").Append('"');
}

readonly struct AttrubNamePair
{
	public readonly string Name;
	public readonly AttributeValue Value;

	public AttrubNamePair(string name, AttributeValue value)
	{
		Name = name ?? throw new ArgumentNullException(nameof(name));
		Value = value;
	}

	public static implicit operator AttrubNamePair((string Name, AttributeValue Value) value) => new AttrubNamePair(value.Name, value.Value);
}