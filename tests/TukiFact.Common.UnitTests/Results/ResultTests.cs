using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.UnitTests.Results;

public sealed class ResultTests
{
    [Fact]
    public void Success_WithoutValue_HasNoError()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_WithError_ExposesError()
    {
        // Arrange
        var error = Error.Validation("Command.Invalid", "The command is invalid.");

        // Act
        var act = () => Result.Failure(error);

        // Assert
        var result = act.Should().NotThrow().Subject;
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Command.Invalid");
    }

    [Fact]
    public void Value_SuccessfulResult_ReturnsValue()
    {
        // Act
        var result = Result.Success(42);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Value_FailedResult_ThrowsInvalidOperationException()
    {
        // Arrange
        var result = Result.Failure<int>(Error.NotFound("Entity.NotFound", "Not found."));

        // Act
        var act = () => result.Value;

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_SuccessCarryingError_ThrowsArgumentException()
    {
        // Act
        var ctor = () => new TestableResult(true, Error.Validation("X", "Y"));

        // Assert
        ctor.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_FailureWithoutError_ThrowsArgumentException()
    {
        // Act
        var ctor = () => new TestableResult(false, Error.None);

        // Assert
        ctor.Should().Throw<ArgumentException>();
    }

    private sealed class TestableResult : Result
    {
        public TestableResult(bool isSuccess, Error error) : base(isSuccess, error)
        {
        }
    }
}
