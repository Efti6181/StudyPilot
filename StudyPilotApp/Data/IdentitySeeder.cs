using Microsoft.AspNetCore.Identity;
using StudyPilotApp.Models;

namespace StudyPilotApp.Data;

public static class IdentitySeeder
{
    private static readonly string[] Roles = ["Student", "Faculty", "Admin"];

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var roleName in Roles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException($"Could not create role '{roleName}': {FormatErrors(roleResult)}");
            }
        }

        var email = configuration["SeedAdmin:Email"];
        var fullName = configuration["SeedAdmin:FullName"];
        var password = configuration["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("SeedAdmin credentials are missing from User Secrets.");
        }

        var admin = await userManager.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                FullName = fullName,
                Email = email,
                UserName = email,
                EmailConfirmed = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var createResult = await userManager.CreateAsync(admin, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException($"Could not create Admin: {FormatErrors(createResult)}");
            }

            var addRoleResult = await userManager.AddToRoleAsync(admin, "Admin");
            if (!addRoleResult.Succeeded)
            {
                throw new InvalidOperationException($"Could not assign Admin role: {FormatErrors(addRoleResult)}");
            }
        }
        else if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            throw new InvalidOperationException("The configured Admin email already exists without the Admin role.");
        }
    }

    private static string FormatErrors(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(x => $"{x.Code}: {x.Description}"));
}
