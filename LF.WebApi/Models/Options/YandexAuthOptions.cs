using System.ComponentModel.DataAnnotations;

namespace LF.WebApi.Models.Options;

public class YandexAuthOptions : AuthOptions
{
    public const string SectionName = "YandexAuth";

    [Required]
    public string ClientId { get; set; } = null!;

    [Required]
    public string ClientSecret { get; set; } = null!;

    [Required]
    public string CallbackPath { get; set; } = null!;
}
