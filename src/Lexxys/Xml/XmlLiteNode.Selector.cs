using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace Lexxys.Xml;

internal static class XmlNodeSelector
{
	public static IEnumerable<IXmlReadOnlyNode> Select(string selector, IEnumerable<IXmlReadOnlyNode>? nodes)
	{
		if (nodes == null)
			return Array.Empty<IXmlReadOnlyNode>();
		return String.IsNullOrWhiteSpace(selector) ? nodes : (Node.Parse(selector) ?? __empty).Select(nodes);
	}
	private static readonly Node __empty = new Node("");

	public static Func<IEnumerable<IXmlReadOnlyNode>, IEnumerable<IXmlReadOnlyNode>> Compile(string selector)
	{
		return String.IsNullOrWhiteSpace(selector) ? EmptySelector :
			__compiledSelectors.GetOrAdd(selector.Trim(), CompileNew);
	}
	private static readonly ConcurrentDictionary<string, Func<IEnumerable<IXmlReadOnlyNode>, IEnumerable<IXmlReadOnlyNode>>> __compiledSelectors =
		new ConcurrentDictionary<string, Func<IEnumerable<IXmlReadOnlyNode>, IEnumerable<IXmlReadOnlyNode>>>();

	private static IEnumerable<IXmlReadOnlyNode> EmptySelector(IEnumerable<IXmlReadOnlyNode>? nodes)
	{
		return nodes ?? Array.Empty<IXmlReadOnlyNode>();
	}

	private static Func<IEnumerable<IXmlReadOnlyNode>, IEnumerable<IXmlReadOnlyNode>> CompileNew(string selector)
	{
		return Node.Parse(selector)?.CompileSelect() ?? throw new ArgumentException($"Cannot parser expression: '{selector}'", nameof(selector));
	}

	enum Op
	{
		None,
		Eq,     // ==
		Neq,    // !=
	}

	private class Node
	{
		private readonly string _text;
		private readonly bool _attrib;
		private readonly Op _condition;
		private readonly string? _value;
		private readonly Node? _guard;
		private readonly Node? _next;

		public Node(string text)
		{
			_text = text;
		}

		private Node(string text, bool attrib, Op condition = Op.None, string? value = null, Node? guard = null, Node? next = null)
		{
			_text = text;
			_attrib = attrib;
			_condition = condition;
			_value = value;
			_guard = guard;
			_next = next;
		}

		public static Node? Parse(string value)
		{
			return new Parser(value).Parse();
		}

		public IEnumerable<IXmlReadOnlyNode> Select(IEnumerable<IXmlReadOnlyNode> nodes)
		{
			if (nodes == null)
				throw new ArgumentNullException(nameof(nodes));
			Debug.Assert(_condition == Op.None);

			if (_text.Length > 0 && _text != "*" && _text != "**")
				nodes = _attrib ?
					nodes.Where(o => o[_text] != null).Select(o => new XmlLiteNode(_text, o[_text], o.Comparer, null, null)) :
					nodes.Where(o => o.Comparer.Equals(o.Name, _text));
			if (_guard != null)
				nodes = nodes.Where(_guard.Where);

			if (_text == "**")
				return _next == null ? AllDescendants(nodes) : _next.Select(AllDescendants(nodes));

			return _next == null ? nodes :
				_next._attrib ? _next.Select(nodes) :
				_next.Select(nodes.SelectMany(o => o.Elements));
		}

		private static IEnumerable<IXmlReadOnlyNode> AllDescendants(IEnumerable<IXmlReadOnlyNode> nodes)
		{
			return nodes is ICollection<IXmlReadOnlyNode> cl ? AllDescendants(cl) :
				nodes is IReadOnlyCollection<IXmlReadOnlyNode> ro ? AllDescendants(ro) :
				AllDescendants((ICollection<IXmlReadOnlyNode>)nodes.ToList());
		}

		private static IEnumerable<IXmlReadOnlyNode> AllDescendants(ICollection<IXmlReadOnlyNode> nodes)
		{
			return nodes.Concat(nodes.Where(o => o.Elements.Count > 0).SelectMany(o => AllDescendants(o.Elements)));
		}

		private static IEnumerable<IXmlReadOnlyNode> AllDescendants(IReadOnlyCollection<IXmlReadOnlyNode> nodes)
		{
			return nodes.Concat(nodes.Where(o => o.Elements.Count > 0).SelectMany(o => AllDescendants(o.Elements)));
		}

		public override string ToString()
		{
			var text = new StringBuilder();
			text.Append("{ ");
			if (_attrib)
				text.Append(':');
			text.Append(_text.Length == 0 ? "." : _text);
			if (_condition != Op.None)
			{
				text.Append(_condition == Op.Eq ? "==" : "!=");
				if (_value == null)
					text.Append("(null)");
				else
					text.Append('"').Append(_value).Append('"');
			}
			else if (_value != null)
			{
				text.Append("!!").Append(_value);
			}
			if (_guard != null)
				text.Append("[ ").Append(_guard).Append(" ]");
			text.Append(" }");
			if (_next != null)
				text.Append(_next);
			return text.ToString();
		}

		private bool Where(IXmlReadOnlyNode node)
		{
			if (_condition == Op.None)
			{
				Debug.Assert(_value == null);
				if (_text.Length == 0)
					return (_guard == null || _guard.Where(node)) && (_next == null || _next.Where(node));

				IEnumerable<IXmlReadOnlyNode> nodes = _text == "*" ? node.Elements : node.Where(_text);
				if (_guard != null)
					nodes = nodes.Where(o => _guard.Where(o));
				if (_next != null)
					nodes = nodes.Where(o => _next.Where(o));
				return nodes.Any();
			}

			Debug.Assert(_next == null);
			Debug.Assert(_guard == null);

			bool result;
			if (_text.Length == 0)
			{
				result = String.IsNullOrEmpty(_value) ? String.IsNullOrEmpty(node.Value) : node.Comparer.Equals(node.Value, _value);
			}
			else
			{
				Debug.Assert(_attrib);
				result = String.IsNullOrEmpty(_value) ? String.IsNullOrEmpty(node[_text]) : node.Comparer.Equals(_value, node[_text]);
			}
			return _condition == Op.Eq ? result : !result;
		}

		public Func<IEnumerable<IXmlReadOnlyNode>, IEnumerable<IXmlReadOnlyNode>> CompileSelect()
		{
			Func<IEnumerable<IXmlReadOnlyNode>, IEnumerable<IXmlReadOnlyNode>>? selector = null;
			if (_text.Length > 0 && _text != "*")
				if (_attrib)
					selector = p => p.Where(o => o[_text] != null).Select(o => new XmlLiteNode(_text, o[_text], o.Comparer, null, null));
				else
					selector = p => p.Where(o => o.Comparer.Equals(o.Name, _text));

			Func<IEnumerable<IXmlReadOnlyNode>, IEnumerable<IXmlReadOnlyNode>>? guarded;
			if (_guard != null)
			{
				Func<IXmlReadOnlyNode, bool> guard = _guard.CompileWhere();
				guarded = selector == null ? (Func<IEnumerable<IXmlReadOnlyNode>, IEnumerable<IXmlReadOnlyNode>>)
					(p => p.Where(guard)) :
					(p => selector(p).Where(guard));
			}
			else
			{
				guarded = selector;
			}

			if (_next == null)
				return guarded ?? (o => o);

			Func<IEnumerable<IXmlReadOnlyNode>, IEnumerable<IXmlReadOnlyNode>> next = _next.CompileSelect();
			if (_next._attrib)
				return guarded == null ? next : o => next(guarded(o));

			return guarded == null ?
				(o => next(o.SelectMany(p => p.Elements))) :
				(o => next(guarded(o).SelectMany(p => p.Elements)));
		}

		private Func<IXmlReadOnlyNode, bool> CompileWhere()
		{
			if (_condition == Op.None)
			{
				Debug.Assert(_value == null);
				var guard = _guard?.CompileWhere();
				var next = _next?.CompileWhere();
				if (_text.Length == 0)
				{
					return next == null ? guard ?? (_ => true) :
						guard == null ? next : o => guard(o) && next(o);
				}

				Func<IXmlReadOnlyNode, IEnumerable<IXmlReadOnlyNode>> nodes = _text == "*" ?
					(o => o.Elements) :
					(o => o.Where(_text));

				return next == null ?
					guard == null ? (Func<IXmlReadOnlyNode, bool>)
						(o => nodes(o).Any()) :
						(o => nodes(o).Any(guard)) :
					guard == null ? (Func<IXmlReadOnlyNode, bool>)
						(o => nodes(o).Any(next)) :
						(o => nodes(o).Any(p => guard(p) && next(p)));
			}

			Debug.Assert(_next == null);
			Debug.Assert(_guard == null);

			if (_text.Length == 0)
			{
				if (String.IsNullOrEmpty(_value))
					return _condition == Op.Eq ?
						(o => String.IsNullOrEmpty(o.Value)) :
						(o => !String.IsNullOrEmpty(o.Value));
				else
					return _condition == Op.Eq ?
						(o => o.Comparer.Equals(o.Value, _value)) :
					(o => !o.Comparer.Equals(o.Value, _value));
			}
			else
			{
				Debug.Assert(_attrib);
				if (String.IsNullOrEmpty(_value))
					return _condition == Op.Eq ?
						(o => String.IsNullOrEmpty(o[_text])) :
						(o => !String.IsNullOrEmpty(o[_text]));
				else
					return _condition == Op.Eq ?
						(o => o.Comparer.Equals(o[_text], _value)) :
						(o => !o.Comparer.Equals(o[_text], _value));
			}
		}

		/// <summary>
		/// Query expression parser.
		/// <code>
		/// <b>Syntax:</b><br/>
		/// query		:= node_query node_query*
		/// node_query	:= node [condition]
		/// node		:= name ['.' name]*
		/// attrib		:= (':'|'@') name
		/// reference	:= node [ attrib ]
		/// condition	:= '[' reference ('=' | '==' | '!=') value ']'
		/// escape_char	:= '`'
		/// </code>
		/// </summary>
		private ref struct Parser
		{
			private readonly ReadOnlySpan<char> _selector;
			int _index;

			public Parser(string value)
			{
				if (value is null)
					throw new ArgumentNullException(nameof(value));
				_selector = value.AsSpan();
				_index = -1;
			}

			public Node? Parse()
			{
				_index = -1;
				return Parse(false);
			}

			private Node? Parse(bool condition, bool attrib = false)
			{
				var text = new StringBuilder();
				while (++_index < _selector.Length)
				{
					char c = _selector[_index];
					if (c == '`')
					{
						if (++_index < _selector.Length)
							c = _selector[_index];
					}
					else if (c is '.' or ':' or '@' or '[')
					{
						string s = text.ToString().Trim();
						Node? node;
						if (c == '.')
						{
							node = new Node(s, attrib, Op.None, null, null, Parse(condition));
							if (node._next == null)
								return null;
							if (node._next._text.Length == 0)
								node = new Node(s, node._next._attrib, node._next._condition, node._next._value, node._next._guard, node._next._next);
						}
						else
						{
							node = c switch
							{
								'[' => new Node(s, attrib, Op.None, null, Parse(true), Parse(condition)),
								':' or '@' => new Node(s, false, Op.None, null, null, Parse(condition, true)),
								_ => new Node(s)
							};
						}
						if (node._text.Length == 0 && node._guard == null)
							node = node._next;
						return node;
					}
					else if (condition && (c is '=' or '!' or ']'))
					{
						string s = text.ToString().Trim();
						if (c == ']')
							return s.Length == 0 ? null : new Node(s, attrib);

						if (c == '!')
						{
							if (_index + 1 >= _selector.Length || _selector[_index + 1] != '=')
							{
								text.Append(c);
								continue;
							}
							++_index;
						}
						else    // c == '='
						{
							if (_index + 1 >= _selector.Length || _selector[_index + 1] == '=')
								++_index;
						}
						text.Clear();
						while (++_index < _selector.Length)
						{
							c = _selector[_index];
							if (c == ']')
								break;

							if (c == '`' && ++_index < _selector.Length)
								c = _selector[_index];
							text.Append(c);
						}
						return !attrib && s.Length > 0 ?
							new Node(s, false, Op.None, null, null, new Node("", false, c == '!' ? Op.Neq : Op.Eq, text.ToString())) :
							new Node(s, attrib, c == '!' ? Op.Neq : Op.Eq, text.ToString());
					}
					text.Append(c);
				}

				return text.Length == 0 ? null : new Node(text.ToString(), attrib);
			}
		}
	}
}
