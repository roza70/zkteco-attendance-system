using Microsoft.AspNetCore.Identity;

namespace AttendanceSystem.Data;

public static class SeedData
{
    public static readonly string[] Roles = { "Admin", "HR", "Viewer" };

    public static async Task InitializeAsync(IServiceProvider services, IConfiguration config)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                Check(await roleManager.CreateAsync(new IdentityRole(role)), $"create role {role}");
        }

        var seeds = config.GetSection("SeedUsers").GetChildren().ToList();
        Console.WriteLine($"[Seed] {seeds.Count} seed user(s) found in configuration");

        foreach (var s in seeds)
        {
            var email = s["Email"];
            var password = s["Password"];
            var role = s["Role"];
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(role))
                continue;

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                Check(await userManager.CreateAsync(user, password), $"create user {email}");
            }

            if (!await userManager.IsInRoleAsync(user, role))
                Check(await userManager.AddToRoleAsync(user, role), $"add {email} to role {role}");

            var now = string.Join(", ", await userManager.GetRolesAsync(user));
            Console.WriteLine($"[Seed] {email} -> roles: {now}");
        }
    }

    private static void Check(IdentityResult result, string what)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException($"Seed failed to {what}: " + string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}