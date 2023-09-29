// Lexxys Infrastructural library.
// file: XsdLiteNode.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//

namespace Lexxys.Xml;

public class XsdLiteNode
{
	public XsdLiteNode(string name, XsdLiteValue value, int minCount, int maxCount, IEnumerable<KeyValuePair<string, XsdLiteValue>>? attribute, IEnumerable<XsdLiteNode>? descendant)
	{
		if (minCount < 0)
			throw new ArgumentOutOfRangeException(nameof(minCount), minCount, null);
		if (maxCount < minCount)
			throw new ArgumentOutOfRangeException(nameof(maxCount), maxCount, null).Add(nameof(minCount), minCount);

		Name = name ?? throw new ArgumentNullException(nameof(name));
		Value = value ?? throw new ArgumentNullException(nameof(value));
		Attribute = ReadOnly.WrapCopy(attribute) ?? ReadOnly.Empty<KeyValuePair<string, XsdLiteValue>>();
		Descendant = ReadOnly.WrapCopy(descendant) ?? ReadOnly.Empty<XsdLiteNode>();
		MinCount = minCount;
		MaxCount = maxCount;
	}

	public string Name { get; }

	public IList<KeyValuePair<string, XsdLiteValue>> Attribute { get; }

	public XsdLiteValue Value { get; }

	public IList<XsdLiteNode> Descendant { get; }

	public int MinCount { get; }

	public int MaxCount { get; }

	public bool IsMatch(XmlLiteNode node)
	{
		if (node == null)
			throw new ArgumentNullException(nameof(node));
		if (!node.Comparer.Equals(Name, node.Name))
			return false;
		if (!Value.IsMatch(node.Value))
			return false;
		if (Attribute.Any(o => !o.Value.IsMatch(node[o.Key])))
			return false;

		foreach (var item in Descendant)
		{
			IList<XmlLiteNode> nodes = node.Where(item.Name).ToList();
			if (nodes.Count < MinCount)
				return false;
			if (nodes.Count > MaxCount)
				return false;
			if (nodes.Any(o => !item.IsMatch(o)))
				return false;
		}

		return true;
	}

	/// <summary>
	/// Construct the object from XmlLiteNode
	///	sample:
	///		element
	///			:name	Company
	///			:min	0
	///			:max	5
	///			attribute
	///				:name	CompanyName
	///				:type	string
	///			attribute
	///				:name	IncorporationDate
	///				:type	datetime
	///				:nullable	true
	///			element
	///				:name	Staff
	///				:min	0
	///				:max	1
	///				element
	///					:name	Person
	///				...
	///		Company
	///			:min	0
	///			:max	5
	///			attributes
	///				CompanyName
	///					:type	string
	///				IncorporationDate
	///					:type	datetime
	///					:nullable	true
	///			Staff
	///				:min	0
	///				:max	1
	///				Person
	///				...
	/// </summary>
	/// <param name="node"></param>
	/// <returns></returns>
	public static XsdLiteNode? FromXml(XmlLiteNode? node)
	{
		if (node == null || node.IsEmpty)
			return null;

		string name = node["name"] ?? node.Name;
		if (String.IsNullOrEmpty(name))
			return null;
		var value = XsdLiteValue.FromXml(node);
		if (value == null)
			return null;

		int minCount = node["min"].AsInt32(0);
		int maxCount = node["max"].AsInt32(Int32.MaxValue);
		var attribs = new List<KeyValuePair<string, XsdLiteValue>>();
		var subNodes = new List<XsdLiteNode>();
		foreach (var item in node.Elements)
		{
			if (item.Name == "attributes")
			{
				foreach (var attr in item.Elements)
				{
					var s = attr["name"] ?? attr.Name;
					var v = XsdLiteValue.FromXml(attr);
					if (s != null && v != null)
						attribs.Add(new KeyValuePair<string, XsdLiteValue>(s, v));
				}
			}
			else if (item.Name == "attribute" && item["name"] != null)
			{
				var s = item["name"].AsString(String.Empty);
				var v = XsdLiteValue.FromXml(item);
				if (s != null && v != null)
					attribs.Add(new KeyValuePair<string, XsdLiteValue>(s, v));
			}
			else
			{
				var x = XsdLiteNode.FromXml(item);
				if (x != null)
					subNodes.Add(x);
			}
		}

		return new XsdLiteNode(name, value, minCount, maxCount, attribs, subNodes);
	}
}

public class XsdLiteValue
{
	public Type ValueType { get; }
	public bool IsNullable { get; }

	public XsdLiteValue(Type valueType)
	{
		if (valueType == null)
			throw new ArgumentNullException(nameof(valueType));

		ValueType = Factory.NullableTypeBase(valueType);
		IsNullable = Factory.IsNullableType(valueType);
	}

	public XsdLiteValue(Type valueType, bool isNullable)
	{
		if (valueType == null)
			throw new ArgumentNullException(nameof(valueType));

		ValueType = Factory.NullableTypeBase(valueType);
		IsNullable = isNullable;
	}

	public bool IsMatch(string? value)
	{
		return value is { Length: >0 } ? Strings.TryGetValue(value, ValueType, out var _): IsNullable;
	}

	/// <summary>
	/// Construct the object from XmlLiteNode
	///	sample:	type="System.Int16" nullable="true"
	/// </summary>
	/// <param name="node"></param>
	/// <returns></returns>
	public static XsdLiteValue? FromXml(XmlLiteNode? node)
	{
		if (node == null || node.IsEmpty)
			return null;

		Type type = Strings.GetType(node["type"] ?? node.Value, typeof(void));
		return type == typeof(void) ? null:
			node["nullable"] == null ? new XsdLiteValue(type): new XsdLiteValue(type, node["nullable"].AsBoolean(false));
	}
}


