using System.ComponentModel.DataAnnotations;

namespace StudentManagementApi.Contracts;

/// <summary>Requests a supported role assignment for an existing user.</summary>
public sealed record AssignUserRoleRequest
{
    [Required]
    [StringLength(32)]
    public required string Role { get; init; }
}
