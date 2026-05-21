using GlutonsAscensions.Helpers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;

namespace GlutonsAscensions.Patches;

[HarmonyPatch]
public class OutOfBusinessPatches {
    [HarmonyPatch(typeof(MapPointTypeCounts), MethodType.Constructor, typeof(int), typeof(int))]
    [HarmonyPostfix]
    static void ReduceShopCount(MapPointTypeCounts __instance) {
        if (!GlutonsAscensionLevel.OutOfBusiness.HasAscension()) return;
        
        var numOfShopsProperty = AccessTools.Property(typeof(MapPointTypeCounts), nameof(MapPointTypeCounts.NumOfShops)) ?? throw new Exception("[GlutonsAcesnsions] Unable to get NumOfShops property");
        var numOfShops = numOfShopsProperty.GetValue(__instance) ?? throw new Exception("[GlutonsAcesnsions] Unable to get NumOfShops value");
        numOfShopsProperty.SetBackingField(__instance, Math.Max((int) numOfShops - 1, 0));
    }
    
    [HarmonyPatch(typeof(MapPointTypeCounts), MethodType.Constructor, typeof(ActMap))]
    [HarmonyPostfix]
    static void ReduceShopCount(MapPointTypeCounts __instance, ActMap existingMap) {
        if (!GlutonsAscensionLevel.OutOfBusiness.HasAscension()) return;
        
        var numOfShops = existingMap.GetAllMapPoints().Count(p => p.PointType == MapPointType.Shop);
        
        var numOfShopsProperty = AccessTools.Property(typeof(MapPointTypeCounts), nameof(MapPointTypeCounts.NumOfShops)) ?? throw new Exception("[GlutonsAcesnsions] Unable to get NumOfShops property");
        numOfShopsProperty.SetBackingField(__instance, Math.Max(numOfShops - 1, 0));
    }
}