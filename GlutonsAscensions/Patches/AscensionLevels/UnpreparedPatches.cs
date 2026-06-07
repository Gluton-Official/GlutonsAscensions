using GlutonsAscensions.Helpers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Hooks;

namespace GlutonsAscensions.Patches.AscensionLevels;

[HarmonyPatch]
public class UnpreparedPatches {
    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyHandDraw))]
    [HarmonyPostfix]
    static void ReduceStartOfCombatDrawCount(CombatState combatState, ref decimal __result) {
        if (!GlutonsAscensionLevel.Unprepared.HasAscension()) return;

        if (combatState.RoundNumber == 1) {
            __result = Math.Max(__result - 1, 0);
        }
    }
}