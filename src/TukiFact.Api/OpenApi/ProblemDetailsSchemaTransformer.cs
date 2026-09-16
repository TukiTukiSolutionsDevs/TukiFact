using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace TukiFact.Api.OpenApi;

/// <summary>Adds the extensions every TukiFact error carries to the single <c>ProblemDetails</c> component, so typed clients read them without a dictionary lookup.</summary>
public sealed class ProblemDetailsSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(context);

        if (context.JsonTypeInfo.Type != typeof(ProblemDetails))
        {
            return Task.CompletedTask;
        }

        schema.Properties ??= new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal);
        schema.Properties["code"] = Text(
            "Stable error code for client logic, e.g. `Document.NotFound`, `Command.Invalid`, `BusinessRule.<Rule>`, " +
            "`Concurrency.Conflict`. Absent on framework responses (malformed JSON, 401, 403, unknown routes) and " +
            "unhandled errors: clients then derive it from the status.");
        schema.Properties["traceId"] = Text("W3C trace id of the request.");
        schema.Properties["correlationId"] = Text("Correlation id of the request (the `X-Correlation-Id` header).");
        schema.Properties["errors"] = new OpenApiSchema
        {
            Type = JsonSchemaType.Array,
            Description = "Only on 400 validation errors: one entry per broken rule.",
            Items = FieldError(),
        };

        return Task.CompletedTask;
    }

    private static OpenApiSchema FieldError() =>
        new()
        {
            Type = JsonSchemaType.Object,
            Title = "FieldError",
            Properties = new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal)
            {
                ["field"] = Text("camelCase JSON path in the request body, e.g. `items[0].quantity`."),
                ["code"] = Text("Stable rule code, e.g. `Quantity.GreaterThanZero`."),
                ["description"] = Text("Human-readable message; clients never branch on it."),
            },
            Required = new HashSet<string>(StringComparer.Ordinal) { "field", "code", "description" },
        };

    private static OpenApiSchema Text(string description) =>
        new() { Type = JsonSchemaType.String, Description = description };
}
