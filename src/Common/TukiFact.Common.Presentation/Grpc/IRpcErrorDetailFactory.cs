using Google.Protobuf;
using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.Presentation.Grpc;

/// <summary>
/// Seam between the kernel's <see cref="RpcErrorMapping"/> and a product-specific error detail
/// message. <see cref="TukiFact.Common.Presentation"/> must not reference
/// <c>TukiFact.Contracts.Grpc</c> — that project is the Invoicing service contract, and the kernel
/// stays product-agnostic. A module's Presentation layer implements this to pack its own
/// <c>ErrorDetail</c> message (e.g. <c>TukiFact.Contracts.V1.ErrorDetail</c>) into the
/// <c>google.rpc.Status</c> details. The kernel default (<see cref="NullRpcErrorDetailFactory"/>)
/// returns <see langword="null"/>: the status still carries a code and a message on its own.
/// </summary>
public interface IRpcErrorDetailFactory
{
    /// <summary>Returns the detail message to pack into <c>google.rpc.Status.Details</c>, or <see langword="null"/> for none.</summary>
    IMessage? Create(Error error, string? correlationId);
}
