using System.Collections.Concurrent;

namespace Lexxys.Argument.Tests;

public static class CallContext
{
	static ConcurrentDictionary<string, AsyncLocal<object>> state = new ConcurrentDictionary<string, AsyncLocal<object>>();

	public static void SetData(string name, object data) =>
		state.GetOrAdd(name, _ => new AsyncLocal<object>()).Value = data;

	public static object? GetData(string name) =>
		state.TryGetValue(name, out AsyncLocal<object>? data) ? data.Value: null;
}

public class CallContextTests
{
	static AsyncLocal<object?> _context = new AsyncLocal<object?>();

	[Fact]
	public void WhenFlowingData_ThenCanUseContext()
	{
		object? d1, t1, t10, t11, t12, t13, t14;
		object? d2, t2, t20, t21, t22, t23, t24;
		d1 = new();
		d2 = new();
		t1 = t10 = t11 = t12 = t13 = t14 = null;
		t2 = t20 = t21 = t22 = t23 = t24 = null;

#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
		Task.WaitAll(
			Task.Run(() =>
			{
				CallContext.SetData("d1", d1);
				new Thread(() => t10 = CallContext.GetData("d1")).Start();
				Task.WaitAll(
					Task.Run(() => { return t1 = CallContext.GetData("d1"); })
						.ContinueWith(t => Task.Run(() => t11 = CallContext.GetData("d1") ?? 1)),
					Task.Run(() => t12 = CallContext.GetData("d1")),
					Task.Run(() => t13 = CallContext.GetData("d1")),
					Task.Run(() => t14 = CallContext.GetData("d2"))
				);
			}),
			Task.Run(() =>
			{
				CallContext.SetData("d2", d2);
				new Thread(() => t20 = CallContext.GetData("d2")).Start();
				Task.WaitAll(
					Task.Run(() => t2 = CallContext.GetData("d2"))
						.ContinueWith(t => Task.Run(() => t21 = CallContext.GetData("d2") ?? 2)),
					Task.Run(() => t22 = CallContext.GetData("d2")),
					Task.Run(() => t23 = CallContext.GetData("d2")),
					Task.Run(() => t24 = CallContext.GetData("d1"))
				);
			})
		);
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method

		Thread.Sleep(10);

		Assert.Equal(d1, t1);
		Assert.Equal(d1, t10);
		Assert.Equal(d1, t11);
		Assert.Equal(d1, t12);
		Assert.Equal(d1, t13);
		Assert.Null(t14);

		Assert.Equal(d2, t2);
		Assert.Equal(d2, t20);
		Assert.Equal(d2, t21);
		Assert.Equal(d2, t22);
		Assert.Equal(d2, t23);
		Assert.Null(t24);

		Assert.Null(CallContext.GetData("d1"));
		Assert.Null(CallContext.GetData("d2"));
	}

	[Fact]
	public void ContextTest()
	{
		object? d1, t1, t10, t11, t12, t13, t14;
		object? d2, t2, t20, t21, t22, t23, t24;
		d1 = new();
		d2 = new();
		t1 = t10 = t11 = t12 = t13 = t14 = null;
		t2 = t20 = t21 = t22 = t23 = t24 = null;

#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
		Task.WaitAll(
			Task.Run(() =>
			{
				_context.Value = d1;
				new Thread(() => t10 = _context.Value).Start();
				Task.WaitAll(
					Task.Run(() => { return t1 = _context.Value; })
						.ContinueWith(t => Task.Run(() => t11 = _context.Value ?? 1)),
					Task.Run(() => t12 = _context.Value),
					Task.Run(() => t13 = _context.Value),
					Task.Run(() => t14 = _context.Value)
				);
			}),
			Task.Run(() =>
			{
				_context.Value = d2;
				new Thread(() => t20 = _context.Value).Start();
				Task.WaitAll(
					Task.Run(() => t2 = _context.Value)
						.ContinueWith(t => Task.Run(() => t21 = _context.Value ?? 2)),
					Task.Run(() => t22 = _context.Value),
					Task.Run(() => t23 = _context.Value),
					Task.Run(() => t24 = _context.Value)
				);
			})
		);
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method

		Thread.Sleep(10);

		Assert.Equal(d1, t1);
		Assert.Equal(d1, t10);
		Assert.Equal(d1, t11);
		Assert.Equal(d1, t12);
		Assert.Equal(d1, t13);
		Assert.Equal(d1, t14);

		Assert.Equal(d2, t2);
		Assert.Equal(d2, t20);
		Assert.Equal(d2, t21);
		Assert.Equal(d2, t22);
		Assert.Equal(d2, t23);
		Assert.Equal(d2, t24);

		Assert.Null(_context.Value);
	}
}
