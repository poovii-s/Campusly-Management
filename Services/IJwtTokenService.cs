using Microsoft.AspNetCore.Identity;

namespace StudentManagementApi.Services;

public interface IJwtTokenService
{
    IssuedAccessToken CreateAccessToken(IdentityUser user, IEnumerable<string> roles);
}

public sealed record IssuedAccessToken(string Token, DateTimeOffset ExpiresAt);
