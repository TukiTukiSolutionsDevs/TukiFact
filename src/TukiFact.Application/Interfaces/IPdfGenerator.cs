using TukiFact.Domain.Entities;

namespace TukiFact.Application.Interfaces;

public interface IPdfGenerator
{
    byte[] GenerateInvoicePdf(Document document, Tenant tenant);
}
