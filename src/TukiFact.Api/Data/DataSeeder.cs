using TukiFact.Domain.Entities;
using TukiFact.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TukiFact.Api.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Plans.AnyAsync())
            return; // Already seeded

        var plans = new[]
        {
            new Plan { Name = "Free", PriceMonthly = 0, MaxDocumentsPerMonth = 50,
                Features = "{\"api\":false,\"support\":\"none\",\"ai\":false,\"users\":1,\"series\":1}" },
            new Plan { Name = "Emprendedor", PriceMonthly = 39, MaxDocumentsPerMonth = 300,
                Features = "{\"api\":true,\"api_rate_limit\":100,\"support\":\"email\",\"ai\":false,\"users\":3,\"series\":1}" },
            new Plan { Name = "Negocio", PriceMonthly = 79, MaxDocumentsPerMonth = 1000,
                Features = "{\"api\":true,\"api_rate_limit\":300,\"support\":\"email+tickets\",\"ai\":\"basic\",\"users\":10,\"series\":\"multiple\",\"webhooks\":true,\"custom_branding\":true}" },
            new Plan { Name = "Profesional", PriceMonthly = 149, MaxDocumentsPerMonth = 3000,
                Features = "{\"api\":true,\"api_rate_limit\":500,\"support\":\"priority\",\"ai\":\"full\",\"byok\":true,\"users\":25,\"series\":\"multiple\",\"webhooks\":true,\"reports\":\"advanced\"}" },
            new Plan { Name = "Empresa", PriceMonthly = 299, MaxDocumentsPerMonth = 10000,
                Features = "{\"api\":true,\"api_rate_limit\":1000,\"support\":\"sla_99.9\",\"ai\":\"full_all_agents\",\"byok\":true,\"users\":\"unlimited\",\"series\":\"multiple\",\"webhooks\":true,\"dedicated_api\":true}" },
            new Plan { Name = "Developer", PriceMonthly = 99, MaxDocumentsPerMonth = 1000,
                Features = "{\"api\":true,\"api_rate_limit\":500,\"support\":\"docs\",\"ai\":\"copilot\",\"sandbox\":true,\"sdks\":true,\"users\":5,\"panel\":false}" }
        };

        await context.Plans.AddRangeAsync(plans);
        await context.SaveChangesAsync();
    }
}
