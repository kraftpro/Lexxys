namespace Lexxys.Results.Tests;

public class ResultValueTests
{
	[Test]
	public async Task ResultValue_ValueConstructor_ShouldCreateSuccessfulResult()
	{
		var result = new ResultValue<int>(42);

		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.IsFailure).IsFalse();
		await Assert.That(result.Value).IsEqualTo(42);
		await Assert.That(result.Error).IsNull();
	}

	[Test]
	public async Task ResultValue_ErrorConstructor_ShouldCreateFailureResult()
	{
		var error = new ErrorResult("Error message");
		var result = new ResultValue<int>(error);

		await Assert.That(result.IsSuccess).IsFalse();
		await Assert.That(result.IsFailure).IsTrue();
		await Assert.That(result.Error).IsSameReferenceAs(error);
		await Assert.That(result.Value).IsEqualTo(default(int));
	}

	[Test]
	public Task ResultValue_ErrorConstructor_WithNull_ShouldThrowArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(() => new ResultValue<int>(null!));
		return Task.CompletedTask;
	}

	[Test]
	public async Task ResultValue_ImplicitConversionFromValue_ShouldCreateSuccessfulResult()
	{
		ResultValue<string> result = "value";

		await Assert.That(result.IsSuccess).IsTrue();
		await Assert.That(result.Value).IsEqualTo("value");
		await Assert.That(result.Error).IsNull();
	}

	[Test]
	public async Task ResultValue_ImplicitConversionFromError_ShouldCreateFailureResult()
	{
		var error = new ErrorResult("Failure");
		ResultValue<int> result = error;

		await Assert.That(result.IsFailure).IsTrue();
		await Assert.That(result.Error).IsSameReferenceAs(error);
	}

	[Test]
	public async Task ResultValue_ImplicitConversionToValue_OnSuccess_ShouldReturnWrappedValue()
	{
		ResultValue<int> result = 42;

		int value = result;

		await Assert.That(value).IsEqualTo(42);
	}

	[Test]
	public Task ResultValue_ImplicitConversionToValue_OnFailure_ShouldThrowInvalidOperationException()
	{
		ResultValue<int> result = new ErrorResult("Failure");

		Assert.Throws<InvalidOperationException>(() => _ = (int)result);
		return Task.CompletedTask;
	}

	[Test]
	public async Task ResultValue_TrueOperator_ShouldFollowSuccessState()
	{
		ResultValue<int> success = 42;
		ResultValue<int> failure = new ErrorResult("Failure");

		var successBranch = false;
		var failureBranch = false;

		if (success)
		{
			successBranch = true;
		}

		if (failure)
		{
			failureBranch = true;
		}

		await Assert.That(successBranch).IsTrue();
		await Assert.That(failureBranch).IsFalse();
	}

	[Test]
	public async Task ResultValue_FalseAndNotOperators_ShouldFollowFailureState()
	{
		ResultValue<int> success = 42;
		ResultValue<int> failure = new ErrorResult("Failure");

		var successEvaluatedAsFalse = false;
		var failureEvaluatedAsFalse = false;

		if (!success)
		{
			successEvaluatedAsFalse = true;
		}

		if (!failure)
		{
			failureEvaluatedAsFalse = true;
		}

		await Assert.That(successEvaluatedAsFalse).IsFalse();
		await Assert.That(failureEvaluatedAsFalse).IsTrue();
		await Assert.That(!success).IsFalse();
		await Assert.That(!failure).IsTrue();
	}
}
