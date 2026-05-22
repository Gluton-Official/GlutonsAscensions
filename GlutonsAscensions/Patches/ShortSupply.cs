using GlutonsAscensions.Helpers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;

namespace GlutonsAscensions.Patches;

[HarmonyPatch]
public class ShortSupply {
    [HarmonyPatch(typeof(AncientEventModel), "GenerateInitialOptionsWrapper")]
    [HarmonyPostfix]
    static void ReduceAncientRelicOptions(AncientEventModel __instance, ref IReadOnlyList<EventOption> __result) {
        if (!GlutonsAscensionLevel.ShortSupply.HasAscension()) return;
        
        var rng = __instance.Rng;
        var options = __result.ToList();
        while (options.Count > 2) {
            options.RemoveAt(rng.NextInt(options.Count));
        }
        __result = options;
    }
}