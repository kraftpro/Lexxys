using Lexxys;
using Lexxys.Configuration;
using Lexxys.Data;




static string SqlStr(ref SqlInterpolatedHandler value)
{
	return value.ToStringAndClear();
}


var ss = SqlStr($"select * from Documents where DtCreated < {DateTime.UtcNow} and ID in {[1, 2, 3]} or ID = {(ushort?)22}-{(short)3}");
var sa = SqlStr($"select * from Documents where ID in (@IDS)");

Console.WriteLine(ss);
Statics.AddConfigServices();

var r = new RowVersion(0x00000000000007da);
Console.WriteLine(r.ToString());

var nf = new BlobNameFormatter(minLength: 3, segmentCount: 2, segmentLength: -2);
Console.WriteLine(nf.CreateName("abcdef"));

var sc = Dc.StaticDataFactory.CreateContext(new ConnectionStringInfo
{
	TrustServerCertificate = true,
	Server = ".",
	Database = "DocumentsConverter"
});

var rv = sc.GetValue<RowVersion>(Dc.Sql("select top 1 Version from Documents"));
Console.WriteLine(rv.ToString());

string config = """
	school
		name I.K. High School
		address
			street 123 Main Street
			city Anytown
			state NY
			zip 12345
		buildings
			item
				address
					street 123 Main Street
					city Anytown
					state NY
					zip 12345
				area 1000
				name Main Building
	""";

var node = CfgParser.ParseConfig(config);

var c0 = Config.Current.GetValue<ConfigSection>(Dc.ConfigSection, null);
Config.Current.SetValue<ConnectionStringInfo>(Dc.ConfigSection, new ConnectionStringInfo { TrustServerCertificate = true, Server = ".", Database= "CharityPlanner" });
var c1 = Config.Current.GetValue<ConnectionStringInfo>(Dc.ConfigSection, null);
var c1v = c1.Value;
List<int> docs = Dc.Instance.GetList<int>($"select ID from Documents");
long total = 0;
Console.WriteLine($"count: {docs.Count}");
var t = DateTime.UtcNow;
docs.AsParallel()
	// .WithDegreeOfParallelism(1) // Environment.ProcessorCount * 16)
	.ForAll(id =>
	{
		total += Dc.Instance.GetValue<int>(Dc.Sql("select FileLength from Documents where ID=@D"), Dc.Parameter("@D", id));
	});
//	.ForAll(async id =>
//	{
//		total += await Dc.Instance.GetValueAsync<int>("select FileLength from Documents where ID=@D", Dc.Parameter("@D", id));
//	});
Console.WriteLine($"total: {total} {(DateTime.UtcNow - t).TotalSeconds}");
