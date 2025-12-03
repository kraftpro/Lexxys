namespace Lexxys;

public interface IDump
{
	void DumpContent(IDumpWriter writer);
}

public interface IDumpValue: IDump;
