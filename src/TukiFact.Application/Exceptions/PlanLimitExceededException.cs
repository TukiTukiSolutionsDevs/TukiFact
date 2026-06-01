namespace TukiFact.Application.Exceptions;

/// <summary>
/// Thrown by emit flows when the tenant has consumed its monthly document quota.
/// Controllers translate this to HTTP 402 Payment Required so the client can prompt
/// an upgrade instead of silently failing.
/// </summary>
public class PlanLimitExceededException : Exception
{
    public string PlanName { get; }
    public int MonthlyLimit { get; }
    public int CurrentCount { get; }

    public PlanLimitExceededException(string planName, int monthlyLimit, int currentCount)
        : base($"Plan '{planName}' alcanzó su límite de {monthlyLimit} comprobantes este mes ({currentCount} emitidos).")
    {
        PlanName = planName;
        MonthlyLimit = monthlyLimit;
        CurrentCount = currentCount;
    }
}
