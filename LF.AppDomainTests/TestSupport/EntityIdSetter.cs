using System.Reflection;

namespace LF.AppDomainTests.TestSupport;

/// <summary>
/// Domain entity Ids are DB-generated (private setter, 0 until persisted). Tests exercising
/// Id-based lookups (reorder/remove) need distinct Ids without a real EF round-trip, so this
/// simulates post-persistence state via reflection over the public Id property's private setter.
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
