namespace LF.Application.Common.Options;

// Public URL of the SPA/LF.WebApi, used to build links in outgoing emails. Hosts that compose
// emails bind this from the "App" section and validate it on start.
public sealed class AppUrlOptions
{
    public const string SectionName = "App";

    public string PublicBaseUrl { get; set; } = null!;

    public bool IsValid =>
        Uri.TryCreate(PublicBaseUrl, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);

    public string Link(string relativePath) => $"{PublicBaseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";
}
