using FluentValidation;

namespace TukiFact.Common.UnitTests.Behaviors.Fakes;

internal sealed class RegisterCustomerCommandValidator : AbstractValidator<RegisterCustomerCommand>
{
    public RegisterCustomerCommandValidator() => RuleFor(command => command.Email).NotEmpty();
}
