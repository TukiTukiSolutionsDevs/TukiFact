using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;
using TukiFact.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TukiFact.Api.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext context, IPasswordHasher passwordHasher)
    {
        await SeedPlansAsync(context);
        await SeedAdminTenantAsync(context, passwordHasher);
        await SeedSuperAdminAsync(context, passwordHasher);
    }

    private static async Task SeedPlansAsync(AppDbContext context)
    {
        if (await context.Plans.AnyAsync())
            return;

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

    private static async Task SeedAdminTenantAsync(AppDbContext context, IPasswordHasher passwordHasher)
    {
        // Skip if any tenant already exists (user registered or previously seeded)
        if (await context.Tenants.AnyAsync())
            return;

        // Read admin credentials from environment (defaults for first deploy)
        var adminEmail = Environment.GetEnvironmentVariable("SEED_ADMIN_EMAIL") ?? "admin@tukifact.net.pe";
        var adminPassword = Environment.GetEnvironmentVariable("SEED_ADMIN_PASSWORD") ?? "TukiFact2026!";
        var adminName = Environment.GetEnvironmentVariable("SEED_ADMIN_NAME") ?? "Admin TukiFact";
        var tenantRuc = Environment.GetEnvironmentVariable("SEED_TENANT_RUC") ?? "20613614509";
        var tenantName = Environment.GetEnvironmentVariable("SEED_TENANT_RAZON_SOCIAL") ?? "Tukituki Solution SAC";

        var empresaPlan = await context.Plans.FirstOrDefaultAsync(p => p.Name == "Empresa");

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Ruc = tenantRuc,
            RazonSocial = tenantName,
            NombreComercial = "TukiFact",
            Direccion = "Arequipa, Peru",
            PlanId = empresaPlan?.Id,
            Environment = "beta",
            IsActive = true
        };

        var admin = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = adminEmail,
            PasswordHash = passwordHasher.Hash(adminPassword),
            FullName = adminName,
            Role = "admin",
            IsActive = true
        };

        await context.Tenants.AddAsync(tenant);
        await context.Users.AddAsync(admin);
        await context.SaveChangesAsync();

        Console.WriteLine($"[Seed] Admin tenant created: {tenantName} ({tenantRuc})");
        Console.WriteLine($"[Seed] Admin user: {adminEmail}");
    }

    private static async Task SeedSuperAdminAsync(AppDbContext context, IPasswordHasher passwordHasher)
    {
        if (await context.PlatformUsers.AnyAsync())
            return;

        var email = Environment.GetEnvironmentVariable("SEED_SUPERADMIN_EMAIL") ?? "superadmin@tukifact.net.pe";
        var password = Environment.GetEnvironmentVariable("SEED_SUPERADMIN_PASSWORD") ?? "SuperAdmin2026!";
        var name = Environment.GetEnvironmentVariable("SEED_SUPERADMIN_NAME") ?? "Super Admin";

        var superadmin = new PlatformUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHasher.Hash(password),
            FullName = name,
            Role = "superadmin",
            IsActive = true
        };

        await context.PlatformUsers.AddAsync(superadmin);
        await context.SaveChangesAsync();

        Console.WriteLine($"[Seed] SuperAdmin created: {email}");
    }
}
