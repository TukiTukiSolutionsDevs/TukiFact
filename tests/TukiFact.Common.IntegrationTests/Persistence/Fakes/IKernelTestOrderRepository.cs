namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

public interface IKernelTestOrderRepository
{
    Task<KernelTestOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(KernelTestOrder order);
}
