namespace Lexxys;

public interface IDumpWriter
{
	string Type { get; }

	ObjectDumpLevel Level { get; }

	IDumpWriter Write<T>(string? name, T value);

	IDumpWriter Begin(char type = default, string? name = null);

	IDumpWriter End();
}

public enum ObjectDumpLevel
{
	Regular = 0,    // Minimal info to identify the object and its state
	Debug = 1,      // Include all public members
	Verbose = 2,    // Include all public and non-public members
}
