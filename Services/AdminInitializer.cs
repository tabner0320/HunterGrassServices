
using Microsoft.AspNetCore.Identity;

namespace HunterGrassServices.Api.Services;

public static class AdminInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider serviceProvider)
    {
        // =========================================
        // CREATE SERVICE SCOPE
        // =========================================

        using var scope = serviceProvider.CreateScope();

        var services = scope.ServiceProvider;

        // Get Identity services
        var roleManager = services
            .GetRequiredService<RoleManager<IdentityRole>>();

        var userManager = services
            .GetRequiredService<UserManager<IdentityUser>>();

        // Get application configuration
        var configuration = services
            .GetRequiredService<IConfiguration>();

        // Get application logger
        var logger = services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("AdminInitializer");

        const string adminRole = "Admin";

        // =========================================
        // CREATE ADMINISTRATOR ROLE
        // =========================================

        if (!await roleManager.RoleExistsAsync(adminRole))
        {
            var roleResult = await roleManager.CreateAsync(
                new IdentityRole(adminRole)
            );

            if (!roleResult.Succeeded)
            {
                var errors = string.Join(
                    ", ",
                    roleResult.Errors.Select(error =>
                        error.Description)
                );

                throw new InvalidOperationException(
                    $"Unable to create Admin role: {errors}"
                );
            }

            logger.LogInformation(
                "Administrator role created successfully."
            );
        }
        else
        {
            logger.LogInformation(
                "Administrator role already exists."
            );
        }

        // =========================================
        // CHECK ADMINISTRATOR CONFIGURATION
        // =========================================

        var adminEmail =
            configuration["AdminSettings:Email"];

        var adminPassword =
            configuration["AdminSettings:Password"];

        // Skip account creation when credentials
        // have not been configured
        if (string.IsNullOrWhiteSpace(adminEmail) ||
            string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogInformation(
                "Administrator credentials are not configured. " +
                "Account creation skipped."
            );

            return;
        }

        adminEmail = adminEmail.Trim();

        // =========================================
        // CHECK EXISTING ADMINISTRATOR ACCOUNT
        // =========================================

        var existingUser =
            await userManager.FindByEmailAsync(adminEmail);

        if (existingUser is not null)
        {
            // Do not automatically promote an existing user
            if (!await userManager.IsInRoleAsync(
                existingUser, adminRole))
            {
                throw new InvalidOperationException(
                    "The configured administrator email " +
                    "belongs to an existing non-admin account."
                );
            }

            logger.LogInformation(
                "Administrator account already exists."
            );

            return;
        }

        // =========================================
        // CREATE ADMINISTRATOR ACCOUNT
        // =========================================

        var adminUser = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            LockoutEnabled = true
        };

        var userResult = await userManager.CreateAsync(
            adminUser,
            adminPassword
        );

        if (!userResult.Succeeded)
        {
            var errors = string.Join(
                ", ",
                userResult.Errors.Select(error =>
                    error.Description)
            );

            throw new InvalidOperationException(
                $"Unable to create administrator account: {errors}"
            );
        }

        // =========================================
        // ASSIGN ADMINISTRATOR ROLE
        // =========================================

        var assignmentResult =
            await userManager.AddToRoleAsync(
                adminUser,
                adminRole
            );

        if (!assignmentResult.Succeeded)
        {
            // Remove the newly created account if
            // administrator role assignment fails
            var cleanupResult =
                await userManager.DeleteAsync(adminUser);

            if (!cleanupResult.Succeeded)
            {
                logger.LogError(
                    "Unable to remove the incomplete " +
                    "administrator account."
                );
            }

            var errors = string.Join(
                ", ",
                assignmentResult.Errors.Select(error =>
                    error.Description)
            );

            throw new InvalidOperationException(
                $"Unable to assign Admin role: {errors}"
            );
        }

        logger.LogInformation(
            "Administrator account created successfully."
        );
    }
}
