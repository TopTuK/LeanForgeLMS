using LF.Application.Common;
using LF.Application.Common.Interfaces;
using LF.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LF.WebApiTests;

/// <summary>
/// Guards the fail-closed contract of <c>AddInfrastructureFeatureFlags</c>. The Unleash types are
/// internal to LF.Infrastructure, so these assert observable behaviour through
/// <see cref="IFeatureFlagService"/> rather than concrete types.
/// </summary>
public class FeatureFlagRegistrationTests
{
    // Only ApplicationName is read (it becomes the Unleash AppName), so the rest stays minimal.
    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "LF.WebApiTests";
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static ServiceProvider BuildProvider(params (string Key, string? Value)[] settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.Select(s => new KeyValuePair<string, string?>(s.Key, s.Value)))
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IHostEnvironment>(new StubHostEnvironment());
        services.AddInfrastructureFeatureFlags(configuration);

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    [Fact]
    public async Task AddInfrastructureFeatureFlags_NoConfiguration_ResolvesAndReportsFlagsDisabled()
    {
        // Arrange
        using var provider = BuildProvider();

        // Act
        var featureFlags = provider.GetRequiredService<IFeatureFlagService>();
        var enabled = await featureFlags.IsEnabledAsync(
            FeatureFlags.SelfEnrollment, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.False(enabled);
    }

    [Fact]
    public async Task AddInfrastructureFeatureFlags_PlaceholderApiKey_ReportsFlagsDisabled()
    {
        // Arrange
        // appsettings.json ships "CHANGE_ME"; an environment that never overrides it must not be
        // treated as configured, or the Unleash client would try to authenticate with the literal.
        using var provider = BuildProvider(
            ("Unleash:ApiUrl", "https://feature.example.com/api/"),
            ("Unleash:ApiKey", "CHANGE_ME"));

        // Act
        var enabled = await provider.GetRequiredService<IFeatureFlagService>()
            .IsEnabledAsync(FeatureFlags.SelfEnrollment, userId: 42, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(enabled);
    }

    [Fact]
    public async Task AddInfrastructureFeatureFlags_ApiUrlWithoutKey_ReportsFlagsDisabled()
    {
        // Arrange
        using var provider = BuildProvider(("Unleash:ApiUrl", "https://feature.example.com/api/"));

        // Act
        var enabled = await provider.GetRequiredService<IFeatureFlagService>()
            .IsEnabledAsync(FeatureFlags.SelfEnrollment, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.False(enabled);
    }
}
