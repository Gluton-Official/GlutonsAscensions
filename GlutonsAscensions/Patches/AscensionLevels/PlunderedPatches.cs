using GlutonsAscensions.Helpers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace GlutonsAscensions.Patches.AscensionLevels;

using static HarmonyExtensions;

[HarmonyPatch]
public class PlunderedPatches {
    [HarmonyPatch(typeof(Hook), nameof(Hook.ShouldGenerateTreasure))]
    [HarmonyPostfix]
    static void ShouldGenerateTreasurePostfix(IRunState runState, ref bool __result) {
        if (!GlutonsAscensionLevel.Plundered.HasAscension()) return;
        
        if (runState.CurrentMapPoint?.PointType == MapPointType.Treasure && runState.CurrentRoom?.RoomType == RoomType.Treasure) {
            __result = false;
        }
    }

    [HarmonyPatch(typeof(SilverCrucible), nameof(SilverCrucible.AfterRoomEntered))]
    [HarmonyPrefix]
    static bool PreventSilverCrucibleUse(SilverCrucible __instance, AbstractRoom room, ref Task __result) {
        if (!GlutonsAscensionLevel.Plundered.HasAscension()) return PrefixRunOriginal;

        // If the room is a marked Treasure room, Plundered already prevents treasure generation,
        // so Silver Crucible's Treasure Chest emptying shouldn't be used up
        if (room is TreasureRoom && __instance.Owner.RunState.CurrentMapPoint?.PointType == MapPointType.Treasure) {
            __result = Task.CompletedTask;
            return PrefixSkipOriginal;
        }
        
        return PrefixRunOriginal;
    }

    [HarmonyPatch(typeof(OneOffSynchronizer), nameof(OneOffSynchronizer.DoTreasureRoomRewards))]
    [HarmonyPrefix]
    static bool EnsureHandleSpoilsMap(OneOffSynchronizer __instance, Player player, ref Task<int> __result) {
        if (Hook.ShouldGenerateTreasure(player.RunState, player)) return PrefixRunOriginal;
        
        // Even if treasure isn't generated, Spoils map should still be handled
        var gold = Task.Run(async () => await __instance.TryHandleSpoilsMap(player));
        if (gold.Result == 0) return PrefixRunOriginal;
        
        __result = gold;
        return PrefixSkipOriginal;
    }
}