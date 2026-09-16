using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.Application.Messaging;

/// <summary>Command or query dispatched through <see cref="ISender"/>; the response is always a <see cref="Result"/>.</summary>
public interface IRequest<TResponse>
    where TResponse : Result;
