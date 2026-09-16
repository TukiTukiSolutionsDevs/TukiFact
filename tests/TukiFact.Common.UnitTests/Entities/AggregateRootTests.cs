using TukiFact.Common.Domain.DomainEvents;
using TukiFact.Common.Domain.Entities;

namespace TukiFact.Common.UnitTests.Entities;

public sealed class AggregateRootTests
{
    [Fact]
    public void RaiseDomainEvent_OnDoSomething_ExposesRaisedEvent()
    {
        // Arrange
        var aggregate = new TestAggregate();
        var occurredOn = DateTimeOffset.UtcNow;

        // Act
        aggregate.DoSomething(occurredOn);

        // Assert
        aggregate.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<TestDomainEvent>()
            .Which.OccurredOn.Should().Be(occurredOn);
    }

    [Fact]
    public void ClearDomainEvents_WithRaisedEvents_RemovesAllEvents()
    {
        // Arrange
        var aggregate = new TestAggregate();
        aggregate.DoSomething(DateTimeOffset.UtcNow);

        // Act
        aggregate.ClearDomainEvents();

        // Assert
        aggregate.GetDomainEvents().Should().BeEmpty();
    }

    [Fact]
    public void GetDomainEvents_EventRaisedAfterSnapshot_SnapshotIsUnchanged()
    {
        // Arrange
        var aggregate = new TestAggregate();
        aggregate.DoSomething(DateTimeOffset.UtcNow);

        // Act
        var snapshot = aggregate.GetDomainEvents();
        aggregate.DoSomething(DateTimeOffset.UtcNow);

        // Assert
        snapshot.Should().HaveCount(1);
        aggregate.GetDomainEvents().Should().HaveCount(2);
    }

    private sealed record TestDomainEvent(DateTimeOffset OccurredOn) : DomainEvent(OccurredOn);

    private sealed class TestAggregate : AggregateRoot<Guid>
    {
        public TestAggregate() => Id = Guid.NewGuid();

        public void DoSomething(DateTimeOffset occurredOn) => RaiseDomainEvent(new TestDomainEvent(occurredOn));
    }
}
