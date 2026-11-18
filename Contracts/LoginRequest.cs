using System.ComponentModel.DataAnnotations;

namespace StudentManagementApi.Contracts;

/// <summary>Credentials used to authenticate an existing user.</summary>
public sealed record LoginRequest
{
    [Required]
    [StringLength(256)]
    public required string UserName { get; init; }

    [Required]
    public required string Password { get; init; }
}
