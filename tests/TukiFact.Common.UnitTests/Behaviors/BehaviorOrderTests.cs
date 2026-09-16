using Microsoft.Extensions.DependencyInjection;
using TukiFact.Common.Application.DependencyInjection;
using TukiFact.Common.Application.Messaging;

namespace TukiFact.Common.UnitTests.Behaviors;

public sealed class BehaviorOrderTests
{
    [Fact]
    public void AddCommonApplication_Default_RegistersTheFourBehaviorsInFixedOrder()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCommonApplication(typeof(BehaviorOrderTests).Assembly);

        // Assert: Logging → Telemetry → Validation → Transaction, the exact order the Mediator dispatches in.
        services
            .Where(descriptor => descriptor.ServiceType == typeof(IPipelineBehavior<,>))
            .Select(descriptor => descriptor.ImplementationType?.Name)
            .Should().Equal(
                "LoggingBehavior`2",
                "TelemetryBehavior`2",
                "ValidationBehavior`2",
                "TransactionBehavior`2");
    }
}
