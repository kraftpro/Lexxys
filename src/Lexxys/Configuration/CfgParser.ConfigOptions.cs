namespace Lexxys.Configuration;

public ref partial struct CfgParser
{
	public class ConfigOptions
	{
		private StringComparer _comparer;
		private Dictionary<string, List<Node>> _variable;
		private Dictionary<string, IExternalAction> _externalAction;

		public ConfigOptions()
		{
			_comparer = StringComparer.Ordinal;
			_variable = new Dictionary<string, List<Node>>(Comparer);
			_externalAction = new Dictionary<string, IExternalAction>(Comparer);
		}

		public StringComparer Comparer => _comparer;

		public Dictionary<string, List<Node>> Variable => _variable;

		public Dictionary<string, IExternalAction> ExternalActions => _externalAction;

		public void Clear()
		{
			_comparer = StringComparer.Ordinal;
			_variable.Clear();
			_externalAction.Clear();
		}

		public void SetComparer(StringComparer value)
		{
			if (value == null) throw new ArgumentNullException(nameof(value));
			if (value != _comparer)
			{
				_comparer = value;
				_variable = Clone(_variable, value);
				_externalAction = Clone(_externalAction, value);
			}

			static Dictionary<string, T> Clone<T>(Dictionary<string, T> source, StringComparer comparer)
			{
				var tmp = new Dictionary<string, T>(source.Count, comparer);
				foreach (var item in source)
				{
					tmp[item.Key] = item.Value;
				}
				return tmp;
			}
		}

		public void AddVariable(string name, List<Node> value)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (value == null) throw new ArgumentNullException(nameof(value));
			_variable[name] = value;
		}

		public List<Node>? GetVariable(string name) => _variable.GetValueOrDefault(name);

		public string? GetVariableText(string name)
		{
			if (!_variable.TryGetValue(name, out var value))
				return null;
			return String.Join(", ", value.Select(o => o.ToString(true)));
		}

		public void AddExternalAction(string name, IExternalAction action)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (action == null) throw new ArgumentNullException(nameof(action));
			_externalAction[name] = action;
		}

		public bool TryExecuteExternalAction(string name, ref CfgParser parser, List<Node> parameters, out Node? result)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));
			result = null;
			if (!_externalAction.TryGetValue(name, out var action))
				return false;
			result = action.Execute(ref parser, parameters);
			return true;
		}
	}

	public interface IExternalAction
	{
		Node? Execute(ref CfgParser parser, List<Node> parameters);
	}
}