namespace Lexxys.Configuration;

public interface IConfigService: IConfigSource, IReadOnlyCollection<IConfigSource>
{
	int AddConfiguration(IConfigSource provider, int priority = 0);
}
