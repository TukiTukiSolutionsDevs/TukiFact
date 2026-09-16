using TukiFact.Common.Application.Abstractions;
using TukiFact.Common.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

internal sealed class KernelTestOrderReadDbConnection(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    ISystemScope systemScope,
    ICorrelationIdAccessor correlationIdAccessor,
    ILogger<ReadDbConnection> logger)
    : ReadDbConnection(connectionFactory, currentUser, systemScope, correlationIdAccessor, logger), IKernelTestOrderReadDbConnection;
