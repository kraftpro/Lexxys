using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Lexxys.Testing;

namespace Lexxys.Tests.Testing;

[TestClass]
public class ResourcesTests
{
	[DeploymentItem("Resources.json")]
	[TestMethod]
	public void LoadResourcesTest()
	{
		var resources = Resources.GetResources("Resources.json");
		Assert.IsNotEmpty(resources);
	}

	[DeploymentItem("Resources.json")]
	[TestMethod]
	public void UsCityStateTest()
	{
		var x = Resources.AddressUsCityState;
		Assert.IsFalse(x.IsEmpty);
		Assert.AreEqual(2, x.NextValue().State.Length);
	}
}
