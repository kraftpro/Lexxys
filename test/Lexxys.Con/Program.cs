using Amazon.S3;

using Lexxys;
using Lexxys.Con;
using Lexxys.Con.ArgsCon;
using Lexxys.Con.Ycl;
using Lexxys.Configuration;
using Lexxys.Data;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System.Reflection;
using System.Runtime.CompilerServices;



TestYclConfig.Run();




var cmd = args.Length > 0 ? args[0]: null;
if (cmd != null)
	args = args[1..];

CallContext.Go();

(int, object?[]) tpl = (3, [123]);
var tpl2 = tpl;
var tpl3 = (Count: 3, Data: new[] { 123 });
Console.WriteLine(TplStr(tpl2));
Console.WriteLine(TplStr(tpl));
Console.WriteLine(TplStr3(tpl3));

cmd ??= "ycl";

switch (cmd)
{
	case "args":
		ArgsUsage.Run(args);
		return;

	case "dump":
		DumpTest.Run(args);
		return;

	case "ycl":
		TestYclConfig.Run();
		return;

	default:
		break;
}

static string TplStr((int Count, object?[] Data) obj)
{
	return obj.ToString() ?? "";
}

static string TplStr3(object? obj)
{
	return obj?.ToString() ?? "";
}

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
var sr = SqlStr($"select * from Documents where Version = {r}");

IBlobStorageService<string> bsss = Unsafe.BitCast<IBlobStorageService, IBlobStorageService<string>>(new FileStorageService(@"D:\Data\Documents"));


IHostBuilder host = Host.CreateDefaultBuilder(args);
host.ConfigureServices((context, services) =>
{
	var config = context.Configuration;
	var documentsLocation = config.GetValue<string>("BlobStorage:DocumentsLocation") ?? @"D:\Data\Documents";
	var checksLocation = config.GetValue<string>("BlobStorage:ChecksLocation") ?? @"D:\Data\Checks";
	var awsOpt = context.Configuration.GetAWSOptions();

	services.AddBlobStorageFactory();
	services.AddKeyedSingleton<IBlobStorageService>("documents", (_, _) =>
		new FileStorageService(documentsLocation));
	services.AddKeyedSingleton<IBlobStorageService>("images", (sp, _) =>
		new AmazonBlobStorageService("image-bucket", awsOpt.CreateServiceClient<IAmazonS3>()));

	services.AddBlobStorage<ServiceTag.Documents>(sp => new FileStorageService(documentsLocation));
	services.AddBlobStorage<ServiceTag.Checks>(sp => new FileStorageService(checksLocation));
	services.AddBlobStorage<ServiceTag.Images>(new AmazonBlobStorageService("image-bucket", awsOpt.CreateServiceClient<IAmazonS3>()));
});

//var nf = new PathFormatter(minLength: 3, segmentCount: 2, segmentLength: -2);
//Console.WriteLine(nf.CreateName("abcdef"));

var sc = Dc.StaticDataFactory.Create(new ConnectionStringInfo
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


public static class ServiceTag
{
	public class Documents { }
	public class Checks { }
	public class Images { }
}