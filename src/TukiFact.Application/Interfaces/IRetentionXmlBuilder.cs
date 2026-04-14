using TukiFact.Domain.Entities;

namespace TukiFact.Application.Interfaces;

public interface IRetentionXmlBuilder
{
    string BuildRetentionXml(RetentionDocument retention, Tenant tenant);
}
