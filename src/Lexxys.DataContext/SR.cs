// Lexxys Infrastructural library.
// file: SR.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System.Text;
using System.Globalization;

namespace Lexxys.Data;

internal static class SR
{
	public static readonly CultureInfo Culture = Lexxys.SR.Culture;

	public static string ConnectionInitialized(ConnectionStringInfo connectionInfo)
		=> String.Format(Culture, "Connection initialized: {0}", connectionInfo);

	public static string ConnectionChanged(ConnectionStringInfo connectionInfo)
		=> String.Format(Culture, "Connection changed: {0}", connectionInfo);

	public static string ConnectionTiming(long timeValue)
		=> "SQL Connection Timing: " + WatchTimer.ToString(timeValue, false);

	public static string SqlQueryTiming(long timeValue, string query)
		=> "SQL Timing: " + WatchTimer.ToString(timeValue) + "\n" + Strings.CutIndents(query.Split(Nls, StringSplitOptions.RemoveEmptyEntries), 4, "\n");

	public static string SqlGroupQueryTiming(long timeValue, long timeOffset, string query)
		=> WatchTimer.ToString(timeValue) + " (+" + WatchTimer.ToString(timeOffset) + ")\n" + Strings.CutIndents(query.Split(Nls, StringSplitOptions.RemoveEmptyEntries), 4, "\n");
	private static readonly char[] Nls = ['\r', '\n'];

	public static Func<string> DC_InitConnectionString(string connection)
		=> () => String.Format(Culture, "ConnectionString: {0}", connection);

	internal static Func<string> TransactionDisposedWithCommit()
		=> () => String.Format(Culture, "Auto commit.");

	internal static Func<string> TransactionDisposedWithRollback()
		=> () => String.Format(Culture, "Auto rollback.");

	internal static string NothingToCommit()
		=> "Nothing to do on commit. Transaction is absent.";

	internal static string NothingToRollback()
		=> "Nothing to do on rollback. Transaction is absent.";

	internal static string ConnectionStringIsEmpty()
		=> "Connection String is empty";
}
