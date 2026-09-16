using FluentValidation;
using FluentValidation.Results;
using TukiFact.Common.Application.Messaging;
using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.Application.Behaviors;

/// <summary>
/// Runs FluentValidation validators and short-circuits with one <see cref="ErrorType.Validation"/> error
/// <c>&lt;UseCase&gt;.Validation</c> carrying a <see cref="FieldError"/> per failure; the handler is not
/// invoked and no exception is thrown.
/// </summary>
internal sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    /// <summary>Fixed text: the details travel in the field errors, never in the description.</summary>
    internal const string Description = "La solicitud tiene errores de validación.";

    private readonly IValidator<TRequest>[] _validators = [.. validators];

    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        if (_validators.Length == 0)
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var failures = new List<ValidationFailure>();
        foreach (var validator in _validators)
        {
            var validation = await validator.ValidateAsync(context, cancellationToken);
            failures.AddRange(validation.Errors);
        }

        if (failures.Count == 0)
        {
            return await next();
        }

        var error = Error.Validation(
            $"{UseCaseDiagnostics.UseCaseOf(typeof(TRequest))}.Validation",
            Description,
            failures.Select(ValidationFieldErrors.From));

        return FailureResult<TResponse>.Create(error);
    }
}
