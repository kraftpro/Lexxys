namespace Lexxys;

/// <summary>
/// Specifies the configuration file for the assembly.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
public class ConfigFileAttribute: Attribute
{
	/// <summary>
	/// Get of set name of the configuration file.
	/// </summary>
	public string? Name { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ConfigFileAttribute"/> class.
	/// </summary>
	/// <param name="name">Optional name of the configuration file.</param>
	public ConfigFileAttribute(string? name = null) => Name = name;
}
