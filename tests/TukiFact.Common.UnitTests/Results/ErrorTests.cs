using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.UnitTests.Results;

public sealed class ErrorTests
{
    [Fact]
    public void Validation_WithoutFieldErrors_IsValidationErrorWithEmptyFieldErrors()
    {
        // Act
        var error = Error.Validation("Customer.InvalidRuc", "RUC must have 11 digits.");

        // Assert
        error.Type.Should().Be(ErrorType.Validation);
        error.Code.Should().Be("Customer.InvalidRuc");
        error.FieldErrors.Should().BeEmpty();
    }

    [Fact]
    public void Validation_WithFieldErrors_IsValidationErrorCarryingThem()
    {
        // Arrange
        FieldError[] fieldErrors =
        [
            new("items[0].quantity", "Quantity.MustBePositive", "Quantity must be greater than zero."),
            new("customerId", "Customer.Required", "Customer is required."),
        ];

        // Act
        var error = Error.Validation("Command.Invalid", "The command is invalid.", fieldErrors);

        // Assert
        error.FieldErrors.Should().Equal(fieldErrors);
    }

    [Fact]
    public void Validation_SourceListChangedAfterCreation_KeepsTheOriginalFieldErrors()
    {
        // Arrange
        var fieldError = new FieldError("items[0].quantity", "Quantity.MustBePositive", "Quantity must be greater than zero.");
        var source = new List<FieldError> { fieldError };
        var error = Error.Validation("Command.Invalid", "The command is invalid.", source);

        // Act
        source.Clear();

        // Assert
        error.FieldErrors.Should().Equal(fieldError);
    }

    [Fact]
    public void NotFound_Always_HasNotFoundType()
    {
        // Act & Assert
        Error.NotFound("Document.NotFound", "Document not found.").Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void Conflict_Always_HasConflictType()
    {
        // Act & Assert
        Error.Conflict("Concurrency.Conflict", "The record changed.").Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public void Unauthorized_Always_HasUnauthorizedType()
    {
        // 401/403 are normally surfaced by the host; Unauthorized exists for use cases that
        // must return that outcome as a Result.
        // Act & Assert
        Error.Unauthorized("Auth.Forbidden", "Not allowed.").Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public void Failure_Always_HasFailureType()
    {
        // Act & Assert
        Error.Failure("Unexpected", "Something went wrong.").Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public void None_Always_IsEmptyFailure()
    {
        // Act & Assert
        Error.None.Code.Should().BeEmpty();
        Error.None.Description.Should().BeEmpty();
        Error.None.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public void Equals_ValidationErrorsWithEqualFieldErrorsInDifferentLists_AreEqual()
    {
        // Arrange
        var a = Error.Validation("X", "Y", [new FieldError("f", "c", "d")]);
        var b = Error.Validation("X", "Y", [new FieldError("f", "c", "d")]);

        // Act & Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equals_ValidationErrorsWithDifferentFieldErrors_AreNotEqual()
    {
        // Arrange
        var a = Error.Validation("X", "Y", [new FieldError("f", "c", "d")]);
        var b = Error.Validation("X", "Y", [new FieldError("g", "c", "d")]);

        // Act
        var equal = a == b;

        // Assert
        equal.Should().BeFalse();
    }
}
