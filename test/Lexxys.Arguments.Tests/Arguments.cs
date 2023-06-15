namespace Lexxys.Arguments.Tests;

public class ArgumentsParser
{
	private readonly bool _ignoreCase;
	private readonly bool _allowSlash;
	private readonly bool _strictDoubleDash;
	private readonly bool _combineOptions;
	private readonly bool _colonSeparator;
	private readonly bool _equalSeparator;
	private readonly bool _blankSeparator;
	private readonly bool _doubleDashSeparator;
	private readonly bool _ignoreNameSeparators;
	private readonly bool _allowUnknown;

    public ArgumentsParser(
		bool ignoreCase = false,
		bool allowSlash = false,
		bool strictDoubleDash = false,
		bool combineOptions = true,
		bool colonSeparator = true,
		bool equalSeparator = true,
		bool blankSeparator = true,
		bool doubleDashSeparator = true,
		bool ignoreNameSeparators = false,
		bool allowUnknown = false)
    {
        _ignoreCase = ignoreCase;
        _allowSlash = allowSlash;
        _strictDoubleDash = strictDoubleDash;
        _combineOptions = combineOptions;
        _colonSeparator = colonSeparator;
        _equalSeparator = equalSeparator;
        _blankSeparator = blankSeparator;
        _doubleDashSeparator = doubleDashSeparator;
        _ignoreNameSeparators = ignoreNameSeparators;
        _allowUnknown = allowUnknown;
    }

    public ArgumentsParserResult<T> Parse<T>(string[] args) where T: class, new()
    {
        var options = new T();
        //var parser = new ArgumentsParser(options);
        //parser.Parse(args);
        return new ArgumentsParserResult<T>(options);
    }
}

public class ArgumentsParserResult<T> where T: class
{
    public T Options { get; }
    public bool Help { get; }
    public bool Version { get; }
    public string[] Errors { get; }
    public bool Success => Errors.Length == 0;

    public ArgumentsParserResult(T options, bool help = false, bool version = false, string[]? errors = null)
    {
        Options = options;
        Help = help;
        Version = version;
        Errors = errors ?? Array.Empty<string>();
    }
}