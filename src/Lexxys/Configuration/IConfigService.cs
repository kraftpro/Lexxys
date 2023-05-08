namespace Lexxys.Configuration;

public interface IConfigService: IConfigSource
{
	bool AddConfiguration(Uri location, IReadOnlyCollection<string>? parameters = null, bool tail = false);
	bool AddConfiguration(IConfigProvider provider, bool tail = false);
}
