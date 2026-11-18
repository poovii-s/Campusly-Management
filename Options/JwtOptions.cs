using System.ComponentModel.DataAnnotations;
using Microsoft.IdentityModel.Tokens;

namespace StudentManagementApi.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const string SigningAlgorithm = SecurityAlgorithms.HmacSha256;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Required]
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int AccessTokenLifetimeMinutes { get; set; } = 15;

    public bool HasValidSigningKey
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SigningKey))
            {
                return false;
            }

            try
            {
                return Convert.FromBase64String(SigningKey).Length >= 32;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
