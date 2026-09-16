using FluentValidation;

namespace TukiFact.Common.UnitTests.Behaviors.Fakes;

/// <summary>Mixes rules with and without a declared error code on top-level, nested and indexed properties.</summary>
internal sealed class PlaceBasketCommandValidator : AbstractValidator<PlaceBasketCommand>
{
    public const int MaxLines = 2;
    public const string TooManyLinesMessage = "A basket holds at most 2 lines.";

    public PlaceBasketCommandValidator()
    {
        RuleFor(command => command.CustomerEmail).NotEmpty();
        RuleFor(command => command.Address.PostalCode).NotEmpty().WithErrorCode("PostalCode.Required");
        RuleForEach(command => command.Lines).ChildRules(line =>
        {
            line.RuleFor(item => item.Sku).Must(sku => sku.StartsWith("SKU-", StringComparison.Ordinal));
            line.RuleFor(item => item.Quantity).GreaterThan(0).WithErrorCode("Quantity.GreaterThanZero");
        });
        RuleFor(command => command.Lines).Custom((lines, context) =>
        {
            if (lines.Count > MaxLines)
            {
                context.AddFailure(TooManyLinesMessage);
            }
        });
    }
}
