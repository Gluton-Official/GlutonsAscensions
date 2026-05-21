using GlutonsAscensions.Helpers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Random;

namespace GlutonsAscensions.Patches;

using static HarmonyExtensions;

[HarmonyPatch]
public class BarrenPatches {
    private const int MeanUnknownCount = 10;
    
    [HarmonyPatch(typeof(MapPointTypeCounts), nameof(MapPointTypeCounts.StandardRandomUnknownCount))]
    [HarmonyPrefix]
    static bool ReduceUnknownRoomCount(Rng rng, ref int __result) {
        if (!GlutonsAscensionLevel.Barren.HasAscension()) return PrefixRunOriginal;
        
        __result = rng.NextGaussianInt(MeanUnknownCount, 1, MeanUnknownCount - 2, MeanUnknownCount + 2);
        return PrefixSkipOriginal;
    }
}