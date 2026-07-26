using CardUsageGuard.Models;
using Microsoft.AspNetCore.Identity;

namespace CardUsageGuard.Data;

public static class SeedData
{
    public static async Task InitializeAsync(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        // Seed roles
        if (!await roleManager.RoleExistsAsync("admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("admin"));
        }
        if (!await roleManager.RoleExistsAsync("user"))
        {
            await roleManager.CreateAsync(new IdentityRole("user"));
        }

        // Seed admin user
        var adminEmail = "admin@cardusageguard.local";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Admin",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, "Admin@123456");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "admin");
            }
        }
    }
}