using System.ComponentModel.DataAnnotations;

namespace LF.WebApi.Models.Options;

public class DefaultAuthOptions
{
    public const string SectionName = "DefaultAuth";

    public string AuthCookieName { get; set; } = "leanforge_api";
    public string TempAuthCookieName { get; set; } = "LeanForgeTempCookie";
    public int AuthMaxAgeDays { get; set; } = 7;

    [Required]
    public string JwtIssuer { get; set; } = null!;

    [Required]
    public string JwtAudience { get; set; } = null!;

    [Required]
    public string JwtKey { get; set; } = null!;

    public int JwtExpiresDays { get; set; } = 7;
}
