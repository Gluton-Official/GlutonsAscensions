using GlutonsAscensions.Saves;
using HarmonyLib;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace GlutonsAscensions.Patches;

[HarmonyPatch]
public class ProgressSavePatches {
    [HarmonyPatch(typeof(ProgressSaveManager), nameof(ProgressSaveManager.LoadProgress))]
    [HarmonyPostfix]
    static void LoadProgressPostfix(ProgressSaveManager __instance) {
        var ascensionProgress = AscensionProgress.Load(__instance._migrationManager, __instance._profileIdProvider);
        ascensionProgress?.ApplyToProgressState(__instance.Progress);
    }

    [HarmonyPatch(typeof(ProgressSaveManager), nameof(ProgressSaveManager.SaveProgress))]
    [HarmonyPostfix]
    static void SaveProgressPostfix(ProgressSaveManager __instance) {
        var ascensionProgress = AscensionProgress.FromProgressState(__instance.Progress);
        ascensionProgress.Save(__instance._saveStore, __instance._migrationManager, __instance._profileIdProvider);
    }
}
