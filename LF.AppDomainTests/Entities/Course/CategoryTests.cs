using LF.AppDomain.Entities.Course;

namespace LF.AppDomainTests.Entities.CourseAggregate;

public class CategoryTests
{
    [Fact]
    public void Create_EmptyName_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Category.Create("   "));
    }

    [Fact]
    public void Create_TrimsName()
    {
        // Act
        var category = Category.Create("  Backend  ");

        // Assert
        Assert.Equal("Backend", category.Name);
    }

    [Fact]
    public void Rename_TrimsWhitespace()
    {
        // Arrange
        var category = Category.Create("Backend");

        // Act
        category.Rename("  Frontend  ");

        // Assert
        Assert.Equal("Frontend", category.Name);
    }

    [Fact]
    public void Rename_EmptyName_Throws()
    {
        // Arrange
        var category = Category.Create("Backend");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => category.Rename(""));
    }

    [Fact]
    public void Create_Default_IsFalse()
    {
        // Act
        var category = Category.Create("Backend");

        // Assert
        Assert.False(category.IsDefault);
    }

    [Fact]
    public void Create_IsDefaultTrue_SetsIsDefault()
    {
        // Act
        var category = Category.Create("Common", isDefault: true);

        // Assert
        Assert.True(category.IsDefault);
    }
}
