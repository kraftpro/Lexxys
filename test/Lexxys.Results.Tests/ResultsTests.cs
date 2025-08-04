namespace Lexxys.Results.Tests;

public class ResultTests
{
	[Test]
	public async Task Result_Success_ShouldBeSuccess()
	{
		// Act
		var result = Result.Success();

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.IsFailure).IsFalse();
	}

	[Test]
	public async Task Result_Fail_ShouldCreateErrorResult()
	{
		// Act
		var error = Result.Fail("Error message", "Error title", 500, [("key1", (string?)"value1")]);

		// Assert
		await Assert.That(error.Message).IsEqualTo("Error message");
		await Assert.That(error.Title).IsEqualTo("Error title");
		await Assert.That(error.StatusCode).IsEqualTo(500);
		await Assert.That(error.Data.ContainsKey("key1")).IsTrue();
		await Assert.That(error.Data["key1"]).IsEqualTo("value1");
	}

	[Test]
	public async Task Result_BadRequest_ShouldCreateErrorResultWithStatusCode400()
	{
		// Act
		var error = Result.BadRequest("Bad request", "Invalid input");

		// Assert
		await Assert.That(error.Message).IsEqualTo("Bad request");
		await Assert.That(error.Title).IsEqualTo("Invalid input");
		await Assert.That(error.StatusCode).IsEqualTo(400);
	}

	[Test]
	public async Task Result_NotFound_ShouldCreateErrorResultWithStatusCode404()
	{
		// Act
		var error = Result.NotFound("Not found", "Resource not found");

		// Assert
		await Assert.That(error.Message).IsEqualTo("Not found");
		await Assert.That(error.Title).IsEqualTo("Resource not found");
		await Assert.That(error.StatusCode).IsEqualTo(404);
	}

	[Test]
	public async Task Result_Success_ShouldCreateSuccessResult()
	{
		// Act
		var result = Result.Success(42);

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value).IsEqualTo(42);
	}

	[Test]
	public async Task Result_ImplicitConversionFromErrorResult_ShouldCreateFailureResult()
	{
		// Arrange
		var error = new ErrorResult("Error message");

		// Act
		Result result = error;

		// Assert
		await Assert.That(result.IsFailure).IsTrue();
	}

	[Test]
	public async Task Result_TrueOperator_ShouldReturnIsSuccess()
	{
		// Arrange
		Result success = Result.Success();
		Result failure = Result.Fail("Error");

		// Act & Assert
		if (success)
		{
			// This should execute
			var t = true;
			await Assert.That(t).IsTrue();
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

	[Test]
	public async Task Result_FalseOperator_ShouldReturnIsFailure()
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
			var t = true;
			await Assert.That(t).IsTrue();
		}
		else
		{
			Assert.Fail("Failure result should evaluate to false");
		}
	}

	[Test]
	public async Task Result_NotOperator_ShouldReturnIsFailure()
	{
		// Arrange
		Result success = Result.Success();
		Result failure = Result.Fail("Error");

		// Act & Assert
		await Assert.That(!success).IsFalse();
		await Assert.That(!failure).IsTrue();
	}

	[Test]
	public async Task ResultGeneric_ImplicitConversionFromValue_ShouldCreateSuccessResult()
	{
		// Act
		Result<int> result = 42;

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value).IsEqualTo(42);
	}

	[Test]
	public async Task ResultGeneric_ImplicitConversionFromErrorResult_ShouldCreateFailureResult()
	{
		// Arrange
		var error = new ErrorResult("Error message");

		// Act
		Result<int> result = error;

		// Assert
		await Assert.That(result.IsFailure).IsTrue();
		await Assert.That(result.Error.Message).IsEqualTo("Error message");
	}

	[Test]
	public Task ResultGeneric_AccessingValueOnFailure_ShouldThrowException()
	{
		// Arrange
		Result<int> result = new ErrorResult("Error");

		// Act - should throw
		Assert.Throws<InvalidOperationException>(() => _ = result.Value);
		return Task.CompletedTask;
	}

	[Test]
	public Task ResultGeneric_AccessingErrorOnSuccess_ShouldThrowException()
	{
		// Arrange
		Result<int> result = 42;

		// Act - should throw
		Assert.Throws<InvalidOperationException>(() => _ = result.Error);
		return Task.CompletedTask;
	}

	[Test]
	public async Task SuccessResult_Properties_ShouldReturnExpectedValues()
	{
		// Arrange
		var result = new SuccessResult<int>(42);

		// Assert
		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.IsFailure).IsFalse();
		await Assert.That(result.Value).IsEqualTo(42);
	}

	[Test]
	public async Task SuccessResult_ToString_ShouldFormatCorrectly()
	{
		// Arrange & Act
		var nullResult = new SuccessResult<string?>(null);
		var stringResult = new SuccessResult<string>("test");
		var intResult = new SuccessResult<int>(42);
		var listResult = new SuccessResult<List<int>>([1, 2, 3]);

		// Assert
		await Assert.That(nullResult.ToString()).IsEqualTo("Success: null");
		await Assert.That(stringResult.ToString()).IsEqualTo("Success: test");
		await Assert.That(intResult.ToString()).IsEqualTo("Success: 42");
		await Assert.That(listResult.ToString()).IsEqualTo("Success: [1, 2, 3]");
	}

	[Test]
	public async Task FailureResult_Properties_ShouldReturnExpectedValues()
	{
		// Arrange
		var error = new ErrorResult("Error message", "Error title", 500);
		var result = new FailureResult<int>(error);

		// Assert
		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.IsFailure).IsTrue();
		await Assert.That(result.Error).IsEqualTo(error);
	}

	[Test]
	public async Task FailureResult_ToString_ShouldFormatCorrectly()
	{
		// Arrange
		var error = new ErrorResult("Error message");
		var result = new FailureResult<int>(error);

		// Assert
		await Assert.That(result.ToString()).IsEqualTo($"Error: {error}");
	}
}