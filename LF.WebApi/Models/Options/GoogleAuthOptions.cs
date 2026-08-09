using System.ComponentModel.DataAnnotations;

namespace LF.WebApi.Models.Options;

public class GoogleAuthOptions : AuthOptions
{
    public const string SectionName = "GoogleAuth";

    [Required]
    public string ClientId { get; set; } = null!;

    [Required]
    public string ClientSecret { get; set; } = null!;

    [Required]
    public string CallbackPath { get; set; } = null!;
}
