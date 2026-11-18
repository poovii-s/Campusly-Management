namespace StudentManagementApi.Contracts;

/// <summary>Safe user details and assigned roles returned to an administrator.</summary>
public sealed record AdminUserResponse(
    string UserId,
    string? UserName,
    string? Email,
    IReadOnlyList<string> Roles);
