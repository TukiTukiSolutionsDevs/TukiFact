using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.Application.Messaging;

/// <summary>Request without output value (e.g. a command answered with 204).</summary>
public interface IRequest : IRequest<Result>;
