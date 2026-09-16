using Microsoft.EntityFrameworkCore;

namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

internal sealed class KernelTestOrderRepository(KernelTestDbContext context) : IKernelTestOrderRepository
{
    public Task<KernelTestOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Orders.SingleOrDefaultAsync(order => order.Id == id, cancellationToken);

    public void Add(KernelTestOrder order) => context.Orders.Add(order);
}
