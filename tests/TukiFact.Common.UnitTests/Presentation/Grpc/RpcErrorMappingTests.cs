using Google.Protobuf;
using Google.Rpc;
using Grpc.Core;
using TukiFact.Common.Domain.Results;
using TukiFact.Common.Presentation.Grpc;

namespace TukiFact.Common.UnitTests.Presentation.Grpc;

/// <summary>
/// <see cref="RpcErrorMapping"/> is the only translation from <see cref="Result"/>/<see cref="Error"/>
/// to a gRPC <see cref="RpcException"/>: the <c>google.rpc.Status</c> carries
/// code + message, and an optional detail message is added through the <see cref="IRpcErrorDetailFactory"/>
/// seam so the kernel never references a product-specific contract.
/// </summary>
public sealed class RpcErrorMappingTests
{
    [Theory]
    [InlineData("Validation", StatusCode.InvalidArgument)]
    [InlineData("Unauthorized", StatusCode.Unauthenticated)]
    [InlineData("Forbidden", StatusCode.PermissionDenied)]
    [InlineData("NotFound", StatusCode.NotFound)]
    [InlineData("Conflict", StatusCode.Aborted)]
    [InlineData("Failure", StatusCode.Internal)]
    public void ToRpcException_EachErrorType_MapsToTheExpectedStatusCode(string errorTypeName, StatusCode expected)
    {
        // Arrange
        var error = ErrorOfType(errorTypeName);

        // Act
        var exception = error.ToRpcException(new NullRpcErrorDetailFactory(), correlationId: null);

        // Assert
        exception.StatusCode.Should().Be(expected);
    }

    [Fact]
    public void ToRpcException_FailureType_NeverLeaksTheDescriptionAsMessage()
    {
        // A Failure's Description may name internals (mirrors ErrorProblem.FailureDetail on the HTTP side).
        // Arrange
        var error = Error.Failure("Unexpected", "NullReferenceException at Foo.Bar line 42");

        // Act
        var exception = error.ToRpcException(new NullRpcErrorDetailFactory(), correlationId: null);

        // Assert
        exception.Status.Detail.Should().NotContain("NullReferenceException");
    }

    [Fact]
    public void ToRpcException_AnyFailure_IncludesTheErrorCodeInTheMessage()
    {
        // Arrange
        var error = Error.NotFound("Document.NotFound", "Document not found.");

        // Act
        var exception = error.ToRpcException(new NullRpcErrorDetailFactory(), correlationId: null);

        // Assert
        exception.Status.Detail.Should().Be("Document not found.");
    }

    [Fact]
    public void ToRpcException_NullDetailFactory_ProducesAStatusWithNoDetails()
    {
        // Arrange
        var error = Error.NotFound("Document.NotFound", "Document not found.");

        // Act
        var exception = error.ToRpcException(new NullRpcErrorDetailFactory(), correlationId: "corr-1");

        // Assert
        var status = exception.GetRpcStatus();
        status.Should().NotBeNull();
        status!.Details.Should().BeEmpty();
    }

    [Fact]
    public void ToRpcException_FactoryReturnsAMessage_PacksItIntoTheStatusDetails()
    {
        // Arrange
        var error = Error.NotFound("Document.NotFound", "Document not found.");
        var detail = new ErrorInfo { Reason = "DOCUMENT_NOT_FOUND", Domain = "tukifact.v1" };
        var factory = new FakeRpcErrorDetailFactory(detail);

        // Act
        var exception = error.ToRpcException(factory, correlationId: "corr-1");

        // Assert
        var status = exception.GetRpcStatus();
        status.Should().NotBeNull();
        var unpacked = status!.Details.Single().Unpack<ErrorInfo>();
        unpacked.Reason.Should().Be("DOCUMENT_NOT_FOUND");
    }

    [Fact]
    public void ToRpcException_FactoryIsInvokedWithTheErrorAndTheCorrelationId()
    {
        // Arrange
        var error = Error.Conflict("Concurrency.Conflict", "The record changed.");
        var factory = new RecordingRpcErrorDetailFactory();

        // Act
        error.ToRpcException(factory, correlationId: "corr-42");

        // Assert
        factory.ReceivedError.Should().Be(error);
        factory.ReceivedCorrelationId.Should().Be("corr-42");
    }

    private static Error ErrorOfType(string errorTypeName) => errorTypeName switch
    {
        "Validation" => Error.Validation("Command.Invalid", "The command is invalid."),
        "Unauthorized" => Error.Unauthorized("Auth.Forbidden", "Not allowed."),
        "Forbidden" => Error.Forbidden("Auth.MissingPermission", "Not allowed."),
        "NotFound" => Error.NotFound("Document.NotFound", "Document not found."),
        "Conflict" => Error.Conflict("Concurrency.Conflict", "The record changed."),
        "Failure" => Error.Failure("Unexpected", "Something went wrong."),
        _ => throw new ArgumentOutOfRangeException(nameof(errorTypeName)),
    };

    private sealed class FakeRpcErrorDetailFactory(IMessage detail) : IRpcErrorDetailFactory
    {
        public IMessage? Create(Error error, string? correlationId) => detail;
    }

    private sealed class RecordingRpcErrorDetailFactory : IRpcErrorDetailFactory
    {
        public Error? ReceivedError { get; private set; }
        public string? ReceivedCorrelationId { get; private set; }

        public IMessage? Create(Error error, string? correlationId)
        {
            ReceivedError = error;
            ReceivedCorrelationId = correlationId;
            return null;
        }
    }
}
