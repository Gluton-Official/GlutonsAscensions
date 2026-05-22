using GlutonsAscensions.Helpers;
using GlutonsAscensions.Models;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Rewards;

namespace GlutonsAscensions.Patches;

[HarmonyPatch]
public class EmptyFlasksPatches {
    private const float EmptyFlaskChance = 0.5f;

    [HarmonyPatch(typeof(PotionReward), nameof(PotionReward.Populate))]
    [HarmonyPostfix]
    static void PopulatePostfix(PotionReward __instance) {
        if (!GlutonsAscensionLevel.EmptyFlasks.HasAscension()) return;

        var rng = __instance._rngOverride ?? __instance.Player.PlayerRng.Rewards;
        if (rng.NextFloat() >= EmptyFlaskChance) {
            __instance.Potion = ModelDb.Potion<EmptyPotion>().ToMutable();
        }
    }
}