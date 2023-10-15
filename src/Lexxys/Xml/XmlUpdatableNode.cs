using System.Text;
using System.Xml;

namespace Lexxys.Xml;

/// <summary>
/// Implementation of <see cref="IXmlNode"/> that allows to update its content.
/// </summary>
[Serializable]
public class XmlUpdatableNode: IXmlNode
{
	private List<KeyValuePair<string, string>> _attributes;
	private List<IXmlNode> _elements;

	/// <summary>
	/// Creates new instance of <see cref="XmlUpdatableNode"/>.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="value"></param>
	/// <param name="attributes"></param>
	/// <param name="elements"></param>
	/// <param name="comparer"></param>
	/// <exception cref="ArgumentNullException"></exception>
	public XmlUpdatableNode(string name, string? value, StringComparer? comparer, List<KeyValuePair<string, string>>? attributes, List<IXmlNode>? elements)
	{
		Name = name ?? throw new ArgumentNullException(nameof(name));
		Value = value ?? String.Empty;
		_attributes = attributes ?? new List<KeyValuePair<string, string>>();
		_elements = elements ?? new List<IXmlNode>();
		Comparer = comparer ?? StringComparer.Ordinal;
	}

	/// <summary>
	/// Gets or sets the value of the attribute with the specified name.
	/// </summary>
	/// <param name="name"></param>
	public string? this[string name]
	{
		get => _attributes.FirstOrDefault(o => Comparer.Equals(o.Key, name)).Value;
		set => _attributes.Add(new KeyValuePair<string, string>(name, value ?? String.Empty));
	}

	/// <summary>
	/// Returns the name of the node.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Returns the value of the node.
	/// </summary>
	public string Value { get; set; }

	/// <summary>
	/// Returns the list of attributes of the node.
	/// </summary>
	public IList<KeyValuePair<string, string>> Attributes => _attributes;

	/// <summary>
	/// Returns the list of child nodes of the node.
	/// </summary>
	public IList<IXmlNode> Elements => _elements;

	/// <summary>
	/// Returns the comparer used to compare names of the attributes and child nodes.
	/// </summary>
	public StringComparer Comparer { get; }

	/// <summary>
	/// Indicates whether the node is empty.
	/// </summary>
	public bool IsEmpty => Name.Length == 0 && Value.Length == 0 && _elements.Count == 0 && _attributes.Count == 0;

	IReadOnlyList<KeyValuePair<string, string>> IXmlReadOnlyNode.Attributes => _attributes;

	IReadOnlyList<IXmlReadOnlyNode> IXmlReadOnlyNode.Elements => _elements;

	public IXmlReadOnlyNode AsReadOnly() => new XmlLiteNode(Name, Value, Comparer, _attributes, _elements);

	public XmlReader ReadSubtree() => throw new NotImplementedException();

	public StringBuilder WriteXml(StringBuilder text, bool innerXml, string prefix) => throw new NotImplementedException();

	/// <summary>
	/// Returns an XML text representation of this node.
	/// </summary>
	/// <returns></returns>
	public override string ToString() => ((IXmlReadOnlyNode)this).WriteXml().ToString();

	/// <summary>
	/// Returns an XML text representation of this node.
	/// </summary>
	/// <param name="format">True to format the output using indentation, otherwise false</param>
	/// <param name="innerXml">True to return only inner XML of the node, otherwise false</param>
	/// <returns></returns>
	public string ToString(bool format, bool innerXml = false) => ((IXmlReadOnlyNode)this).WriteXml(null, innerXml, format).ToString();
}