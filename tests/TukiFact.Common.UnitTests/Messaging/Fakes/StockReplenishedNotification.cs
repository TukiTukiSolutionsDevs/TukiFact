using TukiFact.Common.Application.Messaging;

namespace TukiFact.Common.UnitTests.Messaging.Fakes;

internal sealed record StockReplenishedNotification(Guid SkuId) : INotification;
