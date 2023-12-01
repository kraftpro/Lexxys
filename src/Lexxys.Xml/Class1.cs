using System.Xml;
using System.Xml.Linq;

namespace Lexxys.Xml;

public class Class1
{
	public void Test()
	{
		XDocument x = new XDocument();
	}

}

public class XElem: IXmlReadOnlyNode
{
	private XElement _e;
	private IReadOnlyList<KeyValuePair<string, string>>? _attributes;
	private IReadOnlyList<IXmlReadOnlyNode>? _elements;

	public XElem(XDocument document)
	{
		_e = document?.Root ?? throw new ArgumentNullException(nameof(document));
		_e.Changed += Element_Changed;
	}

	public XElem(XElement element)
    {
		_e = element;
		_e.Changed += Element_Changed;
    }

	private void Element_Changed(object? sender, XObjectChangeEventArgs e)
	{
		_attributes = null;
		_elements = null;
	}

	public string? this[string name] => Attributes.FirstOrDefault(o => Comparer.Equals(o.Key, name)).Value;

	public StringComparer Comparer => StringComparer.Ordinal;

	public IReadOnlyList<KeyValuePair<string, string>> Attributes => _attributes ??= _e.Attributes().Select(a => new KeyValuePair<string, string>(a.Name.LocalName, a.Value)).ToList();

	public IReadOnlyList<IXmlReadOnlyNode> Elements => _elements ??= _e.Descendants().Select(o => new XElem(o)).ToList();

	public string Name => _e.Name.LocalName;

	public string Value => _e.Value;

	public bool IsEmpty => _e.IsEmpty;
}
