namespace LF.AppDomain.Entities.Course;

public sealed class Category
{
    private Category()
    {
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public bool IsDefault { get; private set; }

    public static Category Create(string name, bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name cannot be empty.", nameof(name));

        return new Category { Name = name.Trim(), IsDefault = isDefault };
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name cannot be empty.", nameof(name));

        Name = name.Trim();
    }
}
