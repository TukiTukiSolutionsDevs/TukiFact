using TukiFact.Domain.Entities;

namespace TukiFact.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    (Guid UserId, Guid TenantId)? ValidateAccessToken(string token);
}
