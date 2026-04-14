namespace TukiFact.Application.DTOs.Dashboard;

public record DashboardResponse(
    DashboardSummary Today,
    DashboardSummary ThisMonth,
    DashboardSummary ThisYear,
    List<DocumentsByType> ByType,
    List<DocumentsByStatus> ByStatus,
    List<MonthlySales> MonthlySales
);

public record DashboardSummary(
    int TotalDocuments,
    decimal TotalAmount,
    decimal TotalIgv,
    int Accepted,
    int Rejected,
    int Pending
);

public record DocumentsByType(string DocumentType, string Name, int Count, decimal Total);
public record DocumentsByStatus(string Status, int Count);
public record MonthlySales(string Month, int Year, int Count, decimal Total);
