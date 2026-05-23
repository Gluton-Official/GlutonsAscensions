using GlutonsAscensions.Helpers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models.Cards;

namespace GlutonsAscensions.Patches;

[HarmonyPatch]
public class PlunderedPatches {
    [HarmonyPatch(typeof(StandardActMap), nameof(StandardActMap.AssignPointTypes))]
    [HarmonyPostfix]
    static void ReplaceTreasuresWithUnknowns(StandardActMap __instance) {
        if (!GlutonsAscensionLevel.Plundered.HasAscension()) return;

        var treasureRow = __instance.GetRowCount() - 7;
        var treasureRowPoints = __instance.GetPointsInRow(treasureRow).ToList();
        foreach (var point in treasureRowPoints) {
            if (point.PointType == MapPointType.Treasure) {
                point.PointType = MapPointType.Unknown;
            }
        }
    }

    // If the player has more than 1 Spoils Map, multiple Treasures may generate in Act 2
    // despite only one containing the spoils
    [HarmonyPatch(typeof(SpoilsMap), nameof(SpoilsMap.AfterMapGenerated))]
    [HarmonyPostfix]
    static void ReplaceExtraSpoilsTreasuresWithUnknowns(ActMap map) {
        if (!GlutonsAscensionLevel.Plundered.HasAscension()) return;
        if (map is not SpoilsActMap spoilsActMap) return;
        
        var treasureRowPoints = spoilsActMap.GetPointsInRow(spoilsActMap._treasureRow);
        foreach (var point in treasureRowPoints) {
            if (point.PointType == MapPointType.Treasure && !point.Quests.Any(m => m is SpoilsMap)) {
                point.PointType = MapPointType.Unknown;
            }
        }
    }
}