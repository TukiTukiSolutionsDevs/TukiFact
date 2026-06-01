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
        // Canonical plans (upsert by Name — safe to evolve across deploys).
        // Pricing tuned against Nubefact, IntiFact, FacturaPeru and CPESunat (2026).
        var canonical = new[]
        {
            new Plan { Name = "Gratis", PriceMonthly = 0, MaxDocumentsPerMonth = 10,
                Features = "{\"api\":false,\"support\":\"none\",\"ai\":false,\"users\":1,\"series\":1,\"trial\":true}" },
            new Plan { Name = "Emprendedor", PriceMonthly = 35, MaxDocumentsPerMonth = 200,
                Features = "{\"api\":false,\"support\":\"email\",\"ai\":false,\"users\":2,\"series\":1}" },
            new Plan { Name = "Negocio", PriceMonthly = 79, MaxDocumentsPerMonth = 2000,
                Features = "{\"api\":true,\"api_rate_limit\":100,\"support\":\"email+tickets\",\"ai\":\"basic\",\"ai_queries\":100,\"users\":5,\"series\":\"multiple\",\"webhooks\":true}" },
            new Plan { Name = "Profesional", PriceMonthly = 179, MaxDocumentsPerMonth = 5000,
                Features = "{\"api\":true,\"api_rate_limit\":500,\"support\":\"priority\",\"ai\":\"full\",\"ai_queries\":500,\"byok\":true,\"sdks\":true,\"users\":15,\"series\":\"multiple\",\"webhooks\":true,\"custom_branding\":true,\"reports\":\"advanced\"}" },
            new Plan { Name = "Empresa", PriceMonthly = 349, MaxDocumentsPerMonth = 15000,
                Features = "{\"api\":true,\"api_rate_limit\":1000,\"support\":\"sla_99.9\",\"ai\":\"full_all_agents\",\"ai_queries\":\"unlimited\",\"byok\":true,\"sdks\":true,\"users\":\"unlimited\",\"series\":\"multiple\",\"webhooks\":true,\"custom_branding\":true,\"reports\":\"advanced\",\"dedicated_api\":true,\"onboarding\":true}" },
        };

        var existing = await context.Plans.ToListAsync();
        var canonicalNames = canonical.Select(p => p.Name).ToHashSet();

        // Upsert by Name — keep Id + CulqiPlanId stable for tenants/subscriptions already pointing at them.
        foreach (var plan in canonical)
        {
            var current = existing.FirstOrDefault(p => p.Name == plan.Name);
            if (current is null)
            {
                await context.Plans.AddAsync(plan);
                Console.WriteLine($"[Seed] Plan added: {plan.Name} (S/{plan.PriceMonthly} · {plan.MaxDocumentsPerMonth} docs)");
            }
            else
            {
                current.PriceMonthly = plan.PriceMonthly;
                current.MaxDocumentsPerMonth = plan.MaxDocumentsPerMonth;
                current.Features = plan.Features;
                current.IsActive = true;
                Console.WriteLine($"[Seed] Plan updated: {plan.Name} (S/{plan.PriceMonthly} · {plan.MaxDocumentsPerMonth} docs)");
            }
        }

        // Soft-deactivate plans no longer in the canonical list (don't delete — Tenants may reference them).
        foreach (var stale in existing.Where(p => !canonicalNames.Contains(p.Name) && p.IsActive))
        {
            stale.IsActive = false;
            Console.WriteLine($"[Seed] Plan deactivated: {stale.Name}");
        }

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
