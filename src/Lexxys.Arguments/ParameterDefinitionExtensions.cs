// ReSharper disable VariableHidesOuterVariable
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Lexxys;

internal static class ParameterDefinitionExtensions
{
	public static ParameterDefinition? FindExact(this IEnumerable<ParameterDefinition> parameters, string name, StringComparison comparison, bool abbreviationOnly = false)
	{
		if (name is null) throw new ArgumentNullException(nameof(name));

		foreach (var p in parameters)
		{
			if (!abbreviationOnly && String.Equals(p.Name, name, comparison) || p.Abbreviations != null && p.Abbreviations.Any(o => String.Equals(o, name, comparison)))
				return p;
		}
		return default;
	}

	private static ParameterDefinition? FindExact(this IEnumerable<ArgumentParameter> parameters, string name, StringComparison comparison)
	{
		if (name is null) throw new ArgumentNullException(nameof(name));

		foreach (var p in parameters)
		{
			var i = p.Definition;
			if (String.Equals(i.Name, name, comparison) || i.Abbreviations != null && i.Abbreviations.Any(o => String.Equals(o, name, comparison)))
				return i;
		}
		return default;
	}

    private static IReadOnlyList<ParameterDefinition> FindAuto(this IEnumerable<ParameterDefinition> parameters, string name, bool ignoreDelimiters, StringComparison comparison)
        => parameters.Where(o => o.IsReverseSimilar(name, comparison, ignoreDelimiters)).ToList();

    private static IReadOnlyList<ParameterDefinition> FindSimilar(this IEnumerable<ParameterDefinition> parameters, string name, bool ignoreDelimiters, StringComparison comparison)
		=> parameters.Where(o => o.IsSimilar(name, comparison, ignoreDelimiters)).ToList();

	private static IReadOnlyList<ArgumentParameter> FindSimilar(this IEnumerable<ArgumentParameter> parameters, string name, bool ignoreDelimiters, StringComparison comparison)
		=> parameters.Where(o => o.Definition.IsSimilar(name, comparison, ignoreDelimiters)).ToList();

	public static ParameterDefinitionFindResult TryFind(this IReadOnlyCollection<ParameterDefinition> parameters, string name, out ParameterDefinition? parameter, StringComparison comparison, bool auto = false, bool ignoreDelimiters = false)
	{
		if (name is null) throw new ArgumentNullException(nameof(name));

		parameter = FindExact(parameters, name, comparison);
		if (parameter != null)
			return ParameterDefinitionFindResult.Found;
		if (!auto)
			return ParameterDefinitionFindResult.NotFound;

		var similar = FindAuto(parameters, name, ignoreDelimiters, comparison);
		if (similar.Count == 0)
			return ParameterDefinitionFindResult.NotFound;
		if (similar.Count > 1)
			return ParameterDefinitionFindResult.Ambiguous;

		parameter = similar[0];
		return ParameterDefinitionFindResult.Found;
	}

	public static ParameterDefinitionFindResult TryFind(this IReadOnlyCollection<ArgumentParameter> parameters, string name, out ParameterDefinition? parameter, StringComparison comparison, bool findSimilar = false, bool ignoreDelimiters = false)
	{
		if (name is null) throw new ArgumentNullException(nameof(name));

		parameter = FindExact(parameters, name, comparison);
		if (parameter != null)
			return ParameterDefinitionFindResult.Found;
		if (!findSimilar)
			return ParameterDefinitionFindResult.NotFound;

		var similar = FindSimilar(parameters, name, ignoreDelimiters, comparison);
		if (similar.Count == 0)
			return ParameterDefinitionFindResult.NotFound;
		if (similar.Count > 1)
			return ParameterDefinitionFindResult.Ambiguous;

		parameter = similar[0].Definition;
		return ParameterDefinitionFindResult.Found;
	}

	public static ParameterDefinitionFindResult TryFindDefinition(this IReadOnlyCollection<ParameterDefinition> parameters, string name, out ParameterDefinition? definition, StringComparison comparison, bool similar = false, bool ignoreDelimiters = false, bool abbreviationOnly = false)
	{
		if (name == null) throw new ArgumentNullException(nameof(name));

		var pd = parameters.FindExact(name, comparison, abbreviationOnly);
		if (pd is not null)
		{
			definition = pd;
			return ParameterDefinitionFindResult.Found;
		}
		if (similar)
		{
			var ppd = parameters.FindSimilar(name, ignoreDelimiters, comparison);
			if (ppd.Count == 1)
			{
				definition = ppd[0];
				return ParameterDefinitionFindResult.Found;
			}
			if (ppd.Count > 1)
			{
				definition = null;
				return ParameterDefinitionFindResult.Ambiguous;
			}
		}
		definition = null;
		return ParameterDefinitionFindResult.NotFound;
	}

	public static ParameterDefinitionFindResult TryFindDefinition(this IReadOnlyCollection<ArgumentParameter> parameters, string name, out ParameterDefinition? definition, StringComparison comparison, bool similar = false, bool ignoreDelimiters = false)
	{
		if (name == null) throw new ArgumentNullException(nameof(name));

		var pd = parameters.FindExact(name, comparison);
		if (pd is not null)
		{
			definition = pd;
			return ParameterDefinitionFindResult.Found;
		}
		if (similar)
		{
			var ppd = parameters.FindSimilar(name, ignoreDelimiters, comparison);
			if (ppd.Count == 1)
			{
				definition = ppd[0].Definition;
				return ParameterDefinitionFindResult.Found;
			}
			if (ppd.Count > 1)
			{
				definition = null;
				return ParameterDefinitionFindResult.Ambiguous;
			}
		}
		definition = null;
		return ParameterDefinitionFindResult.NotFound;
	}
}
