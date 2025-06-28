using System;

namespace Lexxys.Configuration.New;

public static class ConfigNodeExtensions
{

    public static T? AsValue<T>(this ConfigNode? node)
    {
        if (node is null or node.IsEmpty)
            return default;

        if (typeof(T) == typeof(ConfigNode))
            return (T)(object)node;

        if (node.Value is not null)
            return node.Value.AsValue<T>();

        if (typeof(T) == typeof(string))
            return (T)(object)node.ToString();

		if ()
    }

}
