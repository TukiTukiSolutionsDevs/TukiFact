using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.UnitTests.Behaviors.Fakes;

/// <summary>Use-case name kept unique across the test assembly: telemetry tests assert on activity names by string.</summary>
internal sealed record VoidInvoiceCommand : IRequest;
