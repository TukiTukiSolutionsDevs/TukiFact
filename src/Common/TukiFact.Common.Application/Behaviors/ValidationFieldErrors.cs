using System.Text;
using System.Text.Json;
using FluentValidation.Results;
using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.Application.Behaviors;

/// <summary>Translates a FluentValidation failure into the contract <see cref="FieldError"/>.</summary>
internal static class ValidationFieldErrors
{
    private const string FallbackCodePrefix = "Validation.";
    private const string ValidatorSuffix = "Validator";

    /// <summary>Code for a failure added without any code (e.g. <c>context.AddFailure</c> inside <c>Custom</c>).</summary>
    private const string CustomRule = "Custom";

    public static FieldError From(ValidationFailure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);

        return new FieldError(JsonPathOf(failure.PropertyName), CodeOf(failure.ErrorCode), failure.ErrorMessage);
    }

    /// <summary>
    /// A rule's <c>WithErrorCode</c> is kept. Without it FluentValidation reports the validator name
    /// (<c>NotEmptyValidator</c>), which becomes <c>Validation.NotEmpty</c>. Declared codes follow
    /// <c>&lt;Field&gt;.&lt;Rule&gt;</c> and always contain a dot, so they never look like a validator name.
    /// </summary>
    private static string CodeOf(string? errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            return FallbackCodePrefix + CustomRule;
        }

        var isValidatorName = errorCode.Length > ValidatorSuffix.Length
            && errorCode.EndsWith(ValidatorSuffix, StringComparison.Ordinal)
            && !errorCode.Contains('.', StringComparison.Ordinal);

        return isValidatorName ? FallbackCodePrefix + errorCode[..^ValidatorSuffix.Length] : errorCode;
    }

    /// <summary>
    /// camelCase per member segment, indexers kept verbatim: <c>Items[0].Quantity</c> → <c>items[0].quantity</c>.
    /// The command uses the request property names, so the path points into the HTTP body.
    /// </summary>
    private static string JsonPathOf(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return string.Empty;
        }

        var path = new StringBuilder(propertyName.Length);
        var member = new StringBuilder();
        var indexerDepth = 0;

        foreach (var character in propertyName)
        {
            if (indexerDepth > 0)
            {
                path.Append(character);
                indexerDepth += character switch { '[' => 1, ']' => -1, _ => 0 };
                continue;
            }

            if (character is '.' or '[')
            {
                AppendMember(path, member);
                path.Append(character);
                indexerDepth = character == '[' ? 1 : 0;
                continue;
            }

            member.Append(character);
        }

        AppendMember(path, member);

        return path.ToString();
    }

    private static void AppendMember(StringBuilder path, StringBuilder member)
    {
        if (member.Length == 0)
        {
            return;
        }

        path.Append(JsonNamingPolicy.CamelCase.ConvertName(member.ToString()));
        member.Clear();
    }
}
