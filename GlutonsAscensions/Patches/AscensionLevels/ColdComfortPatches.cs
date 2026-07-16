using GlutonsAscensions.Helpers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace GlutonsAscensions.Patches.AscensionLevels;

[HarmonyPatch]
public class ColdComfortPatches {
    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Heal))]
    [HarmonyPrefix]
    static void ClampAncientHealing(Creature creature, ref decimal amount) {
        if (!GlutonsAscensionLevel.ColdComfort.HasAscension()) return;

        if (creature.Player is not { } player || player.RunState.CurrentActIndex == 0) return; // Skip first-act ancient
        if (player.RunState.CurrentRoom is not EventRoom { CanonicalEvent: AncientEventModel }) return;

        var maxHealAmount = HealRestSiteOption.GetBaseHealAmount(player.Creature);
        amount = Math.Min(amount, maxHealAmount); // Cap ancient heal amount to base rest site heal
    }
}
