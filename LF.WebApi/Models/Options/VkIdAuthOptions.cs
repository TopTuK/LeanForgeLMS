using System.ComponentModel.DataAnnotations;

namespace LF.WebApi.Models.Options;

public class VkIdAuthOptions : AuthOptions
{
    public const string SectionName = "VkIdAuth";

    [Required]
    public string ClientId { get; set; } = null!;

    [Required]
    public string ClientSecret { get; set; } = null!;

    [Required]
    public string CallbackPath { get; set; } = null!;
}
