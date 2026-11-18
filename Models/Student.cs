using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;

namespace StudentManagementApi.Models;

public class Student
{
    public int Id { get; set; }

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? CourseName { get; set; }

    public DateOnly DateOfBirth { get; set; }

    public DateOnly? EnrollmentDate { get; set; }

    [JsonIgnore]
    public string? StudentUserId { get; set; }

    [JsonIgnore]
    public IdentityUser? StudentUser { get; set; }

    [JsonIgnore]
    public string? TrainerUserId { get; set; }

    [JsonIgnore]
    public IdentityUser? TrainerUser { get; set; }
}