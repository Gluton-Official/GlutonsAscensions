using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HarmonyLib;

namespace GlutonsAscensions.Helpers;

public static class BetaCompatHelpers {
    public static bool TryGetBetaType(string fullyQualifiedTypeName, [NotNullWhen(true)] out Type? betaType) {
        betaType = AccessTools.TypeByName(fullyQualifiedTypeName);
        if (betaType is not null) GlutonsAscensionsMod.Logger.Debug($"Beta type {fullyQualifiedTypeName} not found");
        return betaType is not null;
    }

    public static bool TryGetBetaMethod(Type type, string methodName, [NotNullWhen(true)] out MethodBase? betaMethod) {
        betaMethod = AccessTools.Method(type, methodName);
        if (betaMethod is not null) GlutonsAscensionsMod.Logger.Debug($"Beta method {type.FullName}:{methodName} not found");
        return betaMethod is not null;
    }
    
    public static bool TryGetBetaMethod(string fullyQualifiedTypeName, string methodName, [NotNullWhen(true)]  out MethodBase? betaMethod) {
        if (TryGetBetaType(fullyQualifiedTypeName, out var type)) {
            return TryGetBetaMethod(type, methodName, out betaMethod);
        }
        betaMethod = null;
        return false;
    }
}
