using QuestPDF.Drawing;
using QuestPDF.Infrastructure;

namespace LF.Infrastructure.Services.Documents;

// QuestPDF's license and font registry are process-wide statics, so they are set exactly once.
internal static class QuestPdfSetup
{
    public const string BodyFont = "Noto Sans";
    public const string MonoFont = "Noto Sans Mono";

    private const string FontResourcePrefix = "LF.Fonts.";

    private static readonly Lazy<bool> Configured = new(Configure, LazyThreadSafetyMode.ExecutionAndPublication);

    public static void EnsureConfigured() => _ = Configured.Value;

    private static bool Configure()
    {
        // Community license: free for organizations under $1M annual revenue (https://www.questpdf.com/license/).
        QuestPDF.Settings.License = LicenseType.Community;

        // Embedded Noto fonts cover Latin and Cyrillic, so output is identical on a dev box and in the
        // font-less Debian runtime image. A glyph neither font has (e.g. emoji) is drawn as a box instead of failing the export.
        QuestPDF.Settings.UseSystemFonts = false;
        QuestPDF.Settings.ThrowOnMissingTextGlyphs = false;

        var assembly = typeof(QuestPdfSetup).Assembly;
        foreach (var resource in assembly.GetManifestResourceNames().Where(n => n.StartsWith(FontResourcePrefix, StringComparison.Ordinal)))
        {
            using var stream = assembly.GetManifestResourceStream(resource)
                ?? throw new InvalidOperationException($"Font resource '{resource}' is missing.");
            FontManager.RegisterFontFromStream(stream);
        }

        return true;
    }
}
