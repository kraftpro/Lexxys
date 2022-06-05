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
	private readonly string _machineName;
	private readonly string _domainName;
	private readonly int _processId;
	private readonly int _threadId;
	private readonly int _threadSysId;
	private readonly int _sequentialNumber;
	private readonly DateTime _timestamp;

	private static readonly string __staticMachineName = Tools.MachineName;
	private static readonly string __staticDomainName = Tools.DomainName;
	private static readonly int __staticProcessId = Tools.ProcessId;
	private static readonly TimeSpan __localUtcOffset = TimeSpan.FromTicks((DateTime.Now.Ticks - DateTime.UtcNow.Ticks) / (TimeSpan.TicksPerSecond * 60) * (TimeSpan.TicksPerSecond * 60));
	private static int _lastSequenceNumber;

	/// <summary>
	/// Initialize new system context object
	/// </summary>
	internal SystemContext()
	{
		_timestamp = DateTime.UtcNow;
		_sequentialNumber = Interlocked.Increment(ref _lastSequenceNumber);
		_machineName = __staticMachineName;
		_domainName = __staticDomainName;
		_processId = __staticProcessId;
		_threadId = Thread.CurrentThread.ManagedThreadId;
		_threadSysId = Tools.ThreadId;
	}

	/// <summary>
	/// Get Current Machine name
	/// </summary>
	public string MachineName => _machineName;

	/// <summary>
	/// Get .NET Domain Name (name of executable file)
	/// </summary>
	public string DomainName => _domainName;

	/// <summary>
	/// Get system process ID
	/// </summary>
	public int ProcessId => _processId;

	/// <summary>
	/// Get managed thread ID
	/// </summary>
	public int ThreadId => _threadId;

	/// <summary>
	/// Get system thread ID
	/// </summary>
	public int ThreadSysId => _threadSysId;

	/// <summary>
	/// Sequential number of the object
	/// </summary>
	public int SequentialNumber => _sequentialNumber;

	/// <summary>
	/// Local time when the object was created
	/// </summary>
	public DateTime Timestamp => _timestamp + __localUtcOffset;

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
			.Item("utcTimestamp", UtcTimestamp);
	}
}


