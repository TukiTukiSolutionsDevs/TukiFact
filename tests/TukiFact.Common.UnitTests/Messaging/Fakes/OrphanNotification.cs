using TukiFact.Common.Application.Messaging;

namespace TukiFact.Common.UnitTests.Messaging.Fakes;

/// <summary>Deliberately has no registered <see cref="INotificationHandler{TNotification}"/>.</summary>
internal sealed record OrphanNotification : INotification;
