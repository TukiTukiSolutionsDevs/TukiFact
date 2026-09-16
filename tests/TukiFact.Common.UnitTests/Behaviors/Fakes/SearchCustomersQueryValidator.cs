using FluentValidation;

namespace TukiFact.Common.UnitTests.Behaviors.Fakes;

internal sealed class SearchCustomersQueryValidator : AbstractValidator<SearchCustomersQuery>
{
    public SearchCustomersQueryValidator() => RuleFor(query => query.PageSize).GreaterThan(0);
}
