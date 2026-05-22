using GlutonsAscensions.Helpers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace GlutonsAscensions.Patches;

[HarmonyPatch]
public class SlimPickingsPatches {
    [HarmonyPatch(typeof(CardReward), nameof(CardReward.Populate))]
    [HarmonyPostfix]
    static void ReduceEliteCardRewardOptions(CardReward __instance) {
        if (!GlutonsAscensionLevel.ShortSupply.HasAscension()) return;
        if (__instance.Player.RunState.BaseRoom?.RoomType != RoomType.Elite) return;
    
        if (__instance._cards.Count > 0) {
            __instance._cards.RemoveAt(0);
        }
    }
}