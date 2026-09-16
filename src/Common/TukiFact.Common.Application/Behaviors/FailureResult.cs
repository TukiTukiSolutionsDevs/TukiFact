using System.Reflection;
using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.Application.Behaviors;

/// <summary>Builds a failed <typeparamref name="TResponse"/> (<see cref="Result"/> or <see cref="Result{TValue}"/>) from an <see cref="Error"/>.</summary>
internal static class FailureResult<TResponse>
    where TResponse : Result
{
    public static readonly Func<Error, TResponse> Create = BuildFactory();

    private static Func<Error, TResponse> BuildFactory()
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            return error => (TResponse)Result.Failure(error);
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            return typeof(Result)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(method => method is { Name: nameof(Result.Failure), IsGenericMethodDefinition: true })
                .MakeGenericMethod(responseType.GetGenericArguments()[0])
                .CreateDelegate<Func<Error, TResponse>>();
        }

        throw new InvalidOperationException($"Response type '{responseType}' must be Result or Result<T>.");
    }
}
