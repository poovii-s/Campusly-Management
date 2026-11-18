namespace StudentManagementApi.Contracts;

/// <summary>Safe account details returned after registration.</summary>
public sealed record RegistrationResponse(string UserId, string UserName, string Email);
