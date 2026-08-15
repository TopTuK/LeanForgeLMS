using System.Reflection;

namespace LF.ApplicationTests.TestSupport;

/// <summary>
/// Domain entity Ids are DB-generated (private setter, 0 until persisted). Tests that need
/// multiple distinct entities looked up or grouped by Id (e.g. Dictionary&lt;int, T&gt;) need
/// distinct Ids without a real EF round-trip, so this simulates post-persistence state via
/// reflection over the public Id property's private setter.
/// </summary>
internal static class EntityIdSetter
{
    public static void SetId(object entity, int id)
    {
        var property = entity.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"{entity.GetType().Name} has no Id property.");

        property.SetValue(entity, id);
    }
}
