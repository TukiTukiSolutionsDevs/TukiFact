using TukiFact.Domain.Entities;

namespace TukiFact.Application.Interfaces;

public interface IUblBuilder
{
    string BuildInvoiceXml(Document document, Tenant tenant);
    string BuildCreditNoteXml(Document document, Tenant tenant);
    string BuildDebitNoteXml(Document document, Tenant tenant);
}
