using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TukiFact.Common.Application.Behaviors;
using TukiFact.Common.Application.Messaging;
using TukiFact.Common.UnitTests.Behaviors.Fakes;
using TukiFact.Common.UnitTests.Fakes;

namespace TukiFact.Common.UnitTests.Behaviors;

public sealed class TelemetryBehaviorTests : IDisposable
{
    private readonly ConcurrentQueue<Activity> _stoppedActivities = new();
    private readonly ActivityListener _listener;

    public TelemetryBehaviorTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == UseCaseDiagnostics.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => _stoppedActivities.Enqueue(activity),
        };
        ActivitySource.AddActivityListener(_listener);
    }

    [Fact]
    public void ActivitySourceName_Always_IsPrefixedWithTukiFact()
    {
        // Act & Assert
        UseCaseDiagnostics.ActivitySourceName.Should().Be("TukiFact.Common.Application");
    }

    [Fact]
    public async Task Send_SuccessfulCommand_RecordsUseCaseActivityWithTukifactPrefixedTags()
    {
        // Arrange
        await using var provider = ApplicationTestHost.Build();
        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        await sender.Send(new ApproveInvoiceCommand(), TestContext.Current.CancellationToken);

        // Assert
        var tags = TagsOfActivity("tukifact.usecase.ApproveInvoice");
        tags.Should().Contain(new Dictionary<string, string?>
        {
            ["tukifact.usecase"] = "ApproveInvoice",
            ["tukifact.user.id"] = FakeCurrentUser.User.ToString(),
            ["tukifact.tenant.id"] = FakeCurrentUser.Tenant.ToString(),
            ["tukifact.result"] = "success",
        });
        tags.Keys.Should().OnlyContain(key => key.StartsWith("tukifact.", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Send_FailedCommand_TagsFailureWithErrorCode()
    {
        // Arrange
        await using var provider = ApplicationTestHost.Build();
        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        await sender.Send(new VoidInvoiceCommand(), TestContext.Current.CancellationToken);

        // Assert
        TagsOfActivity("tukifact.usecase.VoidInvoice").Should().Contain(new Dictionary<string, string?>
        {
            ["tukifact.result"] = "failure",
            ["tukifact.error.code"] = VoidInvoiceCommandHandler.InvoiceNotFound.Code,
            ["tukifact.error.type"] = "NotFound",
        });
    }

    public void Dispose() => _listener.Dispose();

    private Dictionary<string, string?> TagsOfActivity(string operationName) =>
        _stoppedActivities.Should().ContainSingle(activity => activity.OperationName == operationName)
            .Which.Tags.ToDictionary(tag => tag.Key, tag => tag.Value);
}
