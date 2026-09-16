using TukiFact.Common.Application.Messaging;
using TukiFact.Common.UnitTests.Fakes;

namespace TukiFact.Common.UnitTests.Messaging.Fakes;

internal sealed class FirstStockReplenishedHandler(CallLog log) : INotificationHandler<StockReplenishedNotification>
{
    public Task Handle(StockReplenishedNotification notification, CancellationToken cancellationToken)
    {
        log.Add(nameof(FirstStockReplenishedHandler));
        return Task.CompletedTask;
    }
}
