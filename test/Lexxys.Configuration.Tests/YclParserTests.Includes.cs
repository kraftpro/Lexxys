namespace Lexxys.Configuration.Tests;

public partial class YclParserTests
{
	[Test]
	public async Task ParseFile_Include_AllowsExternalParserHandler()
	{
		var directory = Path.Combine(Path.GetTempPath(), "lexxys-ycl-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		try
		{
			var part = Path.Combine(directory, "part.properties");
			File.WriteAllText(part, "external = parsed");
			var main = Path.Combine(directory, "main.ycl");
			File.WriteAllText(main,
				"""
				%!include part.properties
				""");

			var nodes = YclParser.ParseFile(main, parser => parser.SetMetaStatementHandler("include", context =>
				context.ParseInclude((text, fileName) =>
				{
					var parts = text.Split(['='], 2);
					var location = new ConfigSourceLocation(fileName, 1, 1);
					return new ConfigNodeCollection(
					[
						new KeyValuePair<string?, ConfigNode>(parts[0].Trim(), new ConfigNode(parts[1].Trim(), location))
					], location);
				})));

			await Assert.That(nodes["external"].Value).IsEqualTo("parsed");
			await Assert.That(nodes["external"].Location.FileName).IsEqualTo(part);
		}
		finally
		{
			if (Directory.Exists(directory))
				Directory.Delete(directory, recursive: true);
		}
	}

	[Test]
	public async Task ParseFile_AssignsSourceFileToIncludedNodes()
	{
		var directory = Path.Combine(Path.GetTempPath(), "lexxys-ycl-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		try
		{
			var part = Path.Combine(directory, "part.ycl");
			File.WriteAllText(part,
				"""
				included yes
				""");
			var main = Path.Combine(directory, "main.ycl");
			File.WriteAllText(main,
				"""
				main true
				%!include part.ycl
				""");

			var nodes = YclParser.ParseFile(main);

			await Assert.That(nodes["main"].Location.FileName).IsEqualTo(main);
			await Assert.That(nodes["main"].Location.Line).IsEqualTo(1);
			await Assert.That(nodes["main"].Location.Column).IsEqualTo(1);
			await Assert.That(nodes["included"].Location.FileName).IsEqualTo(part);
			await Assert.That(nodes["included"].Location.Line).IsEqualTo(1);
			await Assert.That(nodes["included"].Location.Column).IsEqualTo(1);
		}
		finally
		{
			if (Directory.Exists(directory))
				Directory.Delete(directory, recursive: true);
		}
	}

	[Test]
	public async Task ParseFile_HashBangInclude_InsertsIncludedNodes()
	{
		var directory = Path.Combine(Path.GetTempPath(), "lexxys-ycl-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		try
		{
			File.WriteAllText(Path.Combine(directory, "users.ycl"),
				"""
				user
				  id 1214
				  name John Travolta
				""");
			var main = Path.Combine(directory, "main.ycl");
			File.WriteAllText(main,
				"""
				%!include users.ycl
				app
				  name demo
				%!include users.ycl
				""");

			var nodes = YclParser.ParseFile(main);

			await AssertDump(nodes,
				"""
				{
				  user = [
				    { id = "1214", name = "John Travolta" },
				    { id = "1214", name = "John Travolta" }
				  ],
				  app = { name = "demo" }
				}
				""");
		}
		finally
		{
			if (Directory.Exists(directory))
				Directory.Delete(directory, recursive: true);
		}
	}

	[Test]
	public void ParseFile_Include_RejectsRecursiveIncludes()
	{
		var directory = Path.Combine(Path.GetTempPath(), "lexxys-ycl-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		try
		{
			var main = Path.Combine(directory, "main.ycl");
			File.WriteAllText(main,
				"""
				%!include child.ycl
				""");
			File.WriteAllText(Path.Combine(directory, "child.ycl"),
				"""
				%!include main.ycl
				""");

			Assert.ThrowsExactly<SyntaxException>(() => YclParser.ParseFile(main));
		}
		finally
		{
			if (Directory.Exists(directory))
				Directory.Delete(directory, recursive: true);
		}
	}

	[Test]
	public void ParseFile_Include_RejectsPathsOutsideRoot()
	{
		var directory = Path.Combine(Path.GetTempPath(), "lexxys-ycl-" + Guid.NewGuid().ToString("N"));
		var root = Path.Combine(directory, "root");
		Directory.CreateDirectory(root);
		try
		{
			File.WriteAllText(Path.Combine(directory, "outside.ycl"), "escaped true");
			var main = Path.Combine(root, "main.ycl");
			File.WriteAllText(main,
				"""
				%!include ../outside.ycl
				""");

			Assert.ThrowsExactly<SyntaxException>(() => YclParser.ParseFile(main));
		}
		finally
		{
			if (Directory.Exists(directory))
				Directory.Delete(directory, recursive: true);
		}
	}

	[Test]
	public async Task ParseFile_Include_UsesCurrentVariables()
	{
		var directory = Path.Combine(Path.GetTempPath(), "lexxys-ycl-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		try
		{
			File.WriteAllText(Path.Combine(directory, "part.ycl"),
				"""
				public-key /users/${user}-public.key
				""");
			var main = Path.Combine(directory, "main.ycl");
			File.WriteAllText(main,
				"""
				%$user = Gerry
				%!include part.ycl
				""");

			var nodes = YclParser.ParseFile(main);

			await AssertDump(nodes,
				"""
				{
				  public-key = "/users/Gerry-public.key"
				}
				""");
		}
		finally
		{
			if (Directory.Exists(directory))
				Directory.Delete(directory, recursive: true);
		}
	}

	[Test]
	public async Task ParseFile_Include_CanAppearAfterIndentedScalarValues()
	{
		var directory = Path.Combine(Path.GetTempPath(), "lexxys-ycl-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		try
		{
			File.WriteAllText(Path.Combine(directory, "part.ycl"),
				"""
				included ${name}
				""");
			var main = Path.Combine(directory, "main.ycl");
			File.WriteAllText(main,
				"""
				root
				  name demo
				    %$name = nested
				    %!include part.ycl
				  after done
				""");

			var nodes = YclParser.ParseFile(main);

			await AssertDump(nodes,
				"""
				{
				  root = {
				    name = "demo",
				    included = "nested",
				    after = "done"
				  }
				}
				""");
		}
		finally
		{
			if (Directory.Exists(directory))
				Directory.Delete(directory, recursive: true);
		}
	}
}
