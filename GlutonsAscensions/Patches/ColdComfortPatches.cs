using GlutonsAscensions.Helpers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Rooms;

namespace GlutonsAscensions.Patches;

[HarmonyPatch]
public class ColdComfortPatches {
    [HarmonyPatch(typeof(AncientEventModel), "BeforeEventStarted")]
    [HarmonyPrefix]
    static void BeforeTurnEndPrefix(AncientEventModel __instance, out int __state) {
        if (!GlutonsAscensionLevel.ColdComfort.HasAscension() || __instance is Neow) {
            __state = 0;
            return;
        }
        
        var oldHp = __instance.Owner!.Creature.CurrentHp;
        __state = oldHp;
    }
    
    [HarmonyPatch(typeof(AncientEventModel), "BeforeEventStarted")]
    [HarmonyPostfix]
    static void BeforeTurnEndPostfix(AncientEventModel __instance, int __state) {
        if (!GlutonsAscensionLevel.ColdComfort.HasAscension() || __instance is Neow) return;
        
        var oldHp = __state;
        __instance.HealedAmount = __instance.Owner!.Creature.CurrentHp - oldHp;
    }

    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Heal))]
    [HarmonyPrefix]
    static void HealPrefix(Creature creature, ref decimal amount) {
        if (!GlutonsAscensionLevel.ColdComfort.HasAscension()) return;

        if (!LocalContext.IsMe(creature)) return;

        var currentRoom = creature.Player!.RunState.CurrentRoom;
        if (currentRoom?.RoomType != RoomType.Event) return;
        if (currentRoom is not EventRoom { CanonicalEvent: AncientEventModel } ancient) return;
        if (ancient.CanonicalEvent is Neow) return;

        amount *= .25M; // amount will have already been multiplied by .8, so the additional .25 gets the amount down to .2
    }
}
