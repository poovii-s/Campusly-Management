namespace StudentManagementApi.Contracts;

/// <summary>JWT access token returned after successful authentication.</summary>
public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt);
