using LF.WebApi.Endpoints;

namespace LF.WebApiTests.Endpoints;

/// <summary>
/// Guards the contract that <c>Program.MapEndpointGroups</c> relies on: every concrete
/// <see cref="IEndpointGroup"/> in the LF.WebApi assembly must be discoverable and constructible
/// with a parameterless constructor via <see cref="Activator.CreateInstance(Type)"/>.
/// </summary>
public class EndpointGroupDiscoveryTests
{
    private static Type[] ConcreteEndpointGroups() =>
        [.. typeof(ProfileEndpoints).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false } && typeof(IEndpointGroup).IsAssignableFrom(t))];

    [Fact]
    public void AllKnownEndpointGroups_AreDiscovered()
    {
        var groups = ConcreteEndpointGroups();

        Assert.Contains(typeof(ProfileEndpoints), groups);
        Assert.Contains(typeof(CourseEndpoints), groups);
        Assert.Contains(typeof(EnrollmentEndpoints), groups);
        Assert.Contains(typeof(AdminUserEndpoints), groups);
        Assert.Contains(typeof(AdminCategoryEndpoints), groups);
        Assert.Contains(typeof(AdminPlatformSettingsEndpoints), groups);
        Assert.Contains(typeof(AdminPaymentReportEndpoints), groups);
        Assert.Contains(typeof(PlatformEndpoints), groups);
        Assert.Contains(typeof(DevAuthEndpoints), groups);
    }

    [Fact]
    public void EveryEndpointGroup_HasParameterlessConstructor()
    {
        foreach (var type in ConcreteEndpointGroups())
        {
            var instance = Activator.CreateInstance(type);
            Assert.IsAssignableFrom<IEndpointGroup>(instance);
        }
    }
}
