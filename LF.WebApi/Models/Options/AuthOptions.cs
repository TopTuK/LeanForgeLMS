using System.ComponentModel.DataAnnotations;

namespace LF.WebApi.Models.Options;

public class AuthOptions
{
    [Required]
    public string SchemeName { get; set; } = string.Empty;

    [Required]
    public string RedirectUri { get; set; } = string.Empty;
}
