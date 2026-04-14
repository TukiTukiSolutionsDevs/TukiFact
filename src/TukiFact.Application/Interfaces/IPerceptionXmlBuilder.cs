using TukiFact.Domain.Entities;

namespace TukiFact.Application.Interfaces;

public interface IPerceptionXmlBuilder
{
    string BuildPerceptionXml(PerceptionDocument perception, Tenant tenant);
}
