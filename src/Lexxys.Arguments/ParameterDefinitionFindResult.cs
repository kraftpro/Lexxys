namespace Lexxys;

/// <summary>
/// Identifies the result of looking up a parameter definition by name or alias.
/// </summary>
internal enum ParameterDefinitionFindResult
{
	/// <summary>
	/// No matching definition was found.
	/// </summary>
	NotFound,

	/// <summary>
	/// Exactly one matching definition was found.
	/// </summary>
	Found,

	/// <summary>
	/// More than one definition matched, or a non-collection parameter was already assigned.
	/// </summary>
	Ambiguous,
}
