using LF.AppDomain.Entities.User;
using LF.AppDomain.Models.User.Enums;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.User;
using LF.Application.Services.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;

namespace LF.ApplicationTests.Services.User;

public class UserServiceTests
{
    private static UserService CreateService(
        IReadOnlyCollection<DbUser> users,
        out Mock<IAppDbContext> dbContextMock,
        out Mock<DbSet<DbUser>> usersMock)
    {
        usersMock = users.ToList().BuildMockDbSet();

        dbContextMock = new Mock<IAppDbContext>();
        dbContextMock.SetupGet(c => c.Users).Returns(usersMock.Object);
        dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return new UserService(NullLogger<UserService>.Instance, dbContextMock.Object);
    }

    [Fact]
    public async Task GetOrCreateUserAsync_UserDoesNotExist_CreatesUserWithStudentRole()
    {
        // Arrange
        var service = CreateService([], out var dbContextMock, out var usersMock);
        var request = new GetOrCreateUserDto { Email = "new@example.com", FirstName = "Ada", LastName = "Lovelace" };

        // Act
        var result = await service.GetOrCreateUserAsync(request);

        // Assert
        Assert.Equal(request.Email, result.Email);
        Assert.Equal(request.FirstName, result.FirstName);
        Assert.Equal(request.LastName, result.LastName);
        Assert.Equal(UserRole.Student, result.Role);
        usersMock.Verify(m => m.Add(It.IsAny<DbUser>()), Times.Once);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateUserAsync_UserExists_ReturnsExistingWithoutInserting()
    {
        // Arrange
        var existing = new DbUser { Id = 1, Email = "existing@example.com", FirstName = "Grace", LastName = "Hopper", Role = UserRole.Instructor };
        var service = CreateService([existing], out var dbContextMock, out var usersMock);
        var request = new GetOrCreateUserDto { Email = existing.Email, FirstName = "Ignored", LastName = "Ignored" };

        // Act
        var result = await service.GetOrCreateUserAsync(request);

        // Assert
        Assert.Equal(existing.Id, result.Id);
        Assert.Equal(existing.FirstName, result.FirstName);
        Assert.Equal(UserRole.Instructor, result.Role);
        usersMock.Verify(m => m.Add(It.IsAny<DbUser>()), Times.Never);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetUserByIdAsync_UserExists_ReturnsDto()
    {
        // Arrange
        var existing = new DbUser { Id = 42, Email = "a@b.com", FirstName = "A", LastName = "B", Role = UserRole.Admin };
        var service = CreateService([existing], out _, out _);

        // Act
        var result = await service.GetUserByIdAsync(42);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existing.Email, result!.Email);
        Assert.Equal(existing.Role, result.Role);
    }

    [Fact]
    public async Task GetUserByIdAsync_UserDoesNotExist_ReturnsNull()
    {
        // Arrange
        var service = CreateService([], out _, out _);

        // Act
        var result = await service.GetUserByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateUserNameAsync_UserExists_UpdatesNameAndSaves()
    {
        // Arrange
        var existing = new DbUser { Id = 7, Email = "u@x.com", FirstName = "Old", LastName = "Name", Role = UserRole.Student };
        var service = CreateService([existing], out var dbContextMock, out _);
        var dto = new UpdateUserProfileDto { FirstName = "New", LastName = "Name2" };

        // Act
        var result = await service.UpdateUserNameAsync(7, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New", result!.FirstName);
        Assert.Equal("Name2", result.LastName);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserNameAsync_UserDoesNotExist_ReturnsNull()
    {
        // Arrange
        var service = CreateService([], out var dbContextMock, out _);

        // Act
        var result = await service.UpdateUserNameAsync(123, new UpdateUserProfileDto { FirstName = "X" });

        // Assert
        Assert.Null(result);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserNameAsync_NoChanges_DoesNotSave()
    {
        // Arrange
        var existing = new DbUser { Id = 7, Email = "u@x.com", FirstName = "Ada", LastName = "Lovelace", Role = UserRole.Student };
        var service = CreateService([existing], out var dbContextMock, out _);
        var dto = new UpdateUserProfileDto { FirstName = "Ada", LastName = "Lovelace" };

        // Act
        var result = await service.UpdateUserNameAsync(7, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Ada", result!.FirstName);
        Assert.Equal("Lovelace", result.LastName);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserNameAsync_OnlyDescriptionChanged_SavesAndKeepsName()
    {
        // Arrange
        var existing = new DbUser { Id = 7, Email = "u@x.com", FirstName = "Ada", LastName = "Lovelace", Description = null };
        var service = CreateService([existing], out var dbContextMock, out _);
        var dto = new UpdateUserProfileDto { FirstName = "Ada", LastName = "Lovelace", Description = "Backend engineer." };

        // Act
        var result = await service.UpdateUserNameAsync(7, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Ada", result!.FirstName);
        Assert.Equal("Backend engineer.", result.Description);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserNameAsync_NameAndDescriptionUnchanged_DoesNotSave()
    {
        // Arrange
        var existing = new DbUser { Id = 7, Email = "u@x.com", FirstName = "Ada", LastName = "Lovelace", Description = "Bio." };
        var service = CreateService([existing], out var dbContextMock, out _);
        var dto = new UpdateUserProfileDto { FirstName = "Ada", LastName = "Lovelace", Description = "Bio." };

        // Act
        var result = await service.UpdateUserNameAsync(7, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Bio.", result!.Description);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserNameAsync_NullLastNameMatchingEmptyString_DoesNotSave()
    {
        // Arrange
        var existing = new DbUser { Id = 7, Email = "u@x.com", FirstName = "Ada", LastName = string.Empty };
        var service = CreateService([existing], out var dbContextMock, out _);
        var dto = new UpdateUserProfileDto { FirstName = "Ada", LastName = null };

        // Act
        var result = await service.UpdateUserNameAsync(7, dto);

        // Assert
        Assert.NotNull(result);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserNameAsync_EmptyFirstName_ThrowsArgumentException()
    {
        // Arrange
        var existing = new DbUser { Id = 1, Email = "a@b.com", FirstName = "Old", LastName = "Name" };
        var service = CreateService([existing], out _, out _);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateUserNameAsync(1, new UpdateUserProfileDto { FirstName = "   " }));
    }

    [Fact]
    public async Task UpdateUserAvatarAsync_UserExists_SetsAvatarKeyAndSaves()
    {
        // Arrange
        var existing = new DbUser { Id = 7, Email = "u@x.com", FirstName = "Ada", AvatarKey = null };
        var service = CreateService([existing], out var dbContextMock, out _);
        var dto = new UpdateUserAvatarDto { AvatarKey = "avatars/7/new.png" };

        // Act
        var result = await service.UpdateUserAvatarAsync(7, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("avatars/7/new.png", result!.AvatarKey);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAvatarAsync_NullAvatarKey_ClearsAvatarAndSaves()
    {
        // Arrange
        var existing = new DbUser { Id = 7, Email = "u@x.com", FirstName = "Ada", AvatarKey = "avatars/7/old.png" };
        var service = CreateService([existing], out var dbContextMock, out _);
        var dto = new UpdateUserAvatarDto { AvatarKey = null };

        // Act
        var result = await service.UpdateUserAvatarAsync(7, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result!.AvatarKey);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAvatarAsync_UserDoesNotExist_ReturnsNull()
    {
        // Arrange
        var service = CreateService([], out var dbContextMock, out _);

        // Act
        var result = await service.UpdateUserAvatarAsync(123, new UpdateUserAvatarDto { AvatarKey = "avatars/123/x.png" });

        // Assert
        Assert.Null(result);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserAvatarAsync_WhitespaceAvatarKey_ThrowsArgumentException()
    {
        // Arrange
        var existing = new DbUser { Id = 1, Email = "a@b.com", FirstName = "Ada" };
        var service = CreateService([existing], out _, out _);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateUserAvatarAsync(1, new UpdateUserAvatarDto { AvatarKey = "   " }));
    }

    [Fact]
    public async Task EnsureUserWithRoleAsync_UserDoesNotExist_CreatesUserWithGivenRole()
    {
        // Arrange
        var service = CreateService([], out var dbContextMock, out var usersMock);
        var request = new EnsureUserWithRoleDto { Email = "dev.instructor@leanforge.local", FirstName = "Dev", LastName = "Instructor", Role = UserRole.Instructor };

        // Act
        var result = await service.EnsureUserWithRoleAsync(request);

        // Assert
        Assert.Equal(request.Email, result.Email);
        Assert.Equal(request.FirstName, result.FirstName);
        Assert.Equal(UserRole.Instructor, result.Role);
        usersMock.Verify(m => m.Add(It.IsAny<DbUser>()), Times.Once);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnsureUserWithRoleAsync_UserExistsWithDifferentRole_UpdatesRoleAndSaves()
    {
        // Arrange
        var existing = new DbUser { Id = 3, Email = "dev.student@leanforge.local", FirstName = "Dev", LastName = "Student", Role = UserRole.Student };
        var service = CreateService([existing], out var dbContextMock, out var usersMock);
        var request = new EnsureUserWithRoleDto { Email = existing.Email, FirstName = "Ignored", LastName = "Ignored", Role = UserRole.CourseCreator };

        // Act
        var result = await service.EnsureUserWithRoleAsync(request);

        // Assert
        Assert.Equal(existing.Id, result.Id);
        Assert.Equal(UserRole.CourseCreator, result.Role);
        usersMock.Verify(m => m.Add(It.IsAny<DbUser>()), Times.Never);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnsureUserWithRoleAsync_UserExistsWithSameRole_DoesNotSave()
    {
        // Arrange
        var existing = new DbUser { Id = 3, Email = "dev.student@leanforge.local", FirstName = "Dev", LastName = "Student", Role = UserRole.Student };
        var service = CreateService([existing], out var dbContextMock, out var usersMock);
        var request = new EnsureUserWithRoleDto { Email = existing.Email, FirstName = "Dev", LastName = "Student", Role = UserRole.Student };

        // Act
        var result = await service.EnsureUserWithRoleAsync(request);

        // Assert
        Assert.Equal(UserRole.Student, result.Role);
        usersMock.Verify(m => m.Add(It.IsAny<DbUser>()), Times.Never);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListUsersAsync_ReturnsPagedResults()
    {
        // Arrange
        var users = Enumerable.Range(1, 25)
            .Select(i => new DbUser { Id = i, Email = $"user{i}@x.com", FirstName = $"User{i}", LastName = "Test" })
            .ToList();
        var service = CreateService(users, out _, out _);

        // Act
        var result = await service.ListUsersAsync(page: 2, pageSize: 10, search: null);

        // Assert
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(10, result.Items.Count);
        Assert.Equal(11, result.Items[0].Id);
    }

    [Fact]
    public async Task ListUsersAsync_WithSearch_FiltersByNameOrEmail()
    {
        // Arrange
        var users = new List<DbUser>
        {
            new() { Id = 1, Email = "ada@x.com", FirstName = "Ada", LastName = "Lovelace" },
            new() { Id = 2, Email = "grace@x.com", FirstName = "Grace", LastName = "Hopper" },
        };
        var service = CreateService(users, out _, out _);

        // Act
        var result = await service.ListUsersAsync(page: 1, pageSize: 10, search: "ada");

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("ada@x.com", result.Items[0].Email);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_UserExists_UpdatesRoleAndSaves()
    {
        // Arrange
        var existing = new DbUser { Id = 5, Email = "u@x.com", FirstName = "U", Role = UserRole.Student };
        var service = CreateService([existing], out var dbContextMock, out _);

        // Act
        var result = await service.UpdateUserRoleAsync(5, new UpdateUserRoleDto { Role = UserRole.Instructor });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(UserRole.Instructor, result!.Role);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_UserDoesNotExist_ReturnsNull()
    {
        // Arrange
        var service = CreateService([], out var dbContextMock, out _);

        // Act
        var result = await service.UpdateUserRoleAsync(999, new UpdateUserRoleDto { Role = UserRole.Admin });

        // Assert
        Assert.Null(result);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_SameRole_DoesNotSave()
    {
        // Arrange
        var existing = new DbUser { Id = 5, Email = "u@x.com", FirstName = "U", Role = UserRole.Instructor };
        var service = CreateService([existing], out var dbContextMock, out _);

        // Act
        var result = await service.UpdateUserRoleAsync(5, new UpdateUserRoleDto { Role = UserRole.Instructor });

        // Assert
        Assert.NotNull(result);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteUserAsync_UserExists_RemovesAndSaves()
    {
        // Arrange
        var existing = new DbUser { Id = 9, Email = "u@x.com", FirstName = "U" };
        var service = CreateService([existing], out var dbContextMock, out var usersMock);

        // Act
        var result = await service.DeleteUserAsync(9);

        // Assert
        Assert.True(result);
        usersMock.Verify(m => m.Remove(existing), Times.Once);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_UserDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var service = CreateService([], out var dbContextMock, out _);

        // Act
        var result = await service.DeleteUserAsync(999);

        // Assert
        Assert.False(result);
        dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
