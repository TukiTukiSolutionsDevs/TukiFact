using TukiFact.Common.Domain.Rules;

namespace TukiFact.Common.UnitTests.Rules;

public sealed class BusinessRuleTests
{
    [Fact]
    public void Constructor_WithBrokenRule_ExposesRuleAndItsMessage()
    {
        // Arrange
        var rule = new AlwaysBrokenRule();

        // Act
        var exception = new BusinessRuleValidationException(rule);

        // Assert
        exception.Message.Should().Be(rule.Message);
        exception.BrokenRule.Should().BeSameAs(rule);
    }

    [Fact]
    public void Constructor_NullRule_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new BusinessRuleValidationException(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class AlwaysBrokenRule : IBusinessRule
    {
        public string Message => "Rule broken.";

        public bool IsBroken() => true;
    }
}
