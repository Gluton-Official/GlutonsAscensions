using System.Reflection;
using HarmonyLib;

namespace GlutonsAscensions.Helpers;

public static class ReflectionExtensions {
    extension(PropertyInfo property) {
        public void SetBackingField(object obj, object? value) {
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
    }

    extension(Type type) {
        public bool CanBeNull() => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;

        public string FriendlyName {
            get {
                if (!type.IsGenericType) return type.Name;
        
                var baseName = type.Name.Split('`')[0];
                var argumentNames = type.GetGenericArguments().Select(type => type.Name);
                return $"{baseName}<{string.Join(", ", argumentNames)}>";
            }
        }
    }

    extension(MethodBase baseMethod) {
        /// <param name="searchAllTypes">If false, only searches the assembly of the base method's declaring type</param>
        /// <returns>The base method and all methods that override it</returns>
        public IEnumerable<MethodBase> FindOverrides(bool searchAllTypes = false) {
            GlutonsAscensionsMod.Logger.Debug($"Searching {(searchAllTypes ? "all assemblies " : "")}for overrides of: {baseMethod?.DeclaringType?.FriendlyName}::{baseMethod?.Name}");
            if (baseMethod is null) yield break;
            if (baseMethod.HasMethodBody()) {
                GlutonsAscensionsMod.Logger.Debug("  Including base method");
                yield return baseMethod;
            }
            if (baseMethod.DeclaringType is not { } baseType) {
                GlutonsAscensionsMod.Logger.Debug("  Declaring type was null, unable to search types");
                yield break;
            }

            foreach (var method in (searchAllTypes ? AccessTools.AllTypes() : baseType.Assembly.GetTypes())
                .Where(type => type.IsClass && !type.IsAbstract && type.IsAssignableTo(baseType))
                .Select(type => {
                    var method = AccessTools.DeclaredMethod(type, baseMethod.Name);
                    if (method is null) GlutonsAscensionsMod.Logger.Debug($"  {type.FriendlyName} inherits {baseMethod.Name}");
                    return method;
                })
                .Where(method =>
                    method is not null &&
                    method.HasMethodBody() &&
                    method.GetBaseDefinition() == baseMethod
                )
            ) {
                GlutonsAscensionsMod.Logger.Debug($"Found: {method!.DeclaringType?.FriendlyName}::{method.Name}");
                yield return method;
            }
        }
    }
}