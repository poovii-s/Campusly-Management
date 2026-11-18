using System.ComponentModel.DataAnnotations;

namespace StudentManagementApi.Contracts;

/// <summary>Credentials and account details for a new user registration.</summary>
public sealed record RegisterRequest
{
    [Required]
    [StringLength(256)]
    public required string UserName { get; init; }

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public required string Email { get; init; }

    [Required]
    public required string Password { get; init; }
}
