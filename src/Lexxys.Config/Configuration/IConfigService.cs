namespace Lexxys.Configuration;

internal interface IConfigService
{
	int AddConfiguration(IConfigSource provider, int priority = 0);
	IConfigSource Build();
}


