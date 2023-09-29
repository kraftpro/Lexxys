using System.Text;
using System.Xml;

namespace Lexxys.Xml;

public class XmlUpdatableNode: IXmlNode
{
	private List<KeyValuePair<string, string>> _attributes;
	private List<IXmlNode> _elements;
	private readonly IReadOnlyList<KeyValuePair<string, string>> _attributesRo;
	private readonly IReadOnlyList<IXmlReadOnlyNode> _elementsRo;

	public XmlUpdatableNode(string name, string? value, List<KeyValuePair<string, string>>? attributes, List<IXmlNode>? elements, IEqualityComparer<string?>? comparer = null)
	{
		Name = name ?? throw new ArgumentNullException(nameof(name));
		Value = value ?? String.Empty;
		_attributes = attributes ?? new List<KeyValuePair<string, string>>();
		_elements = elements ?? new List<IXmlNode>();
		_attributesRo = ReadOnly.Wrap(_attributes);
		_elementsRo = ReadOnly.Wrap(_elements);
		Comparer = comparer ?? StringComparer.Ordinal;
	}

	public string? this[string name]
	{
		get => _attributes.FirstOrDefault(o => Comparer.Equals(o.Key, name)).Value;
		set => _attributes.Add(new KeyValuePair<string, string>(name, value ?? String.Empty));
	}

	public string Name { get; set; }
	public string Value { get; set; }
	public IList<KeyValuePair<string, string>> Attributes => _attributes;
	public IList<IXmlNode> Elements => _elements;
	public IEqualityComparer<string?> Comparer { get; }
	public bool IsEmpty { get; }

	IReadOnlyList<KeyValuePair<string, string>> IXmlReadOnlyNode.Attributes => _attributesRo;
	IReadOnlyList<IXmlReadOnlyNode> IXmlReadOnlyNode.Elements => _elementsRo;

	public IXmlReadOnlyNode AsReadOnly()
	{
		var attributes = _attributes.ToArray();
		var elements = _elements.ToArray().ConvertAll(o => o.AsReadOnly());
		return new XmlRONode(Name, Value, attributes, elements, Comparer);
	}

	public XmlReader ReadSubtree() => throw new NotImplementedException();

	public StringBuilder WriteXml(StringBuilder text, bool innerXml, string prefix) => throw new NotImplementedException();
}