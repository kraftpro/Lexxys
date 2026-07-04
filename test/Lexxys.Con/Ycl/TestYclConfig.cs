using Lexxys.Configuration;

using System;
using System.Collections.Generic;
using System.Text;

namespace Lexxys.Con.Ycl;

internal class TestYclConfig
{
	public static void Run()
	{
		var options = new DumpWriterOptions{ FormatIndentation = true, MaxLength = int.MaxValue };

		//var config2 = YclParser.Parse(Xxx);
		//Console.WriteLine(config2.Dump(options));

		//var config3 = YclParser.Parse(Yyy);
		//Console.WriteLine(config3.Dump(options));

		var config4 = YclParser.ParseFile(@"C:\Application\Config-0\fsadmin.rabbit.entitlement.config.txt");
		Console.WriteLine(config4.Dump(options));

		var xx = Xx;
		xx = """
			root
			%*/param/- key val
			%nodeA nameA valueA
			%nodeB nameB valueB
			nodeA A 1
				extra: 2
				param
					- p1 v1
			""";
		xx = Xx;
		var config5 = YclParser.Parse(xx);
		Console.WriteLine(config5.Dump(options));

		var config = YclParser.ParseFile(@"C:\Application\Config-0\fsadmin.db.config.txt");
		var text = config.ToString();
		Console.WriteLine(config.Dump(options));
	}
	static string Xx = """
		entitlement

			%*/parameters/- key value
			%queue name
			%exchangeBind destination source routingKey

			// queue        name:, durable:, exclusive:, autoDelete:, parameters
			// exchange     name:, type:, durable:, autoDelete:, parameters
			// queueBind    queue:, exchange:, routingKey:, parameters
			// exchangeBind	destination:, source:, routingKey:, parameters
			// qos          prefetchSize:, prefetchCount:, global:
			// consumer     queue:, consumerTag:, autoAck:, noLocal:, exclusive:, parameters
			// producer     exchange:, routingKey:, mandatory:, parameters

			queue		admin
				durable:		yes
				parameters
					- x-dead-letter-exchange admin/dead

		""";

	static string Xxx = """
		%$item33 = (address = (street = "123A Main Street", city: AAnytown, state: AY, zip = 00000), area = 1000, name = "Main Building A")

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
				item ${item33|}
		""";
	static string Yyy = """
		hvb
			from		test-support@foundationsource.com
			to			test-primary@foundationsource.com
			bcc			test-transfer@foundationsource.com
			sendViaEmail	no
			sftpServerHost	172.17.3.51
			sftpServerPort	22
			sftpUserName	bofa-dr
			sftpOutboundFolder		IPIMPORT/
			sftpInboundFolder		inbound/bofa-prod/
			secretName		SnbConnectionSettings
			fsAccountWithSnb		FOUND06824T
			snbPublicEncryptionKey	RE8tTk9ULVVTRQ==	# This is DO-NOT-USE converted to base 64. It will not be used, but must be a valid base 64 string.
			snbPublicSshKey	RE8tTk9ULVVTRQ==	# This is DO-NOT-USE converted to base 64. It will not be used, but must be a valid base 64 string.
			snbBalanceAndTransactionFilePath	D:\Temp\FsData\qa1\snb_balance_and_transaction
		""";
}
