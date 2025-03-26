using Lexxys;
using Lexxys.Configuration;
using Lexxys.Data;

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

Statics.AddServices(o => o.AddConfigService());
var c0 = Config.Current.GetValue<ConfigSection>(Dc.ConfigSection, null);
Config.Current.SetValue<ConnectionStringInfo>(Dc.ConfigSection, new ConnectionStringInfo { TrustServerCertificate = true, Server = ".", Database= "CharityPlanner" });
var c1 = Config.Current.GetValue<ConnectionStringInfo>(Dc.ConfigSection, null);
var c1v = c1.Value;
List<int> docs = Dc.Instance.GetList<int>("select ID from Documents");
long total = 0;
Console.WriteLine($"count: {docs.Count}");
var t = DateTime.UtcNow;
docs.AsParallel()
	// .WithDegreeOfParallelism(1) // Environment.ProcessorCount * 16)
	.ForAll(id =>
	{
		total += Dc.Instance.GetValue<int>("select FileLength from Documents where ID=@D", Dc.Parameter("@D", id));
	});
//	.ForAll(async id =>
//	{
//		total += await Dc.Instance.GetValueAsync<int>("select FileLength from Documents where ID=@D", Dc.Parameter("@D", id));
//	});
Console.WriteLine($"total: {total} {(DateTime.UtcNow - t).TotalSeconds}");
