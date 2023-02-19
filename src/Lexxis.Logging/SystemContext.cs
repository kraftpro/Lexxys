// Lexxys Infrastructural library.
// file: SystemContext.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Threading;

namespace Lexxys.Logging;

public class SystemContext: IDumpJson
{
	private readonly StaticContext _context;
	private readonly int _threadId;
	private readonly int _sequentialNumber;
	private readonly DateTime _timestamp;

	private static readonly StaticContext __static = new StaticContext();
	private static volatile int _lastSequenceNumber;

	private class StaticContext
	{
		public string MachineName { get; } = Tools.MachineName;
		public string DomainName { get; } = Tools.DomainName;
		public int ProcessId { get; } = Tools.ProcessId;
		public TimeSpan LocalUtcOffset { get; } = TimeSpan.FromTicks((DateTime.Now.Ticks - DateTime.UtcNow.Ticks) / (TimeSpan.TicksPerSecond * 60) * (TimeSpan.TicksPerSecond * 60));
	}

	/// <summary>
	/// Initialize new system context object
	/// </summary>
	internal SystemContext()
	{
		_context = __static;
		_timestamp = DateTime.UtcNow;
		_sequentialNumber = Interlocked.Increment(ref _lastSequenceNumber);
		_threadId = Thread.CurrentThread.ManagedThreadId;
	}

	/// <summary>
	/// Get Current Machine name
	/// </summary>
	public string MachineName => _context.MachineName;

	/// <summary>
	/// Get .NET Domain Name (name of executable file)
	/// </summary>
	public string DomainName => _context.DomainName;

	/// <summary>
	/// Get system process ID
	/// </summary>
	public int ProcessId => _context.ProcessId;

	/// <summary>
	/// Get managed thread ID
	/// </summary>
	public int ThreadId => _threadId;

	/// <summary>
	/// Sequential number of the object
	/// </summary>
	public int SequentialNumber => _sequentialNumber;

	/// <summary>
	/// Local time when the object was created
	/// </summary>
	public DateTime Timestamp => _timestamp + _context.LocalUtcOffset;

	/// <summary>
	/// UTC time when the object was created
	/// </summary>
	public DateTime UtcTimestamp => _timestamp;

	public JsonBuilder ToJsonContent(JsonBuilder json)
	{
		return json
			.Item("machine", MachineName)
			.Item("domain", DomainName)
			.Item("processId", ProcessId)
			.Item("threadId", ThreadId)
			.Item("seqNumber", SequentialNumber)
			.Item("timestamp", UtcTimestamp);
	}
}


