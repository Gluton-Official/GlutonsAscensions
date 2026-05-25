using System.Reflection;
using BaseLib.Config;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;

namespace GlutonsAscensions;

using static Helpers.HarmonyExtensions;

[ConfigHoverTipsByDefault]
internal class GlutonsAscensionsConfig : SimpleModConfig {
    [ConfigButton("UnlockAscension11")]
    public static void UnlockAscension11Button() {
        var progress = SaveManager.Instance.Progress;
        foreach (var character in ModelDb.AllCharacters) {
            if (progress.CharacterStats.TryGetValue(character.Id, out var characterStats)) {
                if (characterStats.MaxAscension == 10) {
                    characterStats.PreferredAscension = ++characterStats.MaxAscension;
                }
            }
        }

        if (progress.MaxMultiplayerAscension == 10) {
            progress.PreferredMultiplayerAscension = ++progress.MaxMultiplayerAscension;
        }
        
        SaveManager.Instance.SaveProgressFile();
    }
}

[HarmonyPatch]
public class BaseLibConfigPatches {
    [HarmonyPatch(typeof(ModConfig), nameof(ModConfig.HasVisibleSettings))]
    [HarmonyPostfix]
    static void ForceAddToModConfigList(ModConfig __instance, ref bool __result) {
        if (__instance is GlutonsAscensionsConfig) {
            __result = true;
        }
    }

    [HarmonyPatch(typeof(ModConfig), nameof(ModConfig.HasSettings))]
    [HarmonyPostfix]
    static void ForceRegisterConfig(ModConfig __instance, ref bool __result) {
        if (__instance is GlutonsAscensionsConfig) {
            __result = true;
        }
    }

    [HarmonyPatch(typeof(SimpleModConfig), "AddRestoreDefaultsButton")]
    [HarmonyPrefix]
    static bool RemoveRestoreDefaultsButton(SimpleModConfig __instance) {
        if (__instance is not GlutonsAscensionsConfig) return PrefixRunOriginal;
        
        if (AccessTools.Field(typeof(SimpleModConfig), "ConfigProperties").GetValue(__instance) is List<PropertyInfo> configProperties) {
            return configProperties.Count == 0 ? PrefixSkipOriginal : PrefixRunOriginal;
        }

        return PrefixRunOriginal;
    }
}