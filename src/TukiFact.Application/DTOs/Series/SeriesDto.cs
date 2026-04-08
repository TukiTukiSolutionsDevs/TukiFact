namespace TukiFact.Application.DTOs.Series;

public record CreateSeriesRequest(
    string DocumentType,
    string Serie,
    string EmissionPoint = "PRINCIPAL"
);

public record SeriesResponse(
    Guid Id,
    string DocumentType,
    string Serie,
    long CurrentCorrelative,
    string EmissionPoint,
    bool IsActive,
    DateTimeOffset CreatedAt
);
