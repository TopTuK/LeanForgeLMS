using LF.AppDomain.Entities.Groups;

namespace LF.AppDomainTests.Entities.Groups;

public class StudentGroupTests
{
    private static readonly DateTime Now = new(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_TrimsNameAndBlanksEmptyDescription()
    {
        var group = StudentGroup.Create(courseId: 7, "  Stream A ", "   ", createdByUserId: 3, Now);

        Assert.Equal("Stream A", group.Name);
        Assert.Null(group.Description);
        Assert.Empty(group.Members);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyName_Throws(string name) =>
        Assert.Throws<ArgumentException>(() => StudentGroup.Create(7, name, null, 3, Now));

    [Fact]
    public void Create_OverlongName_Throws() =>
        Assert.Throws<ArgumentException>(() => StudentGroup.Create(7, new string('x', StudentGroup.MaxNameLength + 1), null, 3, Now));

    [Fact]
    public void Rename_SameName_ReportsNoChange()
    {
        var group = StudentGroup.Create(7, "Stream A", null, 3, Now);

        Assert.False(group.Rename(" Stream A "));
        Assert.True(group.Rename("Stream B"));
        Assert.Equal("Stream B", group.Name);
    }

    [Fact]
    public void AddMember_Twice_Throws()
    {
        var group = StudentGroup.Create(7, "Stream A", null, 3, Now);
        group.AddMember(userId: 10, addedByUserId: 3, Now);

        Assert.Throws<InvalidOperationException>(() => group.AddMember(10, 3, Now));
    }

    [Fact]
    public void RemoveMember_ReportsWhetherAnythingWasRemoved()
    {
        var group = StudentGroup.Create(7, "Stream A", null, 3, Now);
        group.AddMember(10, 3, Now);

        Assert.True(group.RemoveMember(10));
        Assert.False(group.RemoveMember(10));
        Assert.Empty(group.Members);
    }
}
