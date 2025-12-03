using System.Collections.Concurrent;

namespace Lexxys.Con;

public static class CallContext
{
	static ConcurrentDictionary<string, AsyncLocal<object>> _state = new ConcurrentDictionary<string, AsyncLocal<object>>();

	public static void SetData(string name, object data) =>
		_state.GetOrAdd(name, _ => new AsyncLocal<object>()).Value = data;

	public static object? GetData(string name) =>
		_state.TryGetValue(name, out AsyncLocal<object>? data) ? data.Value : null;

	public static void Go()
	{
		var test = new CallContextTests();
		test.ContextTest();
		test.WhenFlowingData_ThenCanUseContext();
	}
}

public partial class CallContextTests
{
	static AsyncLocal<object?> _context = new AsyncLocal<object?>();

	public void WhenFlowingData_ThenCanUseContext()
	{
		object? d1, t1, t10, t11, t12, t13, t14;
		object? d2, t2, t20, t21, t22, t23, t24;
		d1 = new();
		d2 = new();
		t1 = t10 = t11 = t12 = t13 = t14 = null;
		t2 = t20 = t21 = t22 = t23 = t24 = null;

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

		Thread.Sleep(10);

		Assert.That(t1).EqualTo(d1);
		Assert.That(t10).EqualTo(d1);
		Assert.That(t11).EqualTo(d1);
		Assert.That(t12).EqualTo(d1);
		Assert.That(t13).EqualTo(d1);
		Assert.That(t14).IsNull();

		Assert.That(t2).EqualTo(d2);
		Assert.That(t20).EqualTo(d2);
		Assert.That(t21).EqualTo(d2);
		Assert.That(t22).EqualTo(d2);
		Assert.That(t23).EqualTo(d2);
		Assert.That(t24).IsNull();

		Assert.That(CallContext.GetData("d1")).IsNull();
		Assert.That(CallContext.GetData("d2")).IsNull();
	}

	public void ContextTest()
	{
		object? d1, t1, t10, t11, t12, t13, t14;
		object? d2, t2, t20, t21, t22, t23, t24;
		d1 = new();
		d2 = new();
		t1 = t10 = t11 = t12 = t13 = t14 = null;
		t2 = t20 = t21 = t22 = t23 = t24 = null;

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

		Thread.Sleep(10);

		Assert.That(t1).EqualTo(d1);
		Assert.That(t10).EqualTo(d1);
		Assert.That(t11).EqualTo(d1);
		Assert.That(t12).EqualTo(d1);
		Assert.That(t13).EqualTo(d1);
		Assert.That(t14).EqualTo(d1);

		Assert.That(t2).EqualTo(d2);
		Assert.That(t20).EqualTo(d2);
		Assert.That(t21).EqualTo(d2);
		Assert.That(t22).EqualTo(d2);
		Assert.That(t23).EqualTo(d2);
		Assert.That(t24).EqualTo(d2);

		Assert.That(_context.Value).IsNull();
	}
}
