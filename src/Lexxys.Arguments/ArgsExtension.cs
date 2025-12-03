using System.Diagnostics.CodeAnalysis;

namespace Lexxys;

public static class ArgsExtension
{
	extension (IArguments args)
	{
		[return: NotNullIfNotNull(nameof(defaultValue))]
		public string GetValue(string name, string defaultValue = null!) => TryGetValue(args, name, out var value) ? value: defaultValue;

		[return: NotNullIfNotNull(nameof(defaultValue))]
		public T GetValue<T>(string name, T defaultValue = default!) => TryGetValue<T>(args, name, out var value) ? value: defaultValue;

		public bool TryGetValue<T>(string name, [MaybeNullWhen(false)]out T value, ICollection<string>? errors = null)
		{
			if (args == null) throw new ArgumentNullException(nameof(args));

			var required = args.Definition.Parameters[name]?.IsRequired ?? false;
			if (args[name].TryConvert(out value, required, errors, name))
				return true;

			value = default;
			return false;
		}

		public bool TryGetValue(string name, [MaybeNullWhen(false)] out string value, ICollection<string>? errors = null)
		{
			if (args == null) throw new ArgumentNullException(nameof(args));

			var p = args[name];
			if (p.IsEmpty)
			{
				if (args.Definition.Parameters[name]?.IsRequired == true)
					errors?.Add($"parameter {name} is required");
				value = null;
				return false;
			}
			value = p.ToString();
			return true;
		}

		public string[] GetCollection(string name)
		{
			TryGetCollection(args, name, out var value);
			return value;
		}

		public T[] GetCollection<T>(string name)
		{
			TryGetCollection<T>(args, name, out var value);
			return value;
		}

		public bool TryGetCollection<T>(string name, out T[] value, ICollection<string>? errors = null)
		{
			if (args == null) throw new ArgumentNullException(nameof(args));

			var required = args.Definition.Parameters[name]?.IsRequired ?? false;
			if (!args[name].TryConvert(out value, required, errors, name))
			{
				value = [];
				return false;
			}
			return true;
		}

		public bool TryGetCollection(string name, out string[] value, ICollection<string>? errors = null)
		{
			if (args == null) throw new ArgumentNullException(nameof(args));

			ParameterValue p = args[name];
			if (p.IsEmpty)
			{
				if (args.Definition.Parameters[name]?.IsRequired == true)
					errors?.Add($"parameter {name} is required");
				value = [];
				return false;
			}
			value = p.ToArray();
			return true;
		}
	}
}
