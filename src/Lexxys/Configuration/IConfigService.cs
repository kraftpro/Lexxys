namespace Lexxys.Configuration;

public interface IConfigService: IConfigSource
{
	int AddConfiguration(IConfigSource provider, int priority = 0);
}
