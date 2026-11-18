using Microsoft.AspNetCore.Identity;

namespace StudentManagementApi.Services;

public sealed class IdentityRoleProvisioner(
    RoleManager<IdentityRole> roleManager,
    UserManager<IdentityUser> userManager)
{
    private static readonly string[] SupportedRoles = ["Admin", "Trainer", "Student"];

    public async Task ProvisionAsync(string userName, string role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(role);

        if (!SupportedRoles.Contains(role, StringComparer.Ordinal))
        {
            throw new ArgumentException("The requested role is not supported.", nameof(role));
        }

        var user = await userManager.FindByNameAsync(userName)
            ?? throw new InvalidOperationException("The specified user was not found.");

        foreach (var supportedRole in SupportedRoles)
        {
            if (await roleManager.RoleExistsAsync(supportedRole))
            {
                continue;
            }

            var createResult = await roleManager.CreateAsync(new IdentityRole(supportedRole));
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(error => error.Code));
                throw new InvalidOperationException(
                    $"Could not create role '{supportedRole}': {errors}");
            }
        }

        if (await userManager.IsInRoleAsync(user, role))
        {
            return;
        }

        var assignResult = await userManager.AddToRoleAsync(user, role);
        if (!assignResult.Succeeded)
        {
            var errors = string.Join(", ", assignResult.Errors.Select(error => error.Code));
            throw new InvalidOperationException($"Could not assign role '{role}': {errors}");
        }
    }
}
