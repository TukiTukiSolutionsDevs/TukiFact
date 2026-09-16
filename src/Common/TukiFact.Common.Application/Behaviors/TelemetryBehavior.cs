using System.Diagnostics;
using TukiFact.Common.Application.Abstractions;
using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.Application.Behaviors;

/// <summary>Creates one activity per use case tagged with use case, user, tenant and result.</summary>
internal sealed class TelemetryBehavior<TRequest, TResponse>(ICurrentUser currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private const string UseCaseTag = "tukifact.usecase";
    private const string UserIdTag = "tukifact.user.id";
    private const string TenantIdTag = "tukifact.tenant.id";
    private const string ResultTag = "tukifact.result";
    private const string ErrorCodeTag = "tukifact.error.code";
    private const string ErrorTypeTag = "tukifact.error.type";

    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var requestType = typeof(TRequest);
        using var activity = UseCaseDiagnostics.ActivitySource.StartActivity(UseCaseDiagnostics.ActivityNameOf(requestType));
        if (activity is null)
        {
            return await next();
        }

        activity.SetTag(UseCaseTag, UseCaseDiagnostics.UseCaseOf(requestType));
        activity.SetTag(UserIdTag, currentUser.UserId?.ToString());
        activity.SetTag(TenantIdTag, currentUser.TenantId?.ToString());

        try
        {
            var response = await next();

            activity.SetTag(ResultTag, response.IsSuccess ? "success" : "failure");
            if (response.IsFailure)
            {
                activity.SetTag(ErrorCodeTag, response.Error.Code);
                activity.SetTag(ErrorTypeTag, response.Error.Type.ToString());
            }

            return response;
        }
        catch (Exception exception)
        {
            activity.SetTag(ResultTag, "exception");
            activity.AddException(exception);
            activity.SetStatus(ActivityStatusCode.Error);
            throw;
        }
    }
}
