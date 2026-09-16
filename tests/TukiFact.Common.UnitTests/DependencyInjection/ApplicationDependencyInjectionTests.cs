using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TukiFact.Common.Application.Abstractions;
using TukiFact.Common.Application.DependencyInjection;
using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.Results;
using TukiFact.Common.UnitTests.Behaviors.Fakes;
using TukiFact.Common.UnitTests.Fakes;
using TukiFact.Common.UnitTests.Messaging.Fakes;

namespace TukiFact.Common.UnitTests.DependencyInjection;

public sealed class ApplicationDependencyInjectionTests
{
    [Fact]
    public void AddCommonApplication_Default_RegistersSenderAndPublisher()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCommonApplication(typeof(ApplicationDependencyInjectionTests).Assembly);

        // Assert
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(ISender));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IPublisher));
    }

    [Fact]
    public void AddCommonApplication_Default_RegistersClockOverSystemTimeProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCommonApplication(typeof(ApplicationDependencyInjectionTests).Assembly);
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetRequiredService<IClock>().Should().NotBeNull();
        provider.GetRequiredService<TimeProvider>().Should().BeSameAs(TimeProvider.System);
    }

    [Fact]
    public void AddCommonApplication_ScansAssembly_RegistersEveryRequestHandlerFound()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<CallLog>();

        // Act
        services.AddCommonApplication(typeof(ApplicationDependencyInjectionTests).Assembly);
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetRequiredService<IRequestHandler<PlaceOrderCommand, Result<Guid>>>()
            .Should().BeOfType<PlaceOrderCommandHandler>();
    }

    [Fact]
    public void AddCommonApplication_ScansAssembly_RegistersFluentValidationValidatorsFound()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCommonApplication(typeof(ApplicationDependencyInjectionTests).Assembly);
        using var provider = services.BuildServiceProvider();

        // Assert
        provider.GetServices<IValidator<RegisterCustomerCommand>>()
            .Should().ContainSingle(validator => validator is RegisterCustomerCommandValidator);
    }
}
