using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Lexxys.Configuration;

public ref partial struct CfgParser
{
	public enum NodeType
	{
		Value,
		Object,
		Array,
	}

	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	public class Node
	{
		public string Name { get; set; }
		public string? Value { get; set; }
		public List<Node>? Items { get; private set; }
		public bool AttributeMark { get; set; }
		public FilePosition Position { get; set; }
		public bool IsArrayItem { get; set; }
		public bool IsQuoted { get; set; }

		public NodeType NodeType => Items == null ? NodeType.Value: Items.Any(o => o.IsArrayItem) ? NodeType.Array: NodeType.Object;

		public bool IsObject => NodeType == NodeType.Object;
		public bool IsArray => NodeType == NodeType.Array;
		public bool IsValue => Items == null;
		public bool IsAttribute => AttributeMark && IsValue;

		public Node(string name, string? value = null, FilePosition position = default, bool attribute = false, bool arrayItem = false)
		{
			Name = name;
			Value = value;
			Position = position;
			AttributeMark = attribute;
			IsArrayItem = arrayItem;
		}

		private string DebuggerDisplay
		{
			get
			{
				var text = new StringBuilder();
				if (Name == "-")
					text.Append("\"-\"");
				else if (Name != null)
					text.Append(Name);
				if (Value != null)
					if (Value.Length >20)
						text.Append(" = ").Append(Value.Substring(0, 20)).Append("...");
					else
						text.Append(" = ").Append(Value);

				if (Items is { Count: >0 })
					text.Append(IsArray ? ", Array": ", Object");
				if (AttributeMark)
					text.Append(", Attribute");
				if (IsArrayItem)
					text.Append(", ArrayItem");
				if (Items is { Count: >0 })
					text.Append(", Items = ").Append(Items.Count);
				return text.ToString();
			}
		}

		public void AppendValue(string value) => Value = Value is { Length: >0 } ? Value + value: value;

		public void AppendNewLine()
		{
			if (Value is { Length: >0 })
				Value += "\n";
		}

		public void Add(Node? node)
		{
			if (node == null) return;
			(Items ??= []).Add(node);
		}

		public JsonBuilder BuildJsonObject(JsonBuilder json)
		{
			return BuildJson(json.Obj(), false).End();
		}

		public override string ToString() => ToString(false);

		public string ToString(bool valueOnly) => ToString(new StringBuilder(), valueOnly).ToString();

		public StringBuilder ToString(StringBuilder text, bool valueOnly)
		{
			if (!valueOnly)
				text.Append(Name).Append(AttributeMark ? ": ": "= ");
			if (Items is not { Count: > 0 })
			{
				Print(text, Value);
			}
			else if (Items.Any(o => o.IsArrayItem))
			{
				text.Append('[');
				bool next = false;
				foreach (var item in Items)
				{
					if (next)
						text.Append(", ");
					else
						next = true;
					item.ToString(text, true);
				}
				text.Append(']');
			}
			else
			{
				text.Append('{');
				bool next = false;
				foreach (var item in Items)
				{
					if (next)
						text.Append(", ");
					else
						next = true;
					item.ToString(text, false);
				}
				text.Append('}');
			}
			return text;

			static void Print(StringBuilder text, string? value)
			{
				if (value == null)
					return;
				if (value.Length == 0)
				{
					text.Append("\"\"");
					return;
				}
				int len = text.Length;

				var encoded = Strings.EscapeCsString(value, escape: '`');
				if (encoded.Length == value.Length + 2 && value.IndexOfAny([' ', ',']) < 0)
					text.Append(value);
				else
					text.Append(encoded);
			}
		}

		public JsonBuilder BuildJson(JsonBuilder json, bool valueOnly = false)
		{
			if (!valueOnly)
				json.Item(Name);
			if (Items is not { Count: > 0 })
			{
				SetJsonValue(json, Value);
			}
			else if (Items.Any(o => o.IsArrayItem))
			{
				json.Arr();
				foreach (var item in Items!)
				{
					item.BuildJson(json, true);
				}
				json.End();
			}
			else
			{
				json.Obj();
				foreach (var item in Items!)
				{
					item.BuildJson(json);
				}
				json.End();
			}
			return json;
		}

		private void SetJsonValue(JsonBuilder json, string? value)
		{
			if (value == null || value == "null")
			{
				json.Val((string?)null);
			}
			else if (value == "true")
			{
				json.Val(true);
			}
			else if (value == "false")
			{
				json.Val(false);
			}
			else if (IsQuoted || value.Length > 29 || !Regex.IsMatch(value, @"^-?(:?[1-9]\d*(\.\d+)?|0\.\d+)([eE][+-]?\d+)?$"))
			{
				json.Val(value);
			}
			else
			{
				if (value.Contains('e') || value.Contains('E'))
					json.Val(double.Parse(value, CultureInfo.InvariantCulture));
				else
					json.Val(decimal.Parse(value, CultureInfo.InvariantCulture));
			}
		}
	}

	//public class Node
	//{
	//	public string Name { get; set; }
	//	public string? Value { get; set; }
	//	public bool AttributeMark { get; set; }
	//	public FilePosition Position { get; set; }

	//	public Node(string name, string? value)
	//	{
	//		Name = name;
	//		Value = value;
	//	}

	//	public Node(string name, string? value, FilePosition position)
	//	{
	//		Name = name;
	//		Value = value;
	//		Position = position;
	//	}

	//	public Node(string name, string? value, bool attributeMark, FilePosition position = default)
	//	{
	//		Name = name;
	//		Value = value;
	//		AttributeMark = attributeMark;
	//		Position = position;
	//	}

	//	public void AppendValue(string value)
	//	{
	//		if (Value is { Length: > 0 })
	//			Value += value;
	//		else
	//			Value = value;
	//	}

	//	public void AppendNewLine()
	//	{
	//		if (Value is { Length: > 0 })
	//			Value += "\n";
	//		else
	//			Value = "\n";
	//	}

	//	public void Add(Node? node)
	//	{
	//		if (node == null) return;
	//		if (node is ObjectNode obj)
	//		{
	//			if (obj.Items.Count == 1 && obj.Items[0].Name == null)
	//				Items.AddRange(obj.Items[0].Items);
	//			else
	//				Items.Add(obj);
	//		}
	//		else if (node is ArrayNode array)
	//		{
	//			if (array.ItemName == null)
	//				Items.AddRange(array.Items);
	//			else
	//				Items.Add(array);
	//		}
	//		else
	//		{
	//			Items.Add(node);
	//		}
	//	}
	//}

	//public class ObjectNode: Node
	//{
	//	public List<Node> Items { get; }

	//	public ObjectNode(string name, string? value = default): base(name, value) => Items = [];

	//	public ObjectNode(string name, List<Node>? items, string? value = default) : base(name, value) => Items = items ?? [];
	//}

	//public class ArrayNode: Node
	//{
	//	public string? ItemName { get; set; }
	//	public List<Node> Items { get; }
	//	public ArrayNode(string name, string? value = default) : base(name, value) => Items = [];
	//	public ArrayNode(string name, List<Node>? items, string? value = default) : base(name, value) => Items = items ?? [];
	//}
}
