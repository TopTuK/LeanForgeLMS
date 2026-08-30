using LF.AppDomain.Entities.Course;
using LF.AppDomain.Models.Course.Enums;
using LF.ApplicationTests.TestSupport;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.Promo;
using LF.Application.Services.Promo;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using DomainCourse = LF.AppDomain.Entities.Course.Course;

namespace LF.ApplicationTests.Services.Promo;

public class PromoCodeServiceTests
{
    private static PromoCodeService CreateService(
        IReadOnlyCollection<DomainCourse> courses,
        IReadOnlyCollection<PromoCode> promoCodes,
        out Mock<IAppDbContext> dbContextMock)
    {
        var coursesMock = courses.ToList().BuildMockDbSet();
        var promoCodesMock = promoCodes.ToList().BuildMockDbSet();

        dbContextMock = new Mock<IAppDbContext>();
        dbContextMock.SetupGet(c => c.Courses).Returns(coursesMock.Object);
        dbContextMock.SetupGet(c => c.PromoCodes).Returns(promoCodesMock.Object);
        dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return new PromoCodeService(NullLogger<PromoCodeService>.Instance, dbContextMock.Object, TimeProvider.System);
    }

    private static DomainCourse CreatePaidCourse(int id = 1)
    {
        var course = DomainCourse.Create("Paid", "Short", "Description", Category.Create("Backend"), 1, DateTime.UtcNow,
            CoursePricingType.Paid, 2000m, CourseEnrollmentMode.Open);
        EntityIdSetter.SetId(course, id);
        return course;
    }

    [Fact]
    public async Task CreatePromoCodeAsync_GlobalCode_Persists()
    {
        var service = CreateService([], [], out var dbContextMock);
        var dto = new CreatePromoCodeDto { Code = "welcome", DiscountType = PromoCodeDiscountType.Percentage, DiscountValue = 15m };

        var result = await service.CreatePromoCodeAsync(dto, createdByUserId: 1);

        Assert.Equal("WELCOME", result.Code);
        Assert.Equal(PromoCodeDiscountType.Percentage, result.DiscountType);
        Assert.Null(result.CourseId);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreatePromoCodeAsync_CourseScopedToFreeCourse_Throws()
    {
        var freeCourse = DomainCourse.Create("Free", "Short", "Description", Category.Create("Backend"), 1, DateTime.UtcNow);
        EntityIdSetter.SetId(freeCourse, 5);
        var service = CreateService([freeCourse], [], out _);
        var dto = new CreatePromoCodeDto { Code = "x", DiscountType = PromoCodeDiscountType.Percentage, DiscountValue = 10m, CourseId = 5 };

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreatePromoCodeAsync(dto, 1));
    }

    [Fact]
    public async Task CreatePromoCodeAsync_DuplicateCode_Throws()
    {
        var existing = PromoCode.Create("DUP", PromoCodeDiscountType.Percentage, 10m, null, null, null, 1, DateTime.UtcNow);
        var service = CreateService([CreatePaidCourse()], [existing], out _);
        var dto = new CreatePromoCodeDto { Code = "dup", DiscountType = PromoCodeDiscountType.Percentage, DiscountValue = 10m };

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreatePromoCodeAsync(dto, 1));
    }

    [Fact]
    public async Task DeactivatePromoCodeAsync_Existing_DeactivatesAndSaves()
    {
        var promo = PromoCode.Create("OFF", PromoCodeDiscountType.Percentage, 10m, null, null, null, 1, DateTime.UtcNow);
        EntityIdSetter.SetId(promo, 3);
        var service = CreateService([], [promo], out var dbContextMock);

        var result = await service.DeactivatePromoCodeAsync(3);

        Assert.True(result);
        Assert.False(promo.IsActive);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivatePromoCodeAsync_Missing_ReturnsFalse()
    {
        var service = CreateService([], [], out _);

        Assert.False(await service.DeactivatePromoCodeAsync(999));
    }
}
