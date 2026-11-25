using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using BaseLib.Extensions;
using BaseLib.Utils;
using HarmonyLib;
using JetBrains.Annotations;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Saves;

namespace GlutonsAscensions.Helpers;

public static class BetaCompatHelpers {
    public static readonly SemanticVersion VERSION_113 = new(0, 113, 0);
    
    public static readonly VersionDependentType GameModeType =
        new("MegaCrit.Sts2.Core.Runs.GameMode", () => IsAtLeastVersion(VERSION_113));
    public static int? GameModeNone => GameModeType.Type is null ? null : (int) Enum.Parse(GameModeType.Type, "None");
    public static int? GameModeBlitz => GameModeType.Type is null ? null : (int) Enum.Parse(GameModeType.Type, "Blitz");
    
    public static readonly VersionDependentType NDreadScreenType =
        new("MegaCrit.Sts2.Core.Nodes.Screens.Dread.NDreadScreen", () => IsAtLeastVersion(VERSION_113));

    private static readonly VersionDependentProperty<ProgressState, int> MaxBlitzAscensionProperty =
        new("MaxBlitzAscension", () => IsAtLeastVersion(VERSION_113));
    private static readonly VersionDependentProperty<ProgressState, int> PreferredBlitzAscensionProperty =
        new("PreferredBlitzAscension", () => IsAtLeastVersion(VERSION_113));

    private static readonly VersionDependentField<NAscensionPanel, MultiplayerUiMode> UiModeField =
        new("_uiMode", "_mode", () => IsAtLeastVersion(VERSION_113));
    private static readonly VersionDependentField<NAscensionPanel, int> GameModeField =
        new("_gameMode", () => IsAtLeastVersion(VERSION_113));
    
    extension(NAscensionPanel ascensionPanel) {
        public MultiplayerUiMode BetaCompatUiMode => UiModeField.GetValue(ascensionPanel);
        public int? BetaCompatGameMode => GameModeField.GetValue(ascensionPanel);    
    }
    
    extension(ProgressState progressState) {
        public int BetaCompatMaxBlitzAscension {
            get => MaxBlitzAscensionProperty.GetValueOrDefault(progressState);
            set => MaxBlitzAscensionProperty.TrySetValue(progressState, value);
        }
        public int BetaCompatPreferredBlitzAscension {
            get => PreferredBlitzAscensionProperty.GetValueOrDefault(progressState);
            set => PreferredBlitzAscensionProperty.TrySetValue(progressState, value);
        }
    }
    
    public static bool IsAtLeastVersion(SemanticVersion version) => BetaMainCompatibility.Version.GreaterThanOrEqual(version);
    public static bool IsBeforeVersion(SemanticVersion version) => BetaMainCompatibility.Version.LessThan(version);
    
    public static MethodInfo GetVersionDependentMethodInfo<T>(string methodName, string fallbackMethodName, Func<bool> existsPredicate) {
        return existsPredicate() ? AccessTools.Method(typeof(T), methodName) : AccessTools.Method(typeof(T), fallbackMethodName);
    }

    public static Type GetBetaType(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        [LanguageInjection("csharp", Prefix = "typeof(", Suffix = ")")]
        string fullyQualifiedTypeName
    ) => AccessTools.TypeByName(fullyQualifiedTypeName) ?? throw new Exception($"Beta type {fullyQualifiedTypeName} not found");

    public static bool TryGetBetaType(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        [LanguageInjection("csharp", Prefix = "typeof(", Suffix = ")")]
        string fullyQualifiedTypeName,
        [NotNullWhen(true)]
        out Type? betaType
    ) {
        betaType = AccessTools.TypeByName(fullyQualifiedTypeName);
        if (betaType is null) GlutonsAscensionsMod.Logger.Debug($"Beta type {fullyQualifiedTypeName} not found");
        return betaType is not null;
    }

    public static bool TryGetBetaMethod(Type type, string methodName, [NotNullWhen(true)] out MethodBase? betaMethod) {
        betaMethod = AccessTools.Method(type, methodName);
        if (betaMethod is null) GlutonsAscensionsMod.Logger.Debug($"Beta method {type.FullName}:{methodName} not found");
        return betaMethod is not null;
    }
    
    public static bool TryGetBetaMethod(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        [LanguageInjection("csharp", Prefix = "typeof(", Suffix = ")")]
        string fullyQualifiedTypeName,
        string methodName,
        [NotNullWhen(true)]
        out MethodBase? betaMethod
    ) {
        if (TryGetBetaType(fullyQualifiedTypeName, out var type)) {
            return TryGetBetaMethod(type, methodName, out betaMethod);
        }
        betaMethod = null;
        return false;
    }
}

public class VersionDependentType {
    public Type? Type { get; }
    
    public VersionDependentType(string typeName, Func<bool> existsPredicate) {
        Type = existsPredicate() ? AccessTools.TypeByName(typeName) : null;
    }
    
    public VersionDependentType(string typeName, string fallbackTypeName, Func<bool> existsPredicate) {
        Type = existsPredicate() ? AccessTools.TypeByName(typeName) : AccessTools.TypeByName(fallbackTypeName);
    }
}

public class VersionDependentField<T, R> {
    private readonly FieldInfo? _fieldInfo;
        
    public VersionDependentField(string fieldName, Func<bool> existsPredicate) {
        _fieldInfo = existsPredicate() ? AccessTools.Field(typeof(T), fieldName) : null;
    }
        
    public VersionDependentField(string fieldName, string fallbackFieldName, Func<bool> existsPredicate) {
        _fieldInfo = existsPredicate() ? AccessTools.Field(typeof(T), fieldName) : AccessTools.Field(typeof(T), fallbackFieldName);
    }
    
    public R GetValue(T owner) => (R) _fieldInfo!.GetValue(owner)!;

    public R? GetValueOrNull(T owner) => (R?) _fieldInfo?.GetValue(owner);
        
    public R GetValueOrDefault(T owner, R defaultValue = default!) => 
        _fieldInfo is not null ? (R) _fieldInfo.GetValue(owner)! : defaultValue;
        
    public bool TryGetValue(T owner, out R value) {
        value = _fieldInfo is not null ? (R) _fieldInfo.GetValue(owner)! : default!;
        return _fieldInfo is not null;
    }
        
    public bool TrySetValue(T owner, R value) {
        _fieldInfo?.SetValue(owner, value);
        return _fieldInfo is not null;
    }
}

public class VersionDependentProperty<T, R>(string propertyName, Func<bool> existsPredicate) {
    private readonly PropertyInfo? _propertyInfo = existsPredicate() ? AccessTools.Property(typeof(T), propertyName) : null;

    public R? GetValueOrNull(T owner) => (R?) _propertyInfo?.GetValue(owner);
        
    public R GetValueOrDefault(T owner, R defaultValue = default!) => 
        _propertyInfo is not null ? (R) _propertyInfo.GetValue(owner)! : defaultValue;
        
    public bool TryGetValue(T owner, out R value) {
        value = _propertyInfo is not null ? (R) _propertyInfo.GetValue(owner)! : default!;
        return _propertyInfo is not null;
    }
        
    public bool TrySetValue(T owner, R value) {
        _propertyInfo?.SetValue(owner, value);
        return _propertyInfo is not null;
    }
}

public class VersionDependentMethod<T> {
    public MethodInfo? MethodInfo { get; }

    public VersionDependentMethod(string methodName, Func<bool> existsPredicate) {
        MethodInfo = existsPredicate() ? AccessTools.Method(typeof(T), methodName) : null;   
    }
    
    public VersionDependentMethod(string methodName, string fallbackMethodName, Func<bool> existsPredicate) {
        MethodInfo = existsPredicate() ? AccessTools.Method(typeof(T), methodName) : AccessTools.Method(typeof(T), fallbackMethodName);   
    }
    
    public R? Invoke<R>(T owner, params object?[] args) => (R?) MethodInfo?.Invoke(owner, args);
}