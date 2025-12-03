namespace Lexxys;

public partial class Arguments
{

	internal class ArgumentsDictionary
	{
		private readonly Dictionary<string, ParameterValue> _dictionary;
		protected readonly bool _ignoreCase;

		public ArgumentsDictionary(bool ignoreCase = false)
		{
			_ignoreCase = ignoreCase;
			_dictionary = new Dictionary<string, ParameterValue>(ignoreCase ? StringComparer.OrdinalIgnoreCase: StringComparer.Ordinal);
		}

		public ArgumentsDictionary Clone()
		{
			var clone = EmptyClone();
			foreach (var kvp in _dictionary)
				clone._dictionary.Add(kvp.Key, kvp.Value);
			return clone;
		}

		public virtual ParameterValue this[string name]
		{
			get => _dictionary.TryGetValue(name, out var value) ? value: ParameterValue.Empty;
			set => _dictionary[name] = value;
		}

		public int Count => _dictionary.Count;

		protected Dictionary<string, ParameterValue>.KeyCollection Keys => _dictionary.Keys;

		public bool TryGetValue(string name, out ParameterValue value) => _dictionary.TryGetValue(name, out value);

		public virtual bool ContainsKey(string name) => _dictionary.ContainsKey(name);

		public Dictionary<string, ParameterValue>.Enumerator GetEnumerator() => _dictionary.GetEnumerator();

		public void Add(string name, ParameterValue value) => _dictionary.Add(name, value);

		protected virtual ArgumentsDictionary EmptyClone() => new ArgumentsDictionary(_ignoreCase);
	}

	internal class AutoArgumentsDictionary: ArgumentsDictionary
	{
		private readonly bool _fluent;

		public AutoArgumentsDictionary(bool fluent, bool ignoreCase): base(ignoreCase) => _fluent = fluent;

		public override ParameterValue this[string name]
		{
			get
			{
				var key = FindKey(name);
				return key != null ? base[key]: default;
			}
			set => base[name] = value;
		}

		public override bool ContainsKey(string name) => FindKey(name) != null;

		protected override ArgumentsDictionary EmptyClone() => new AutoArgumentsDictionary(_fluent, _ignoreCase);

		private string? FindKey(string name)
		{
			if (base.ContainsKey(name))
				return name;

			string? result = null;
			foreach (var key in Keys)
			{
				if (CloseEquals(key, name, !_ignoreCase, _fluent || !HasDelimiter(key)))
				{
					if (result != null)
						return null;
					result = key;
				}
			}
			return result;
		}
	}
}
