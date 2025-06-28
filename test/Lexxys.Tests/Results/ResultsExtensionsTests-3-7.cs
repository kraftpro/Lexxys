using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lexxys.Tests.Results;

[TestClass]
public class ResultExtensionsTests_3_7
{
	#region Then Tests (Same Type)

	[TestMethod]
	public void Then_WithSyncFunc_WhenSuccess_CallsFunction()
	{
		// Arrange
		var result = new SuccessResult<int>(10);
		var called = false;

		// Act
		var nextResult = result.Then(value => {
			called = true;
			return new SuccessResult<int>(value * 2);
		});

		// Assert
		Assert.IsTrue(called);
		Assert.IsTrue(nextResult.IsSuccess);
		Assert.AreEqual(20, nextResult.Value);
	}

	[TestMethod]
	public void Then_WithSyncFunc_WhenFailure_DoesNotCallFunction()
	{
		// Arrange
		var error = new ErrorResult("Test error");
		var result = new FailureResult<int>(error);
		var called = false;

		// Act
		var nextResult = result.Then<int, int>(value => {
			called = true;
			return new ErrorResult("Inside error");
		});

		// Assert
		Assert.IsFalse(called);
		Assert.IsTrue(nextResult.IsFailure);
		Assert.AreEqual(error, nextResult.Error);
	}

	[TestMethod]
	public async Task Then_WithAsyncFunc_WhenSuccess_CallsFunction()
	{
		// Arrange
		var result = new SuccessResult<int>(10);
		var called = false;

		// Act
		var nextResult = await result.Then(async value => {
			called = true;
			await Task.Delay(1); // Simulate async work
			return Result.Success(value * 2);
		});

		// Assert
		Assert.IsTrue(called);
		Assert.IsTrue(nextResult.IsSuccess);
		Assert.AreEqual(20, nextResult.Value);
	}

	[TestMethod]
	public async Task Then_WithAsyncFunc_WhenFailure_DoesNotCallFunction()
	{
		// Arrange
		var error = new ErrorResult("Test error");
		var result = new FailureResult<int>(error);
		var called = false;

		// Act
		var nextResult = await result.Then(async value => {
			called = true;
			await Task.Delay(1); // Simulate async work
			return Result.Success(value * 2);
		});

		// Assert
		Assert.IsFalse(called);
		Assert.IsTrue(nextResult.IsFailure);
		Assert.AreEqual(error, nextResult.Error);
	}

	[TestMethod]
	public async Task Then_WithTaskResult_AndSyncFunc_WhenSuccess_CallsFunction()
	{
		// Arrange
		var result = Task.FromResult<Result<int>>(new SuccessResult<int>(10));
		var called = false;

		// Act
		var nextResult = await result.Then(value => {
			called = true;
			return new SuccessResult<int>(value * 2);
		});

		// Assert
		Assert.IsTrue(called);
		Assert.IsTrue(nextResult.IsSuccess);
		Assert.AreEqual(20, nextResult.Value);
	}

	[TestMethod]
	public async Task Then_WithTaskResult_AndAsyncFunc_WhenSuccess_CallsFunction()
	{
		// Arrange
		var result = Task.FromResult<Result<int>>(new SuccessResult<int>(10));
		var called = false;

		// Act
		var nextResult = await result.Then(async value => {
			called = true;
			await Task.Delay(1); // Simulate async work
			return Result.Success(value * 2);
		});

		// Assert
		Assert.IsTrue(called);
		Assert.IsTrue(nextResult.IsSuccess);
		Assert.AreEqual(20, nextResult.Value);
	}

	#endregion

	#region Then Tests (Different Types)

	[TestMethod]
	public void Then_WithSyncFunc_DifferentTypes_WhenSuccess_CallsFunction()
	{
		// Arrange
		var result = new SuccessResult<int>(10);
		var called = false;

		// Act
		var nextResult = result.Then<int, string>(value => {
			called = true;
			return new SuccessResult<string>(value.ToString());
		});

		// Assert
		Assert.IsTrue(called);
		Assert.IsTrue(nextResult.IsSuccess);
		Assert.AreEqual("10", nextResult.Value);
	}

	[TestMethod]
	public void Then_WithSyncFunc_DifferentTypes_WhenFailure_DoesNotCallFunction()
	{
		// Arrange
		var error = new ErrorResult("Test error");
		var result = new FailureResult<int>(error);
		var called = false;

		// Act
		var nextResult = result.Then<int, string>(value => {
			called = true;
			return new SuccessResult<string>(value.ToString());
		});

		// Assert
		Assert.IsFalse(called);
		Assert.IsTrue(nextResult.IsFailure);
		Assert.AreEqual(error, nextResult.Error);
	}

	[TestMethod]
	public async Task Then_WithAsyncFunc_DifferentTypes_WhenSuccess_CallsFunction()
	{
		// Arrange
		var result = new SuccessResult<int>(10);
		var called = false;

		// Act
		var nextResult = await result.Then<int, string>(async value => {
			called = true;
			await Task.Delay(1); // Simulate async work
			return new SuccessResult<string>(value.ToString());
		});

		// Assert
		Assert.IsTrue(called);
		Assert.IsTrue(nextResult.IsSuccess);
		Assert.AreEqual("10", nextResult.Value);
	}

	[TestMethod]
	public async Task Then_WithTaskResult_DifferentTypes_WhenSuccess_CallsFunction()
	{
		// Arrange
		var result = Task.FromResult<Result<int>>(new SuccessResult<int>(10));
		var called = false;

		// Act
		var nextResult = await result.Then<int, string>(value => {
			called = true;
			return new SuccessResult<string>(value.ToString());
		});

		// Assert
		Assert.IsTrue(called);
		Assert.IsTrue(nextResult.IsSuccess);
		Assert.AreEqual("10", nextResult.Value);
	}

	[TestMethod]
	public async Task Then_WithTaskResult_AndAsyncFunc_DifferentTypes_WhenSuccess_CallsFunction()
	{
		// Arrange
		var result = Task.FromResult<Result<int>>(new SuccessResult<int>(10));
		var called = false;

		// Act
		var nextResult = await result.Then<int, string>(async value => {
			called = true;
			await Task.Delay(1); // Simulate async work
			return new SuccessResult<string>(value.ToString());
		});

		// Assert
		Assert.IsTrue(called);
		Assert.IsTrue(nextResult.IsSuccess);
		Assert.AreEqual("10", nextResult.Value);
	}

	#endregion

	#region Otherwise Tests

	[TestMethod]
	public void Otherwise_WhenSuccess_DoesNotCallFunction()
	{
		// Arrange
		var result = new SuccessResult<int>(10);
		var called = false;

		// Act
		var nextResult = result.Otherwise(error => {
			called = true;
			return new SuccessResult<int>(99);
		});

		// Assert
		Assert.IsFalse(called);
		Assert.IsTrue(nextResult.IsSuccess);
		Assert.AreEqual(10, nextResult.Value);
	}

	[TestMethod]
	public void Otherwise_WhenFailure_CallsFunction()
	{
		// Arrange
		var error = new ErrorResult("Test error");
		var result = new FailureResult<int>(error);
		var called = false;

		// Act
		var nextResult = result.Otherwise(err => {
			called = true;
			Assert.AreEqual(error, err);
			return new SuccessResult<int>(99);
		});

		// Assert
		Assert.IsTrue(called);
		Assert.IsTrue(nextResult.IsSuccess);
		Assert.AreEqual(99, nextResult.Value);
	}

	[TestMethod]
	public async Task Otherwise_WithAsyncFunc_WhenFailure_CallsFunction()
	{
		// Arrange
		var error = new ErrorResult("Test error");
		var result = new FailureResult<int>(error);
		var called = false;

		// Act
		var nextResult = await result.Otherwise(async err => {
			called = true;
			Assert.AreEqual(error, err);
			await Task.Delay(1); // Simulate async work
			return new SuccessResult<int>(99);
		});

		// Assert
		Assert.IsTrue(called);
		Assert.IsTrue(nextResult.IsSuccess);
		Assert.AreEqual(99, nextResult.Value);
	}

	#endregion

	#region Cast Tests

	[TestMethod]
	public void Cast_WhenSuccess_TransformsValue()
	{
		// Arrange
		var result = new SuccessResult<int>(10);

		// Act
		var casted = result.Cast<int, string>(x => x.ToString());

		// Assert
		Assert.IsTrue(casted.IsSuccess);
		Assert.AreEqual("10", casted.Value);
	}

	[TestMethod]
	public void Cast_WhenFailure_PropagatesError()
	{
		// Arrange
		var error = new ErrorResult("Test error");
		var result = new FailureResult<int>(error);

		// Act
		var casted = result.Cast<int, string>(x => x.ToString());

		// Assert
		Assert.IsTrue(casted.IsFailure);
		Assert.AreEqual(error, casted.Error);
	}

	[TestMethod]
	public async Task Cast_WithTaskResult_WhenSuccess_TransformsValue()
	{
		// Arrange
		var result = Task.FromResult<Result<int>>(new SuccessResult<int>(10));

		// Act
		var casted = await result.Cast<int, string>(x => x.ToString());

		// Assert
		Assert.IsTrue(casted.IsSuccess);
		Assert.AreEqual("10", casted.Value);
	}

	#endregion

	#region Assert Tests

	[TestMethod]
	public void Assert_WhenSuccess_AndValidValue_ReturnsOriginalResult()
	{
		// Arrange
		var result = new SuccessResult<int>(10);

		// Act
		var asserted = result.Assert(x => x > 0 ? null: new ErrorResult("Value should be positive", [], null, 0));

		// Assert
		Assert.IsTrue(asserted.IsSuccess);
		Assert.AreEqual(10, asserted.Value);
	}

	[TestMethod]
	public void Assert_WhenSuccess_AndInvalidValue_ReturnsError()
	{
		// Arrange
		var result = new SuccessResult<int>(-5);

		// Act
		var asserted = result.Assert(x => x > 0 ? null: new ErrorResult("Value should be positive", [], null, 0));

		// Assert
		Assert.IsTrue(asserted.IsFailure);
		Assert.AreEqual("Value should be positive", asserted.Error.Message);
	}

	[TestMethod]
	public void Assert_WhenFailure_ReturnsOriginalError()
	{
		// Arrange
		var error = new ErrorResult("Original error", [], null, 0);
		var result = new FailureResult<int>(error);

		// Act
		var asserted = result.Assert(x => x > 0 ? null: new ErrorResult("Value should be positive", [], null, 0));

		// Assert
		Assert.IsTrue(asserted.IsFailure);
		Assert.AreEqual(error, asserted.Error);
	}

	[TestMethod]
	public void Assert_WithErrorFunc_WhenSuccess_AndValidValue_ReturnsOriginalResult()
	{
		// Arrange
		var result = new SuccessResult<int>(10);

		// Act
		var asserted = result.Assert(x => x > 0, () => new ErrorResult("Value should be positive", [], null, 0));

		// Assert
		Assert.IsTrue(asserted.IsSuccess);
		Assert.AreEqual(10, asserted.Value);
	}

	[TestMethod]
	public void Assert_WithErrorFunc_WhenSuccess_AndInvalidValue_ReturnsError()
	{
		// Arrange
		var result = new SuccessResult<int>(-5);

		// Act
		var asserted = result.Assert(x => x > 0, () => new ErrorResult("Value should be positive", [], null, 0));

		// Assert
		Assert.IsTrue(asserted.IsFailure);
		Assert.AreEqual("Value should be positive", asserted.Error.Message);
	}

	[TestMethod]
	public void Assert_WithErrorFunc_WhenFailure_ReturnsOriginalError()
	{
		// Arrange
		var error = new ErrorResult("Original error", [], null, 0);
		var result = new FailureResult<int>(error);

		// Act
		var asserted = result.Assert(x => x > 0, () => new ErrorResult("Value should be positive", [], null, 0));

		// Assert
		Assert.IsTrue(asserted.IsFailure);
		Assert.AreEqual(error, asserted.Error);
	}

	[TestMethod]
	public void Assert_WithErrorMessage_WhenSuccess_AndValidValue_ReturnsOriginalResult()
	{
		// Arrange
		var result = new SuccessResult<string>("Valid");

		// Act
		var asserted = result.Assert(x => x != null, "Value should not be null");

		// Assert
		Assert.IsTrue(asserted.IsSuccess);
		Assert.AreEqual("Valid", asserted.Value);
	}

	[TestMethod]
	public void Assert_WithErrorMessage_WhenSuccess_AndInvalidValue_ReturnsError()
	{
		// Arrange
		var result = new SuccessResult<string>("");

		// Act
		var asserted = result.Assert(x => !string.IsNullOrEmpty(x), "Value should not be empty", "Validation Error", 400, ("field", "value"));

		// Assert
		Assert.IsTrue(asserted.IsFailure);
		Assert.AreEqual("Value should not be empty", asserted.Error.Message);
		Assert.AreEqual("Validation Error", asserted.Error.Title);
		Assert.AreEqual(400, asserted.Error.StatusCode);
		Assert.AreEqual("value", asserted.Error.Data["field"]);
	}

	[TestMethod]
	public void Assert_WithErrorMessage_WhenFailure_ReturnsOriginalError()
	{
		// Arrange
		var error = new ErrorResult("Original error", [], null, 0);
		var result = new FailureResult<string>(error);

		// Act
		var asserted = result.Assert(x => !string.IsNullOrEmpty(x), "Value should not be empty");

		// Assert
		Assert.IsTrue(asserted.IsFailure);
		Assert.AreEqual(error, asserted.Error);
	}

	#endregion

	#region Complex Chaining Tests

	[TestMethod]
	public async Task ComplexChaining_Success()
	{
		// Arrange
		var result = new SuccessResult<int>(5);

		// Act
		var finalResult = await result
			.Then(x => new SuccessResult<int>(x * 2))
			.Then(async x => {
				await Task.Delay(1);
				return Result.Success(x + 1);
			})
			.Cast<int, string>(x => x.ToString())
			.Assert(x => x.Length > 0, "Value should not be empty");

		// Assert
		Assert.IsTrue(finalResult.IsSuccess);
		Assert.AreEqual("11", finalResult.Value);
	}

	[TestMethod]
	public async Task ComplexChaining_FailsInMiddle()
	{
		// Arrange
		var result = new SuccessResult<int>(5);

		// Act
		var finalResult = await result
			.Then(x => new SuccessResult<int>(x * 2))
			.Assert(x => x > 15 ? null: new ErrorResult("Value too small", [], null, 0))
			.Then(async x => {
				await Task.Delay(1);
				return Result.Success(x + 1);
			})
			.Otherwise(err => {
				Assert.AreEqual("Value too small", err.Message);
				return new SuccessResult<int>(100);
			});

		// Assert
		Assert.IsTrue(finalResult.IsSuccess);
		Assert.AreEqual(100, finalResult.Value);
	}

	#endregion
}