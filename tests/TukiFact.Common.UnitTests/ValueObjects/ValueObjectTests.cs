using TukiFact.Common.Domain.ValueObjects;

namespace TukiFact.Common.UnitTests.ValueObjects;

public sealed class ValueObjectTests
{
    [Fact]
    public void Equals_SameComponents_ReturnsTrue()
    {
        // Arrange
        var a = new Money(100, "PEN");
        var b = new Money(100, "PEN");

        // Act & Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentComponents_ReturnsFalse()
    {
        // Arrange
        var a = new Money(100, "PEN");
        var b = new Money(100, "USD");

        // Act & Assert
        a.Should().NotBe(b);
    }

    [Fact]
    public void Equals_DifferentTypeSameComponents_ReturnsFalse()
    {
        // Arrange
        var money = new Money(100, "PEN");
        var other = new OtherValueObject(100, "PEN");

        // Act & Assert
        money.Equals(other).Should().BeFalse();
    }

    private sealed class Money(decimal amount, string currency) : ValueObject
    {
        private readonly decimal _amount = amount;
        private readonly string _currency = currency;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return _amount;
            yield return _currency;
        }
    }

    private sealed class OtherValueObject(decimal amount, string currency) : ValueObject
    {
        private readonly decimal _amount = amount;
        private readonly string _currency = currency;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return _amount;
            yield return _currency;
        }
    }
}
