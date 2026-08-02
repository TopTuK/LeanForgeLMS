using System.ComponentModel.DataAnnotations;

namespace LF.WebApi.Models.Options;

public class PmiAuthOptions : AuthOptions
{
    public const string SectionName = "PmiAuth";

    [Required]
    public string ClientId { get; set; } = null!;

    [Required]
    public string ClientSecret { get; set; } = null!;

    [Required]
    public string OpenIdConfigurationUrl { get; set; } = null!;

    [Required]
    public string CallbackPath { get; set; } = null!;
}
