using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexxys.Tests.Results;

[TestClass]
public class ResultExtensionsTests_4_1
{
	[TestMethod]
	public void Then_Sync_Success_CallsFunc()
	{
		var result = new SuccessResult<int>(5);
		var chained = result.Then(x => new SuccessResult<int>(x + 1));
		Assert.IsTrue(chained.IsSuccess);
		Assert.AreEqual(6, chained.Value);
	}

	[TestMethod]
	public void Then_Sync_Failure_SkipsFunc()
	{
		var error = new ErrorResult("fail");
		var result = new FailureResult<int>(error);
		var chained = result.Then(x => new SuccessResult<int>(x + 1));
		Assert.IsTrue(chained.IsFailure);
		Assert.AreEqual(error, chained.Error);
	}

	[TestMethod]
	public async Task Then_Async_Success_CallsFunc()
	{
		var result = new SuccessResult<int>(5);
		var chained = await result.Then(x => Task.FromResult(Result.Success(x + 2)));
		Assert.IsTrue(chained.IsSuccess);
		Assert.AreEqual(7, chained.Value);
	}

	[TestMethod]
	public async Task Then_Async_Failure_SkipsFunc()
	{
		var error = new ErrorResult("fail");
		var result = new FailureResult<int>(error);
		var chained = await result.Then(async x => await Task.FromResult(Result.Success(x + 2)));
		Assert.IsTrue(chained.IsFailure);
		Assert.AreEqual(error, chained.Error);
	}

	[TestMethod]
	public void Then_Sync_DifferentTypes_Success()
	{
		var result = new SuccessResult<int>(3);
		var chained = result.Then(x => new SuccessResult<string>((x * 2).ToString()));
		Assert.IsTrue(chained.IsSuccess);
		Assert.AreEqual("6", chained.Value);
	}

	[TestMethod]
	public void Then_Sync_DifferentTypes_Failure()
	{
		var error = new ErrorResult("fail");
		var result = new FailureResult<int>(error);
		var chained = result.Then(x => new SuccessResult<string>(x.ToString()));
		Assert.IsTrue(chained.IsFailure);
		Assert.AreEqual(error, chained.Error);
	}

	[TestMethod]
	public async Task Then_Async_DifferentTypes_Success()
	{
		var result = new SuccessResult<int>(4);
		var chained = await result.Then<int, string>(async x => await Task.FromResult(new SuccessResult<string>((x + 1).ToString())));
		Assert.IsTrue(chained.IsSuccess);
		Assert.AreEqual("5", chained.Value);
	}

	[TestMethod]
	public async Task Then_Async_DifferentTypes_Failure()
	{
		var error = new ErrorResult("fail");
		var result = new FailureResult<int>(error);
		var chained = await result.Then<int, string>(async x => await Task.FromResult(new SuccessResult<string>(x.ToString())));
		Assert.IsTrue(chained.IsFailure);
		Assert.AreEqual(error, chained.Error);
	}

	[TestMethod]
	public async Task Then_TaskResult_Success()
	{
		var result = Task.FromResult<Result<int>>(new SuccessResult<int>(10));
		var chained = await result.Then(x => new SuccessResult<int>(x * 2));
		Assert.IsTrue(chained.IsSuccess);
		Assert.AreEqual(20, chained.Value);
	}

	[TestMethod]
	public async Task Then_TaskResult_Failure()
	{
		var error = new ErrorResult("fail");
		var result = Task.FromResult<Result<int>>(new FailureResult<int>(error));
		var chained = await result.Then(x => new SuccessResult<int>(x * 2));
		Assert.IsTrue(chained.IsFailure);
		Assert.AreEqual(error, chained.Error);
	}

	[TestMethod]
	public void Otherwise_Success_SkipsFunc()
	{
		var result = new SuccessResult<int>(1);
		var chained = result.Otherwise(e => new SuccessResult<int>(99));
		Assert.IsTrue(chained.IsSuccess);
		Assert.AreEqual(1, chained.Value);
	}

	[TestMethod]
	public void Otherwise_Failure_CallsFunc()
	{
		var error = new ErrorResult("fail");
		var result = new FailureResult<int>(error);
		var chained = result.Otherwise(e => new SuccessResult<int>(99));
		Assert.IsTrue(chained.IsSuccess);
		Assert.AreEqual(99, chained.Value);
	}

	[TestMethod]
	public async Task Otherwise_Async_Success_SkipsFunc()
	{
		var result = new SuccessResult<int>(1);
		var chained = await result.Otherwise(async e => await Task.FromResult(new SuccessResult<int>(99)));
		Assert.IsTrue(chained.IsSuccess);
		Assert.AreEqual(1, chained.Value);
	}

	[TestMethod]
	public async Task Otherwise_Async_Failure_CallsFunc()
	{
		var error = new ErrorResult("fail");
		var result = new FailureResult<int>(error);
		var chained = await result.Otherwise(async e => await Task.FromResult(new SuccessResult<int>(99)));
		Assert.IsTrue(chained.IsSuccess);
		Assert.AreEqual(99, chained.Value);
	}

	[TestMethod]
	public void Cast_Success()
	{
		var result = new SuccessResult<int>(7);
		var casted = result.Cast(x => (x * 3).ToString());
		Assert.IsTrue(casted.IsSuccess);
		Assert.AreEqual("21", casted.Value);
	}

	[TestMethod]
	public void Cast_Failure()
	{
		var error = new ErrorResult("fail");
		var result = new FailureResult<int>(error);
		var casted = result.Cast(x => x.ToString());
		Assert.IsTrue(casted.IsFailure);
		Assert.AreEqual(error, casted.Error);
	}

	[TestMethod]
	public async Task Cast_Async_Success()
	{
		var result = Task.FromResult<Result<int>>(new SuccessResult<int>(8));
		var casted = await result.Cast(x => (x + 2).ToString());
		Assert.IsTrue(casted.IsSuccess);
		Assert.AreEqual("10", casted.Value);
	}

	[TestMethod]
	public void Assert_Success_Valid()
	{
		var result = new SuccessResult<int>(5);
		var asserted = result.Assert(x => x > 0 ? null : new ErrorResult("negative"));
		Assert.IsTrue(asserted.IsSuccess);
		Assert.AreEqual(5, asserted.Value);
	}

	[TestMethod]
	public void Assert_Success_Invalid()
	{
		var result = new SuccessResult<int>(-1);
		var asserted = result.Assert(x => x > 0 ? null : new ErrorResult("negative"));
		Assert.IsTrue(asserted.IsFailure);
		Assert.AreEqual("negative", asserted.Error.Message);
	}

	[TestMethod]
	public void Assert_Failure()
	{
		var error = new ErrorResult("fail");
		var result = new FailureResult<int>(error);
		var asserted = result.Assert(x => new ErrorResult("should not be called"));
		Assert.IsTrue(asserted.IsFailure);
		Assert.AreEqual(error, asserted.Error);

		asserted = result.Assert(x => true, () => new ErrorResult("should not be called"));
		Assert.IsTrue(asserted.IsFailure);
		Assert.AreEqual(error, asserted.Error);
	}

	[TestMethod]
	public void Assert_Success_PredicateTrue()
	{
		var result = new SuccessResult<int>(10);
		var asserted = result.Assert(x => x > 0, () => new ErrorResult("fail"));
		Assert.IsTrue(asserted.IsSuccess);
		Assert.AreEqual(10, asserted.Value);
	}

	[TestMethod]
	public void Assert_Success_PredicateFalse()
	{
		var result = new SuccessResult<int>(-10);
		var asserted = result.Assert(x => x > 0, () => new ErrorResult("fail"));
		Assert.IsTrue(asserted.IsFailure);
		Assert.AreEqual("fail", asserted.Error.Message);
	}

	[TestMethod]
	public void Assert_WithMessage_Success()
	{
		var result = new SuccessResult<int>(5);
		var asserted = result.Assert(x => x > 0, "fail");
		Assert.IsTrue(asserted.IsSuccess);
		Assert.AreEqual(5, asserted.Value);
	}

	[TestMethod]
	public void Assert_WithMessage_Failure()
	{
		var result = new SuccessResult<int>(-5);
		var asserted = result.Assert(x => x > 0, "fail", "title", 123, ("k", "v"));
		Assert.IsTrue(asserted.IsFailure);
		Assert.AreEqual("fail", asserted.Error.Message);
		Assert.AreEqual("title", asserted.Error.Title);
		Assert.AreEqual(123, asserted.Error.StatusCode);
		Assert.AreEqual("v", asserted.Error.Data["k"]);
	}
}