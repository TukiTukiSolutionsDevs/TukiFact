using Microsoft.AspNetCore.Http.HttpResults;
using TukiFact.Common.Domain.Results;
using TukiFact.Common.Presentation.Results;

namespace TukiFact.Common.UnitTests.Presentation;

public sealed class ResultExtensionsTests
{
    [Fact]
    public void ToHttpResult_SuccessResult_ReturnsNoContent()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        httpResult.Should().BeOfType<NoContent>();
    }

    [Fact]
    public void ToHttpResultOfValue_SuccessResult_ReturnsOkWithValue()
    {
        // Arrange
        var result = Result.Success("value");

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        var ok = httpResult.Should().BeOfType<Ok<string>>().Subject;
        ok.Value.Should().Be("value");
    }

    [Theory]
    [InlineData("Validation", 400)]
    [InlineData("NotFound", 404)]
    [InlineData("Conflict", 409)]
    [InlineData("Unauthorized", 401)]
    [InlineData("Failure", 500)]
    public void ToHttpResult_FailureResult_MapsErrorTypeToStatusCode(string errorTypeName, int expectedStatusCode)
    {
        // Arrange
        var error = ErrorOfType(errorTypeName);
        var result = Result.Failure(error);

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        var problem = httpResult.Should().BeOfType<ProblemHttpResult>().Subject;
        problem.StatusCode.Should().Be(expectedStatusCode);
    }

    [Fact]
    public void ToHttpResult_ForbiddenType_ReturnsForbidden()
    {
        // Forbidden exists so 403 PERMISSION_DENIED can be told apart from 401 UNAUTHENTICATED; a
        // client error, so its description is safe to return as the detail (unlike a Failure).
        // Arrange
        var result = Result.Failure(Error.Forbidden("Auth.MissingPermission", "Not allowed."));

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        var problem = httpResult.Should().BeOfType<ProblemHttpResult>().Subject;
        problem.StatusCode.Should().Be(403);
        problem.ProblemDetails.Detail.Should().Be("Not allowed.");
    }

    [Fact]
    public void ToHttpResult_AnyFailure_IncludesErrorCodeExtension()
    {
        // Arrange
        var result = Result.Failure(Error.NotFound("Document.NotFound", "Document not found."));

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        var problem = httpResult.Should().BeOfType<ProblemHttpResult>().Subject;
        problem.ProblemDetails.Extensions.Should().ContainKey("code").WhoseValue.Should().Be("Document.NotFound");
    }

    [Fact]
    public void ToHttpResult_ValidationFailure_IncludesFieldErrorsExtension()
    {
        // Arrange
        FieldError[] fieldErrors = [new("customerId", "Customer.Required", "Customer is required.")];
        var result = Result.Failure(Error.Validation("Command.Invalid", "The command is invalid.", fieldErrors));

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        var problem = httpResult.Should().BeOfType<ProblemHttpResult>().Subject;
        var errors = problem.ProblemDetails.Extensions["errors"].Should().BeAssignableTo<IEnumerable<object>>().Subject;
        errors.Should().HaveCount(1);
    }

    [Fact]
    public void ToHttpResult_NonValidationFailure_HasNoErrorsExtension()
    {
        // Arrange
        var result = Result.Failure(Error.NotFound("Document.NotFound", "Document not found."));

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        var problem = httpResult.Should().BeOfType<ProblemHttpResult>().Subject;
        problem.ProblemDetails.Extensions.Should().NotContainKey("errors");
    }

    [Fact]
    public void ToHttpResult_FailureType_NeverLeaksTheDescriptionAsDetail()
    {
        // A Failure's Description may name internals (errors-and-results.md); only the code leaves the API.
        // Arrange
        var result = Result.Failure(Error.Failure("Unexpected", "NullReferenceException at Foo.Bar line 42"));

        // Act
        var httpResult = result.ToHttpResult();

        // Assert
        var problem = httpResult.Should().BeOfType<ProblemHttpResult>().Subject;
        problem.ProblemDetails.Detail.Should().NotContain("NullReferenceException");
    }

    private static Error ErrorOfType(string errorTypeName) => errorTypeName switch
    {
        "Validation" => Error.Validation("Command.Invalid", "The command is invalid."),
        "NotFound" => Error.NotFound("Document.NotFound", "Document not found."),
        "Conflict" => Error.Conflict("Concurrency.Conflict", "The record changed."),
        "Unauthorized" => Error.Unauthorized("Auth.Forbidden", "Not allowed."),
        "Failure" => Error.Failure("Unexpected", "Something went wrong."),
        _ => throw new ArgumentOutOfRangeException(nameof(errorTypeName)),
    };
}
