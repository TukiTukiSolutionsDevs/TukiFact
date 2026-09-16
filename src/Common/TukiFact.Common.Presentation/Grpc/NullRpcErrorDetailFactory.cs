using Google.Protobuf;
using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.Presentation.Grpc;

/// <summary>Kernel default for <see cref="IRpcErrorDetailFactory"/>: no product contract, no detail message.</summary>
public sealed class NullRpcErrorDetailFactory : IRpcErrorDetailFactory
{
    public IMessage? Create(Error error, string? correlationId) => null;
}
