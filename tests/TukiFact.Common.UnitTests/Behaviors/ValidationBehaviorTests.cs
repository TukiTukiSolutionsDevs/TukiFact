using Microsoft.Extensions.DependencyInjection;
using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.Results;
using TukiFact.Common.UnitTests.Behaviors.Fakes;
using TukiFact.Common.UnitTests.Fakes;

namespace TukiFact.Common.UnitTests.Behaviors;

public sealed class ValidationBehaviorTests
{
    private const string FixedDescription = "La solicitud tiene errores de validación.";

    /// <summary>Line 1 breaks both line rules; three lines break the custom rule on the list.</summary>
    private static readonly PlaceBasketCommand InvalidBasket = new(
        CustomerEmail: string.Empty,
        Address: new BasketAddress(PostalCode: string.Empty),
        Lines: [new BasketLine("SKU-1", 1), new BasketLine("bad", 0), new BasketLine("SKU-3", 3)]);

    [Fact]
    public async Task Send_InvalidCommand_ReturnsValidationErrorWithFieldErrorsWithoutInvokingHandlerOrTransaction()
    {
        // Arrange
        await using var provider = ApplicationTestHost.Build();
        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        var result = await sender.Send(new RegisterCustomerCommand(string.Empty), TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("RegisterCustomer.Validation");
        result.Error.Description.Should().Be(FixedDescription);
        result.Error.FieldErrors.Should().ContainSingle(error => error.Field == "email");
        scope.ServiceProvider.GetRequiredService<CallLog>().Entries.Should().BeEmpty();
        scope.ServiceProvider.GetRequiredService<FakeTransactionManager>().Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Send_ValidCommand_InvokesHandlerAndCommitsTransaction()
    {
        // Arrange
        await using var provider = ApplicationTestHost.Build();
        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        var result = await sender.Send(new RegisterCustomerCommand("ana@example.com"), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        scope.ServiceProvider.GetRequiredService<CallLog>().Entries.Should().Equal(nameof(RegisterCustomerCommandHandler));
        scope.ServiceProvider.GetRequiredService<FakeTransactionManager>().Calls.Should().Equal(
            FakeTransactionManager.Begin,
            FakeTransactionManager.Commit);
    }

    [Fact]
    public async Task Send_RequestWithoutRegisteredValidator_PassesThroughToHandler()
    {
        // Arrange
        await using var provider = ApplicationTestHost.Build();
        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        var result = await sender.Send(new RejectOrderCommand(), TestContext.Current.CancellationToken);

        // Assert: RejectOrderCommand has no FluentValidation validator registered, so validation is a no-op.
        result.Error.Should().Be(RejectOrderCommandHandler.AlreadyShipped);
    }

    [Fact]
    public async Task Send_InvalidQuery_ShortCircuitsWithTypedValidationFailureWithoutInvokingHandler()
    {
        // Arrange
        await using var provider = ApplicationTestHost.Build();
        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        var result = await sender.Send(new SearchCustomersQuery(PageSize: 0), TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("SearchCustomers.Validation");
        result.Error.FieldErrors.Select(error => (error.Field, error.Code)).Should().Equal(("pageSize", "Validation.GreaterThan"));
        scope.ServiceProvider.GetRequiredService<CallLog>().Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Send_NestedAndIndexedProperties_MapsFieldToCamelCaseJsonPath()
    {
        // Act
        var result = await SendInvalidAsync(InvalidBasket);

        // Assert
        result.Error.FieldErrors.Select(error => error.Field).Should()
            .BeEquivalentTo("customerEmail", "address.postalCode", "lines[1].sku", "lines[1].quantity", "lines");
    }

    [Fact]
    public async Task Send_RuleWithExplicitErrorCode_KeepsTheDeclaredCode()
    {
        // Act
        var result = await SendInvalidAsync(InvalidBasket);

        // Assert
        result.Error.FieldErrors.Select(error => (error.Field, error.Code)).Should()
            .Contain([("address.postalCode", "PostalCode.Required"), ("lines[1].quantity", "Quantity.GreaterThanZero")]);
    }

    [Fact]
    public async Task Send_RuleWithoutErrorCode_FallsBackToValidationAndTheRuleName()
    {
        // Act
        var result = await SendInvalidAsync(InvalidBasket);

        // Assert
        result.Error.FieldErrors.Select(error => (error.Field, error.Code)).Should()
            .Contain([("customerEmail", "Validation.NotEmpty"), ("lines[1].sku", "Validation.Predicate")]);
    }

    [Fact]
    public async Task Send_CustomFailureWithoutErrorCode_FallsBackToValidationCustom()
    {
        // Act
        var result = await SendInvalidAsync(InvalidBasket);

        // Assert
        result.Error.FieldErrors.Should().ContainSingle(error => error.Field == "lines")
            .Which.Code.Should().Be("Validation.Custom");
    }

    [Fact]
    public async Task Send_InvalidCommand_FieldErrorDescriptionIsTheRuleMessage()
    {
        // Act
        var result = await SendInvalidAsync(InvalidBasket);

        // Assert
        result.Error.FieldErrors.Should().ContainSingle(error => error.Field == "lines")
            .Which.Description.Should().Be(PlaceBasketCommandValidator.TooManyLinesMessage);
    }

    private static async Task<Result> SendInvalidAsync(PlaceBasketCommand command)
    {
        await using var provider = ApplicationTestHost.Build();
        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var result = await sender.Send(command, TestContext.Current.CancellationToken);
        result.IsFailure.Should().BeTrue();

        return result;
    }
}
