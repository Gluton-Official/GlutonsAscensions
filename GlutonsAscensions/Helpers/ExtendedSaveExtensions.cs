using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using BaseLib.Patches.Saves;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Saves;

namespace GlutonsAscensions.Helpers;

public static class ExtendedSaveExtensions {
    extension<T>(T) where T : notnull, new() {
        public static void RegisterAsSaveType(EnforceJsonPropertyNames? enforceJsonPropertyNames = null) {
            typeof(T).RegisterAsSaveType(enforceJsonPropertyNames ?? (typeof(T).IsAssignableTo(typeof(ISaveSchema)) ? EnforceJsonPropertyNames.Required : EnforceJsonPropertyNames.Ignored));
        }
    }
    
    private static readonly MethodInfo _registerObjectSaveTypeMethod = typeof(ExtendedSaveTypes).GetMethod(nameof(ExtendedSaveTypes.RegisterObjectSaveType)) ?? throw new Exception("Unable to get RegisterObjectSaveType method");
    private static readonly MethodInfo _registerListSaveTypeMethod = typeof(ExtendedSaveTypes).GetMethod(nameof(ExtendedSaveTypes.RegisterListSaveType)) ?? throw new Exception("Unable to get RegisterObjectSaveType method");
    private static readonly MethodInfo _registerDictionarySaveTypeMethod = typeof(ExtendedSaveTypes).GetMethod(nameof(ExtendedSaveTypes.RegisterDictionarySaveType)) ?? throw new Exception("Unable to get RegisterDictionarySaveType method");
    
    private static readonly MethodInfo _registerObjectConstructorSaveTypeMethod = typeof(ExtendedSaveExtensions).GetMethod(nameof(RegisterObjectConstructorSaveType)) ?? throw new Exception("Unable to get RegisterObjectConstructorSaveType method");
    
    private static readonly MethodInfo _fieldFuncMethod = typeof(ExtendedSaveTypes).GetMethod(nameof(ExtendedSaveTypes.FieldFunc)) ?? throw new Exception("Unable to get FieldFunc method");
    private static readonly MethodInfo _propertyFuncMethod = typeof(ExtendedSaveTypes).GetMethod(nameof(ExtendedSaveTypes.PropertyFunc)) ?? throw new Exception("Unable to get PropertyFunc method");
    
    private static readonly MethodInfo _namedFieldFuncMethod = typeof(ExtendedSaveExtensions).GetMethod(nameof(NamedFieldFunc)) ?? throw new Exception("Unable to get NamedFieldFunc method");
    private static readonly MethodInfo _namedPropertyFuncMethod = typeof(ExtendedSaveExtensions).GetMethod(nameof(NamedPropertyFunc)) ?? throw new Exception("Unable to get NamedPropertyFunc method");

    public enum EnforceJsonPropertyNames {
        Required,
        Optional,
        Ignored,
    }

    extension(Type type) {
        private void RegisterAsSaveType(EnforceJsonPropertyNames enforceJsonPropertyNames) {
            if (type.IsRegisteredSaveType()) return;
            
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) {
                var listGenericType = type.GetGenericArguments()[0];
                listGenericType.RegisterAsSaveType(enforceJsonPropertyNames);
                _registerListSaveTypeMethod.MakeGenericMethod(listGenericType).Invoke(null, null);
            } else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)) {
                var keyGenericType = type.GetGenericArguments()[0];
                keyGenericType.RegisterAsSaveType(enforceJsonPropertyNames);
                var valueGenericType = type.GetGenericArguments()[1];
                valueGenericType.RegisterAsSaveType(enforceJsonPropertyNames);
                _registerDictionarySaveTypeMethod.MakeGenericMethod(keyGenericType, valueGenericType).Invoke(null, null);
            } else {
                _registerObjectSaveTypeMethod.MakeGenericMethod(type).Invoke(null, [type.GetMemberFuncs(enforceJsonPropertyNames)]);
            }
            
            GlutonsAscensionsMod.Logger.Info($"Registered {type.FriendlyName} as an ExtendedSaveType");
        }

        private bool IsRegisteredSaveType() => SavePatchUtils.IsStoreTypeBaseSupported(type) || ExtendedSaveTypes._extendedTypes.ContainsKey(type);

        private Func<JsonSerializerOptions, JsonPropertyInfo>[] GetMemberFuncs(EnforceJsonPropertyNames enforceJsonPropertyNames) =>
            type.GetFieldFuncs(enforceJsonPropertyNames).Union(type.GetPropertyFuncs(enforceJsonPropertyNames)).ToArray();

        private IEnumerable<Func<JsonSerializerOptions, JsonPropertyInfo>> GetFieldFuncs(EnforceJsonPropertyNames enforceJsonPropertyNames) =>
            type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Select(fieldInfo => {
                    if (!ExtendedSaveTypes._extendedTypes.ContainsKey(fieldInfo.FieldType)) fieldInfo.FieldType.RegisterAsSaveType(enforceJsonPropertyNames);
                    return (jsonPropertyNames: enforceJsonPropertyNames, fieldInfo.TryGetJsonPropertyName()) switch {
                        (EnforceJsonPropertyNames.Ignored, _) => _fieldFuncMethod.MakeGenericMethod(type, fieldInfo.FieldType).Invoke(null, [fieldInfo.Name]),
                        (EnforceJsonPropertyNames.Required, { } jsonPropertyName) => _namedFieldFuncMethod.MakeGenericMethod(type, fieldInfo.FieldType).Invoke(null, [fieldInfo.Name, jsonPropertyName]),
                        (EnforceJsonPropertyNames.Required, null) => throw new Exception($"[GlutonsAscensions] {fieldInfo.Name} in {type.Name} does not have {nameof(JsonPropertyNameAttribute)}"),
                        (EnforceJsonPropertyNames.Optional, { } jsonPropertyName) => _namedFieldFuncMethod.MakeGenericMethod(type, fieldInfo.FieldType).Invoke(null, [fieldInfo.Name, jsonPropertyName]),
                        (EnforceJsonPropertyNames.Optional, null) => _fieldFuncMethod.MakeGenericMethod(type, fieldInfo.FieldType).Invoke(null, [fieldInfo.Name]),
                        _ => throw new InvalidEnumArgumentException(nameof(enforceJsonPropertyNames), (int) enforceJsonPropertyNames, typeof(EnforceJsonPropertyNames))
                    };
                })
                .Cast<Func<JsonSerializerOptions, JsonPropertyInfo>>();

        private IEnumerable<Func<JsonSerializerOptions, JsonPropertyInfo>> GetPropertyFuncs(EnforceJsonPropertyNames enforceJsonPropertyNames = EnforceJsonPropertyNames.Ignored) =>
            type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Select(propertyInfo => {
                    if (!ExtendedSaveTypes._extendedTypes.ContainsKey(propertyInfo.PropertyType)) propertyInfo.PropertyType.RegisterAsSaveType(enforceJsonPropertyNames);
                    return (jsonPropertyNames: enforceJsonPropertyNames, propertyInfo.TryGetJsonPropertyName()) switch {
                        (EnforceJsonPropertyNames.Ignored, _) => _propertyFuncMethod.MakeGenericMethod(type, propertyInfo.PropertyType).Invoke(null, [propertyInfo.Name]),
                        (EnforceJsonPropertyNames.Required, { } jsonPropertyName) => _namedPropertyFuncMethod.MakeGenericMethod(type, propertyInfo.PropertyType).Invoke(null, [propertyInfo.Name, jsonPropertyName]),
                        (EnforceJsonPropertyNames.Required, null) => throw new Exception($"[GlutonsAscensions] {propertyInfo.Name} in {type.Name} does not have {nameof(JsonPropertyNameAttribute)}"),
                        (EnforceJsonPropertyNames.Optional, { } jsonPropertyName) => _namedPropertyFuncMethod.MakeGenericMethod(type, propertyInfo.PropertyType).Invoke(null, [propertyInfo.Name, jsonPropertyName]),
                        (EnforceJsonPropertyNames.Optional, null) => _propertyFuncMethod.MakeGenericMethod(type, propertyInfo.PropertyType).Invoke(null, [propertyInfo.Name]),
                        _ => throw new InvalidEnumArgumentException(nameof(enforceJsonPropertyNames), (int) enforceJsonPropertyNames, typeof(EnforceJsonPropertyNames))
                    };
                })
                .Cast<Func<JsonSerializerOptions, JsonPropertyInfo>>();
    }
    
    private static readonly FieldInfo _extendedTypesField = AccessTools.DeclaredField(typeof(ExtendedSaveTypes), "ExtendedTypes") ?? throw new Exception("Unable to get ExtendedTypes field");

    extension(ExtendedSaveTypes) {
        private static Dictionary<Type, Func<IJsonTypeInfoResolver, JsonSerializerOptions, JsonTypeInfo>> _extendedTypes =>
            _extendedTypesField.GetValue(null) as Dictionary<Type, Func<IJsonTypeInfoResolver, JsonSerializerOptions, JsonTypeInfo>> ?? throw new Exception($"Unable to cast ExtendedTypes field to {typeof(Dictionary<Type, Func<IJsonTypeInfoResolver, JsonSerializerOptions, JsonTypeInfo>>)}");

        public static void RegisterObjectConstructorSaveType<T>(
            EnforceJsonPropertyNames enforceJsonPropertyNames = EnforceJsonPropertyNames.Ignored,
            ConstructorInfo? constructorInfo = null
        ) where T : notnull {
            if (ExtendedSaveTypes._extendedTypes.ContainsKey(typeof(T))) return;

            constructorInfo ??= typeof(T).GetDeclaredConstructors().FirstOrDefault()
                ?? throw new Exception($"[GlutonsAscensions] Unable to find a declared constructor for {typeof(T).Name}");
            
            ExtendedSaveTypes.RegisterAdditionalSaveType<T>((resolver, options) => {
                var objectInfo = new JsonObjectInfoValues<T> {
                    ObjectCreator = null,
                    ObjectWithParameterizedConstructorCreator = args => (T) constructorInfo.Invoke(args),
                    PropertyMetadataInitializer = (Func<JsonSerializerContext, JsonPropertyInfo[]>) (_ => constructorInfo.GetParameters()
                            .Select(paramInfo => {
                                return paramInfo.Member switch {
                                    PropertyInfo propertyInfo => _propertyFuncMethod.MakeGenericMethod(typeof(T), propertyInfo.PropertyType).Invoke(null, [propertyInfo.Name]),
                                    FieldInfo fieldInfo => _fieldFuncMethod.MakeGenericMethod(typeof(T), fieldInfo.FieldType).Invoke(null, [fieldInfo.Name]),
                                    _ => throw new Exception($"[GlutonsAscensions] Parameter {paramInfo.Name} for constructor of {typeof(T).Name} is unable to be mapped to a property or field")
                                };
                            })
                            .Cast<Func<JsonSerializerOptions, JsonPropertyInfo>>()
                            .Select(func => func(options))
                            .ToArray()
                        ),
                    ConstructorParameterMetadataInitializer = (Func<JsonParameterInfoValues[]>) (() => constructorInfo.GetParameters().Select(JsonParameterInfoValues.From).ToArray()),
                    ConstructorAttributeProviderFactory = (Func<ICustomAttributeProvider>) (() => constructorInfo),
                    SerializeHandler = null
                };
                var typeInfo = JsonMetadataServices.CreateObjectInfo(options, objectInfo);
                typeInfo.OriginatingResolver = resolver;
                return typeInfo;
            });
            
            GlutonsAscensionsMod.Logger.Info($"[Type] Registered {typeof(T).FriendlyName} as a save type with constructor {constructorInfo}!");
        }

        public static Func<JsonSerializerOptions, JsonPropertyInfo> NamedPropertyFunc<DeclaringType, PropType>(string propName, string jsonName) {
            var propertyInfo = typeof(DeclaringType).GetProperty(propName);
            if (propertyInfo is null) throw new ArgumentException($"Unable to find public property '{propName}' in type {typeof(DeclaringType).Name}");
            return ExtendedSaveTypes.CreateJsonPropertyInfoFactory<DeclaringType, PropType, PropertyInfo>(
                propertyInfo,
                obj => (PropType) propertyInfo.GetValue(obj)!,
                (obj, value) => propertyInfo.SetValue(obj, value),
                jsonName
            );
        }

        public static Func<JsonSerializerOptions, JsonPropertyInfo> NamedFieldFunc<DeclaringType, FieldType>(string fieldName, string jsonName) {
            var fieldInfo = typeof(DeclaringType).GetField(fieldName);
            if (fieldInfo is null) throw new ArgumentException($"Unable to find public field '{fieldName}' in type {typeof(DeclaringType).Name}");
            return ExtendedSaveTypes.CreateJsonPropertyInfoFactory<DeclaringType, FieldType, FieldInfo>(
                fieldInfo,
                obj => (FieldType) fieldInfo.GetValue(obj)!,
                (obj, value) => fieldInfo.SetValue(obj, value),
                jsonName
            );
        }

        private static Func<JsonSerializerOptions, JsonPropertyInfo> CreateJsonPropertyInfoFactory<DeclaringType, MemberType, MemberInfoType>(
            MemberInfoType memberInfo,
            Func<object, MemberType> getter,
            Action<object, MemberType> setter,
            string jsonName
        ) where MemberInfoType : MemberInfo {
            return options => {
                var jsonPropertyInfoValues = new JsonPropertyInfoValues<MemberType> {
                    IsProperty = false,
                    IsPublic = true,
                    IsVirtual = false,
                    DeclaringType = typeof(DeclaringType),
                    Converter = null,
                    Getter = getter,
                    Setter = setter!,
                    IgnoreCondition = null,
                    HasJsonInclude = false,
                    IsExtensionData = false,
                    NumberHandling = null,
                    PropertyName = memberInfo.Name,
                    JsonPropertyName = jsonName,
                    AttributeProviderFactory = () => memberInfo
                };
                var jsonPropertyInfo = JsonMetadataServices.CreatePropertyInfo(options, jsonPropertyInfoValues);
                jsonPropertyInfo.IsGetNullable = false;
                jsonPropertyInfo.IsSetNullable = false;
                return jsonPropertyInfo;
            };
        }

    }
    
    extension(JsonParameterInfoValues paramInfoValues) {
        private static JsonParameterInfoValues From(ParameterInfo parameterInfo) {
            return new JsonParameterInfoValues {
                Name = parameterInfo.Name ?? throw new Exception("[GlutonsAscensions] ParameterInfo.Name is null"),
                ParameterType = parameterInfo.ParameterType,
                Position = parameterInfo.Position,
                HasDefaultValue = parameterInfo.HasDefaultValue,
                DefaultValue = parameterInfo.DefaultValue,
                IsNullable = parameterInfo.ParameterType.CanBeNull(),
                IsMemberInitializer = parameterInfo.Member is FieldInfo { IsInitOnly: true },
            };
        }
    }

    extension(ICustomAttributeProvider customAttributeProvider) {
        private string? TryGetJsonPropertyName(bool inherit = false) {
            var jsonPropertyNames = customAttributeProvider.GetCustomAttributes(typeof(JsonPropertyNameAttribute), inherit);
            var jsonPropertyName = jsonPropertyNames.FirstOrDefault() as JsonPropertyNameAttribute;
            return jsonPropertyName?.Name;
        }
    }
}
