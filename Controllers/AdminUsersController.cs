using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementApi.Contracts;
using StudentManagementApi.Data;

namespace StudentManagementApi.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/users")]
public sealed class AdminUsersController(
    ApplicationDbContext context,
    UserManager<IdentityUser> userManager,
    RoleManager<IdentityRole> roleManager) : ControllerBase
{
    private static readonly string[] SupportedRoles = ["Admin", "Trainer", "Student"];

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AdminUserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminUserResponse>>> GetUsers(
        CancellationToken cancellationToken)
    {
        var users = await userManager.Users
            .OrderBy(user => user.UserName)
            .ToListAsync(cancellationToken);

        var responses = new List<AdminUserResponse>(users.Count);
        foreach (var user in users)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var roles = await userManager.GetRolesAsync(user);
            responses.Add(ToResponse(user, roles));
        }

        return Ok(responses);
    }

    [HttpPut("{userId}/role")]
    [ProducesResponseType(typeof(AdminUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminUserResponse>> AssignRole(
        string userId,
        AssignUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        if (!SupportedRoles.Contains(request.Role, StringComparer.Ordinal))
        {
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    [nameof(request.Role)] = ["Choose Admin, Trainer, or Student."]
                }));
        }

        if (!await roleManager.RoleExistsAsync(request.Role))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Role is not provisioned",
                Detail = "The requested role must be created through the trusted Identity provisioning process.",
                Instance = Request.Path
            });
        }

        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "User not found",
                Instance = Request.Path
            });
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var unsupportedRoles = currentRoles
            .Where(role => !SupportedRoles.Contains(role, StringComparer.Ordinal))
            .ToArray();
        if (unsupportedRoles.Length > 0)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "User has an unmanaged role",
                Detail = "Resolve the user's roles through a trusted process before changing them here.",
                Instance = Request.Path
            });
        }

        if (currentRoles.Contains("Admin", StringComparer.Ordinal) &&
            !string.Equals(request.Role, "Admin", StringComparison.Ordinal))
        {
            var administrators = await userManager.GetUsersInRoleAsync("Admin");
            if (administrators.Count <= 1)
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Last administrator cannot be demoted",
                    Detail = "Assign another administrator before changing this user's Admin role.",
                    Instance = Request.Path
                });
            }
        }

        var removalResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removalResult.Succeeded)
        {
            return IdentityValidationProblem(removalResult);
        }

        var assignmentResult = await userManager.AddToRoleAsync(user, request.Role);
        if (!assignmentResult.Succeeded)
        {
            return IdentityValidationProblem(assignmentResult);
        }

        await transaction.CommitAsync(cancellationToken);
        return Ok(ToResponse(user, [request.Role]));
    }

    private static AdminUserResponse ToResponse(IdentityUser user, IEnumerable<string> roles) =>
        new(
            user.Id,
            user.UserName,
            user.Email,
            roles.OrderBy(role => role, StringComparer.Ordinal).ToArray());

    private ActionResult<AdminUserResponse> IdentityValidationProblem(IdentityResult result)
    {
        var errors = result.Errors
            .GroupBy(error => error.Code)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Description).ToArray());

        return ValidationProblem(new ValidationProblemDetails(errors));
    }
}
