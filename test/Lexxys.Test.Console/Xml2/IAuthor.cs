using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace FoundationSource.Admin
{
	public interface IApplicationAuthor: IPrincipal
	{
		int UserId { get; }
		string LoginName { get; }
		string SessionId { get; }
		bool IsAuthenticated { get; }
		void LogOff();
		bool Request(string resource);
		bool Request(string resource, int foundationId);
		//FoundationsSqlFilter GetFoundations(string resource = null);
		Action PageVisited(string page, string action, bool error, TimeSpan? checkTime, TimeSpan? beginTime, TimeSpan? endTime, TimeSpan? closedTime, TimeSpan? dbTime, TimeSpan? queryTime);
	}

	public interface IAuthor: IApplicationAuthor
	{
		int FoundationId { get; }
		void SelectFoundation(int foundationId, bool permanent = false);
	}
}
