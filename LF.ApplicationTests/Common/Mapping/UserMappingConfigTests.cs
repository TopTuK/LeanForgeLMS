using LF.AppDomain.Entities.User;
using LF.AppDomain.Models.User.Enums;
using LF.Application.Common.Mapping;
using LF.Application.ModelDto.User;
using Mapster;

namespace LF.ApplicationTests.Common.Mapping;

public class UserMappingConfigTests
{
    private static TypeAdapterConfig CreateConfig()
    {
        var config = new TypeAdapterConfig();
        new UserMappingConfig().Register(config);
        return config;
    }

    [Fact]
    public void Adapt_GetOrCreateUserDtoToDbUser_MapsFieldsAndLeavesIgnoredPropertiesAtDefault()
    {
        // Arrange
        var config = CreateConfig();
        var dto = new GetOrCreateUserDto { Email = "a@b.com", FirstName = "Ada", LastName = "Lovelace" };

        // Act
        var result = dto.Adapt<DbUser>(config);

        // Assert
        Assert.Equal(dto.Email, result.Email);
        Assert.Equal(dto.FirstName, result.FirstName);
        Assert.Equal(dto.LastName, result.LastName);
        Assert.Equal(0, result.Id);
        Assert.Equal(UserRole.None, result.Role);
        Assert.Null(result.AvatarKey);
    }

    [Fact]
    public void Adapt_GetOrCreateUserDtoToDbUser_NullLastName_MapsToEmptyString()
    {
        // Arrange
        var config = CreateConfig();
        var dto = new GetOrCreateUserDto { Email = "a@b.com", FirstName = "Ada", LastName = null };

        // Act
        var result = dto.Adapt<DbUser>(config);

        // Assert
        Assert.Equal(string.Empty, result.LastName);
    }

    [Fact]
    public void Adapt_DbUserToUserDto_MapsAllFields()
    {
        // Arrange
        var config = CreateConfig();
        var user = new DbUser
        {
            Id = 3,
            Email = "a@b.com",
            FirstName = "Ada",
            LastName = "Lovelace",
            Role = UserRole.Instructor,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            AvatarKey = "avatars/3/photo.png",
        };

        // Act
        var result = user.Adapt<UserDto>(config);

        // Assert
        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal(user.FirstName, result.FirstName);
        Assert.Equal(user.LastName, result.LastName);
        Assert.Equal(user.Role, result.Role);
        Assert.Equal(user.CreatedAt, result.CreatedAt);
        Assert.Equal(user.AvatarKey, result.AvatarKey);
    }

    [Fact]
    public void Adapt_DbUserToUserDto_NoAvatar_MapsNull()
    {
        // Arrange
        var config = CreateConfig();
        var user = new DbUser { Id = 3, Email = "a@b.com", FirstName = "Ada", AvatarKey = null };

        // Act
        var result = user.Adapt<UserDto>(config);

        // Assert
        Assert.Null(result.AvatarKey);
    }
}
