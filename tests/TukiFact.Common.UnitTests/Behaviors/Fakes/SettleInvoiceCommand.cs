using TukiFact.Common.Application.Messaging;

namespace TukiFact.Common.UnitTests.Behaviors.Fakes;

/// <summary>Carries the source so its handler can cancel the request token before returning.</summary>
internal sealed record SettleInvoiceCommand(CancellationTokenSource Cancellation) : IRequest;
