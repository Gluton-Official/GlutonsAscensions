using System.Reflection;

namespace GlutonsAscensions.Helpers;

public static class ReflectionHelpers {
    public static void SetBackingField(this PropertyInfo property, object obj, object? value) {
        var parentType = property.DeclaringType ?? throw new InvalidOperationException("Declaring type is null");
        if (obj.GetType() != parentType) {
            throw new ArgumentException($"Object type {obj.GetType()} does not match declaring type {parentType} for property {property.Name}");
        }
        
        var backingField = parentType.GetField($"<{property.Name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new MissingFieldException($"Backing field for property {property.Name} not found");
        if (value is null && !backingField.FieldType.CanBeNull()) {
            throw new ArgumentException($"Cannot assign null to non-nullable type {backingField.FieldType.Name}");
        }
        if (value is not null && !backingField.FieldType.IsInstanceOfType(value)) {
            throw new ArgumentException($"Value type {value.GetType()} does not match backing field type {backingField.FieldType}");
        }

        backingField.SetValue(obj, value);
    }

    public static bool CanBeNull(this Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
}