using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using StudentManagementApi.Contracts;
using StudentManagementApi.Data;
using StudentManagementApi.Models;
using StudentManagementApi.Services;

namespace StudentManagementApi.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager,
    ApplicationDbContext context,
    IJwtTokenService jwtTokenService) : ControllerBase
{
    [HttpPost("login", Name = "LoginUser")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByNameAsync(request.UserName);
        if (user is null)
        {
            return InvalidCredentials();
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);

        if (!signInResult.Succeeded)
        {
            return InvalidCredentials();
        }

        cancellationToken.ThrowIfCancellationRequested();

        var roles = await userManager.GetRolesAsync(user);
        cancellationToken.ThrowIfCancellationRequested();

        var accessToken = jwtTokenService.CreateAccessToken(user, roles);
        var occurredAtUtc = DateTimeOffset.UtcNow;
        context.UserLoginActivities.Add(new UserLoginActivity
        {
            UserId = user.Id,
            ActivityDateUtc = DateOnly.FromDateTime(occurredAtUtc.UtcDateTime),
            OccurredAtUtc = occurredAtUtc
        });
        await context.SaveChangesAsync(cancellationToken);

        return Ok(new LoginResponse(accessToken.Token, accessToken.ExpiresAt));
    }

    [HttpPost("register", Name = "RegisterUser")]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegistrationResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = new IdentityUser
        {
            UserName = request.UserName,
            Email = request.Email
        };

        IdentityResult result;
        try
        {
            result = await userManager.CreateAsync(user, request.Password);
        }
        catch (DbUpdateException exception) when (
            exception.GetBaseException() is SqlException { Number: 2601 or 2627 })
        {
            return RegistrationConflict();
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (result.Succeeded)
        {
            return StatusCode(
                StatusCodes.Status201Created,
                new RegistrationResponse(user.Id, user.UserName!, user.Email!));
        }

        if (result.Errors.Any(error =>
                error.Code is "DuplicateUserName" or "DuplicateEmail"))
        {
            return RegistrationConflict();
        }

        var errors = result.Errors
            .GroupBy(error => error.Code)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Description).ToArray());

        return ValidationProblem(new ValidationProblemDetails(errors));
    }

    private ConflictObjectResult RegistrationConflict() =>
        Conflict(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Registration conflict",
            Detail = "The username or email address is already in use.",
            Instance = Request.Path
        });

    private UnauthorizedObjectResult InvalidCredentials() =>
        Unauthorized(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Authentication failed",
            Detail = "The supplied credentials are invalid.",
            Instance = Request.Path
        });
}
