using LF.AppDomain.Entities.User;

namespace LF.AppDomainTests.Entities.User;

public class DbUserTests
{
    [Fact]
    public void UpdateDescription_NewValue_ReturnsTrueAndSetsDescription()
    {
        // Arrange
        var user = new DbUser { Description = null };

        // Act
        var changed = user.UpdateDescription("Backend engineer.");

        // Assert
        Assert.True(changed);
        Assert.Equal("Backend engineer.", user.Description);
    }

    [Fact]
    public void UpdateDescription_SameValue_ReturnsFalse()
    {
        // Arrange
        var user = new DbUser { Description = "Backend engineer." };

        // Act
        var changed = user.UpdateDescription("Backend engineer.");

        // Assert
        Assert.False(changed);
    }

    [Fact]
    public void UpdateDescription_WhitespaceOrNull_ClearsToNull()
    {
        // Arrange
        var user = new DbUser { Description = "Backend engineer." };

        // Act
        var changed = user.UpdateDescription("   ");

        // Assert
        Assert.True(changed);
        Assert.Null(user.Description);
    }

    [Fact]
    public void UpdateDescription_TrimsWhitespace()
    {
        // Arrange
        var user = new DbUser { Description = null };

        // Act
        user.UpdateDescription("  Backend engineer.  ");

        // Assert
        Assert.Equal("Backend engineer.", user.Description);
    }
}
