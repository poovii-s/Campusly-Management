using Microsoft.AspNetCore.Identity;

namespace StudentManagementApi.Models;

public sealed class UserLoginActivity
{
    public int Id { get; set; }

    public required string UserId { get; set; }

    public DateOnly ActivityDateUtc { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }

    public IdentityUser? User { get; set; }
}
