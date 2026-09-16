using TukiFact.Common.Application.Messaging;

namespace TukiFact.Common.UnitTests.Behaviors.Fakes;

internal sealed record PlaceBasketCommand(string CustomerEmail, BasketAddress Address, IReadOnlyList<BasketLine> Lines) : IRequest;
