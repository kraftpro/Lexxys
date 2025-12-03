using System;
using System.Collections.Generic;
using System.Text;

namespace Lexxys;

internal class NeitherResult<T>: Result<T>
{
	public NeitherResult() => Error = _noResults;

	public NeitherResult(int errorCode): this(new ErrorResult("No results.", errorCode))
	{
	}

	public NeitherResult(ErrorResult error) => Error = error ?? throw new ArgumentNullException(nameof(error));

	public override ErrorResult Error { get; }

	private static readonly ErrorResult _noResults = new ErrorResult("No results.");
}
