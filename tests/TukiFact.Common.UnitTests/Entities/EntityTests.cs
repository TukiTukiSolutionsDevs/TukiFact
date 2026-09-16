using TukiFact.Common.Domain.Entities;

namespace TukiFact.Common.UnitTests.Entities;

public sealed class EntityTests
{
    [Fact]
    public void Equals_SameTypeAndId_ReturnsTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        var a = new TestEntity(id);
        var b = new TestEntity(id);

        // Act & Assert
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentId_ReturnsFalse()
    {
        // Arrange
        var a = new TestEntity(Guid.NewGuid());
        var b = new TestEntity(Guid.NewGuid());

        // Act & Assert
        a.Should().NotBe(b);
    }

    [Fact]
    public void Equals_TransientInstances_ReturnsFalse()
    {
        // Arrange
        // Transient = default(TId) id, e.g. before persistence assigns an identity.
        var a = new TestEntity(Guid.Empty);
        var b = new TestEntity(Guid.Empty);

        // Act & Assert
        a.Should().NotBe(b);
    }

    [Fact]
    public void Equals_SameTransientInstance_ReturnsTrue()
    {
        // Arrange
        var a = new TestEntity(Guid.Empty);

        // Act & Assert
        a.Equals(a).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentTypeSameId_ReturnsFalse()
    {
        // Arrange
        var id = Guid.NewGuid();
        var a = new TestEntity(id);
        var b = new OtherTestEntity(id);

        // Act & Assert
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void CheckRule_BrokenRule_ThrowsBusinessRuleValidationException()
    {
        // Act
        var act = () => TestEntity.EnforceBrokenRule();

        // Assert
        act.Should().Throw<TukiFact.Common.Domain.Rules.BusinessRuleValidationException>()
            .Where(e => e.BrokenRule.Message == "Rule broken.");
    }

    [Fact]
    public void CheckRule_SatisfiedRule_DoesNotThrow()
    {
        // Act
        var act = () => TestEntity.EnforceSatisfiedRule();

        // Assert
        act.Should().NotThrow();
    }

    private sealed class TestEntity : Entity<Guid>
    {
        public TestEntity(Guid id) => Id = id;

        public static void EnforceBrokenRule() => CheckRule(new AlwaysBrokenRule());

        public static void EnforceSatisfiedRule() => CheckRule(new NeverBrokenRule());
    }

    private sealed class OtherTestEntity : Entity<Guid>
    {
        public OtherTestEntity(Guid id) => Id = id;
    }

    private sealed class AlwaysBrokenRule : TukiFact.Common.Domain.Rules.IBusinessRule
    {
        public string Message => "Rule broken.";

        public bool IsBroken() => true;
    }

    private sealed class NeverBrokenRule : TukiFact.Common.Domain.Rules.IBusinessRule
    {
        public string Message => "Never broken.";

        public bool IsBroken() => false;
    }
}
