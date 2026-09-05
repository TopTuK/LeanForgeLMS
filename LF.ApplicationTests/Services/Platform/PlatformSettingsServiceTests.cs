using LF.AppDomain.Entities.Platform;
using LF.Application.Common.Interfaces;
using LF.Application.Services.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace LF.ApplicationTests.Services.Platform;

public class PlatformSettingsServiceTests
{
    private static PlatformSettingsService CreateService(
        IReadOnlyCollection<PlatformSettings> rows,
        out Mock<IAppDbContext> dbContextMock,
        out Mock<DbSet<PlatformSettings>> setMock)
    {
        var list = rows.ToList();
        setMock = list.BuildMockDbSet();

        dbContextMock = new Mock<IAppDbContext>();
        dbContextMock.SetupGet(c => c.PlatformSettings).Returns(setMock.Object);
        dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return new PlatformSettingsService(NullLogger<PlatformSettingsService>.Instance, dbContextMock.Object, TimeProvider.System);
    }

    [Fact]
    public async Task IsStudentEnrollmentEnabledAsync_NoRow_ReturnsFalse()
    {
        var service = CreateService([], out _, out _);

        Assert.False(await service.IsStudentEnrollmentEnabledAsync());
    }

    [Fact]
    public async Task IsStudentEnrollmentEnabledAsync_ReflectsRow()
    {
        var settings = PlatformSettings.CreateDefault(DateTime.UtcNow);
        settings.SetStudentEnrollmentEnabled(true, updatedByUserId: 1, DateTime.UtcNow);
        var service = CreateService([settings], out _, out _);

        Assert.True(await service.IsStudentEnrollmentEnabledAsync());
    }

    [Fact]
    public async Task GetAsync_NoRow_ReturnsDisabledDefault()
    {
        var service = CreateService([], out _, out _);

        var dto = await service.GetAsync();

        Assert.False(dto.StudentEnrollmentEnabled);
        Assert.Null(dto.UpdatedByUserId);
    }

    [Fact]
    public async Task SetStudentEnrollmentEnabledAsync_NoRow_CreatesAndPersists()
    {
        var service = CreateService([], out var dbContextMock, out var setMock);

        var dto = await service.SetStudentEnrollmentEnabledAsync(enabled: true, adminUserId: 9);

        Assert.True(dto.StudentEnrollmentEnabled);
        Assert.Equal(9, dto.UpdatedByUserId);
        setMock.Verify(s => s.Add(It.IsAny<PlatformSettings>()), Times.Once);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetStudentEnrollmentEnabledAsync_ExistingRow_UpdatesFlagAndAudit()
    {
        var settings = PlatformSettings.CreateDefault(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var service = CreateService([settings], out var dbContextMock, out var setMock);

        var dto = await service.SetStudentEnrollmentEnabledAsync(enabled: true, adminUserId: 4);

        Assert.True(dto.StudentEnrollmentEnabled);
        Assert.Equal(4, dto.UpdatedByUserId);
        Assert.True(dto.UpdatedAt > new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        setMock.Verify(s => s.Add(It.IsAny<PlatformSettings>()), Times.Never);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
