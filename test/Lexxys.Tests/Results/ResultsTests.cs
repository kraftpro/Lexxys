using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Lexxys.Tests.Results;

[TestClass]
public class ResultTests
{
	[TestMethod]
	public void Result_Success_ShouldBeSuccess()
	{
		// Act
		var result = Result.Success();

		// Assert
		Assert.IsTrue(result.IsSuccess);
		Assert.IsFalse(result.IsFailure);
	}

	[TestMethod]
	public void Result_Fail_ShouldCreateErrorResult()
	{
		// Act
		var error = Result.Fail("Error message", "Error title", 500, new[] { ("key1", "value1") });

		// Assert
		Assert.AreEqual("Error message", error.Message);
		Assert.AreEqual("Error title", error.Title);
		Assert.AreEqual(500, error.StatusCode);
		Assert.IsTrue(error.Data.ContainsKey("key1"));
		Assert.AreEqual("value1", error.Data["key1"]);
	}

	[TestMethod]
	public void Result_BadRequest_ShouldCreateErrorResultWithStatusCode400()
	{
		// Act
		var error = Result.BadRequest("Bad request", "Invalid input");

		// Assert
		Assert.AreEqual("Bad request", error.Message);
		Assert.AreEqual("Invalid input", error.Title);
		Assert.AreEqual(400, error.StatusCode);
	}

	[TestMethod]
	public void Result_NotFound_ShouldCreateErrorResultWithStatusCode404()
	{
		// Act
		var error = Result.NotFound("Not found", "Resource not found");

		// Assert
		Assert.AreEqual("Not found", error.Message);
		Assert.AreEqual("Resource not found", error.Title);
		Assert.AreEqual(404, error.StatusCode);
	}

	[TestMethod]
	public void Result_Success_ShouldCreateSuccessResult()
	{
		// Act
		var result = Result.Success(42);

		// Assert
		Assert.IsTrue(result.IsSuccess);
		Assert.AreEqual(42, result.Value);
	}

	[TestMethod]
	public void Result_ImplicitConversionFromErrorResult_ShouldCreateFailureResult()
	{
		// Arrange
		var error = new ErrorResult("Error message");

		// Act
		Result result = error;

		// Assert
		Assert.IsTrue(result.IsFailure);
	}

	[TestMethod]
	public void Result_TrueOperator_ShouldReturnIsSuccess()
	{
		// Arrange
		Result success = Result.Success();
		Result failure = Result.Fail("Error");

		// Act & Assert
		if (success)
		{
			// This should execute
			Assert.IsTrue(true);
		}
		else
		{
			Assert.Fail("Success result should evaluate to true");
		}

		if (failure)
		{
			Assert.Fail("Failure result should not evaluate to true");
		}
	}

	[TestMethod]
	public void Result_FalseOperator_ShouldReturnIsFailure()
	{
		// Arrange
		Result success = Result.Success();
		Result failure = Result.Fail("Error");

		// Act & Assert
		if (!success)
		{
			Assert.Fail("Success result should not evaluate to false");
		}

		if (!failure)
		{
			// This should execute
			Assert.IsTrue(true);
		}
		else
		{
			Assert.Fail("Failure result should evaluate to false");
		}
	}

	[TestMethod]
	public void Result_NotOperator_ShouldReturnIsFailure()
	{
		// Arrange
		Result success = Result.Success();
		Result failure = Result.Fail("Error");

		// Act & Assert
		Assert.IsFalse(!success);
		Assert.IsTrue(!failure);
	}

	[TestMethod]
	public void ResultGeneric_ImplicitConversionFromValue_ShouldCreateSuccessResult()
	{
		// Act
		Result<int> result = 42;

		// Assert
		Assert.IsTrue(result.IsSuccess);
		Assert.AreEqual(42, result.Value);
	}

	[TestMethod]
	public void ResultGeneric_ImplicitConversionFromErrorResult_ShouldCreateFailureResult()
	{
		// Arrange
		var error = new ErrorResult("Error message");

		// Act
		Result<int> result = error;

		// Assert
		Assert.IsTrue(result.IsFailure);
		Assert.AreEqual("Error message", result.Error.Message);
	}

	[TestMethod]
	[ExpectedException(typeof(InvalidOperationException))]
	public void ResultGeneric_AccessingValueOnFailure_ShouldThrowException()
	{
		// Arrange
		Result<int> result = new ErrorResult("Error");

		// Act - should throw
		_ = result.Value;
	}

	[TestMethod]
	[ExpectedException(typeof(InvalidOperationException))]
	public void ResultGeneric_AccessingErrorOnSuccess_ShouldThrowException()
	{
		// Arrange
		Result<int> result = 42;

		// Act - should throw
		_ = result.Error;
	}

	[TestMethod]
	public void SuccessResult_Properties_ShouldReturnExpectedValues()
	{
		// Arrange
		var result = new SuccessResult<int>(42);

		// Assert
		Assert.IsTrue(result.IsSuccess);
		Assert.IsFalse(result.IsFailure);
		Assert.AreEqual(42, result.Value);
	}

	[TestMethod]
	public void SuccessResult_ToString_ShouldFormatCorrectly()
	{
		// Arrange & Act
		var nullResult = new SuccessResult<string>(null);
		var stringResult = new SuccessResult<string>("test");
		var intResult = new SuccessResult<int>(42);
		var listResult = new SuccessResult<List<int>>(new List<int> { 1, 2, 3 });

		// Assert
		Assert.AreEqual("Success: null", nullResult.ToString());
		Assert.AreEqual("Success: test", stringResult.ToString());
		Assert.AreEqual("Success: 42", intResult.ToString());
		Assert.AreEqual("Success: [1, 2, 3]", listResult.ToString());
	}

	[TestMethod]
	public void FailureResult_Properties_ShouldReturnExpectedValues()
	{
		// Arrange
		var error = new ErrorResult("Error message", "Error title", 500);
		var result = new FailureResult<int>(error);

		// Assert
		Assert.IsFalse(result.IsSuccess);
		Assert.IsTrue(result.IsFailure);
		Assert.AreEqual(error, result.Error);
	}

	[TestMethod]
	public void FailureResult_ToString_ShouldFormatCorrectly()
	{
		// Arrange
		var error = new ErrorResult("Error message");
		var result = new FailureResult<int>(error);

		// Assert
		Assert.AreEqual($"Error: {error}", result.ToString());
	}
}