namespace LF.WebApi.Common;

// Values of VK ID's authorize-page "provider" parameter (VKCOM/vkid-web-sdk OAuthName): one VK ID app
// signs users in with VK, Mail.ru or OK depending on which one is passed.
internal static class VkIdProviders
{
    public const string PropertiesKey = "lf.vkid.provider";

    public const string Vk = "vkid";
    public const string MailRu = "mail_ru";
    public const string Ok = "ok_ru";

    public static string Normalize(string? provider) =>
        provider is MailRu or Ok ? provider : Vk;
}
