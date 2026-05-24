using System.Reflection;
using GlutonsAscensions.Helpers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;

namespace GlutonsAscensions.Patches;

[HarmonyPatch]
public class OutOfBusinessPatches {
    private static readonly PropertyInfo _numOfShopsProperty = AccessTools.Property(typeof(MapPointTypeCounts), nameof(MapPointTypeCounts.NumOfShops)) ?? throw new Exception("[GlutonsAscensions] Unable to get NumOfShops property");
    
    [HarmonyPatch(typeof(MapPointTypeCounts), MethodType.Constructor, typeof(int), typeof(int))]
    [HarmonyPostfix]
    static void ReduceShopCount(MapPointTypeCounts __instance) {
        if (!GlutonsAscensionLevel.OutOfBusiness.HasAscension()) return;

        var numOfShops = __instance.NumOfShops;
        if (numOfShops <= 1) return;
        
        _numOfShopsProperty.SetBackingField(__instance, 1);
    }
    
    [HarmonyPatch(typeof(MapPointTypeCounts), MethodType.Constructor, typeof(ActMap))]
    [HarmonyPostfix]
    static void ReduceExistingMapShopCount(MapPointTypeCounts __instance, ActMap existingMap) {
        if (!GlutonsAscensionLevel.OutOfBusiness.HasAscension()) return;
        
        var numOfShops = existingMap.GetAllMapPoints().Count(p => p.PointType == MapPointType.Shop);
        if (numOfShops <= 1) return;
        
        _numOfShopsProperty.SetBackingField(__instance, 1);
    }
}