using TukiFact.Common.Presentation.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace TukiFact.Common.Presentation.DependencyInjection;

public static class PresentationDependencyInjection
{
    /// <summary>
    /// RFC 7807 for every error (<see cref="ProblemDetailsEnricher"/>) and the domain exception
    /// handler. Unhandled exceptions fall through to the framework handler, which writes a 500
    /// ProblemDetails without details. <c>DbUpdateConcurrencyException</c> is registered separately by
    /// the host via <c>Common.Infrastructure.Persistence.AddPersistenceExceptionHandling()</c> — this
    /// layer cannot reference Infrastructure (Presentation→Application only).
    /// </summary>
    public static IServiceCollection AddCommonPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddProblemDetails(options => options.CustomizeProblemDetails = ProblemDetailsEnricher.Enrich);
        services.AddExceptionHandler<BusinessRuleValidationExceptionHandler>();

        return services;
    }

    /// <summary>Adds <see cref="CorrelationIdMiddleware"/>; place it after the exception handler and before routing.</summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
