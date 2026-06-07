using GlutonsAscensions.Helpers;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace GlutonsAscensions.Patches.AscensionLevels;

using static GlutonsAscensionsMod;
using static HarmonyExtensions;

[HarmonyPatch]
public class TornRugPatches {
    private static readonly string TornRugPath = ModResource("images/rooms/merchant_room/torn_shop_rug.png");
    
    // WARN: static constructor patches causes retriggering
    [HarmonyPatch(typeof(MerchantInventory), MethodType.StaticConstructor)]
    [HarmonyPostfix]
    static void ReduceColoredCards(ref CardType[] ____coloredCardTypes) {
        if (!GlutonsAscensionLevel.TornRug.HasAscension()) return;
        
        var coloredCardTypes = ____coloredCardTypes.ToList();
        coloredCardTypes.Remove(CardType.Attack);
        coloredCardTypes.Remove(CardType.Skill);
        ____coloredCardTypes = coloredCardTypes.ToArray();
    }

    [HarmonyPatch(typeof(MerchantCardEntry), nameof(MerchantCardEntry.SetOnSale))]
    [HarmonyPrefix]
    static bool PreventSetOnSale() => GlutonsAscensionLevel.TornRug.HasAscension() ? PrefixSkipOriginal : PrefixRunOriginal;

    [HarmonyPatch(typeof(NMerchantInventory), nameof(NMerchantInventory._Ready))]
    [HarmonyPostfix]
    static void ReplaceRugTexture(NMerchantInventory __instance) {
        if (!GlutonsAscensionLevel.TornRug.HasAscension()) return;
        
        if (__instance._slotsContainer is TextureRect textureRect) {
            textureRect.SetTexture(ResourceLoader.Load<Texture2D>(TornRugPath));
        }
    }

    [HarmonyPatch(typeof(NMerchantInventory), nameof(NMerchantInventory.Initialize))]
    [HarmonyPrefix]
    static void RemoveColoredCardSlots(NMerchantInventory __instance, MerchantInventory inventory) {
        if (!GlutonsAscensionLevel.TornRug.HasAscension()) return;

        if (inventory.CharacterCardEntries.Count != 3) {
            var ascensionLevel = GlutonsAscensionLevel.TornRug.Level();
            var ascensionName = GlutonsAscensionLevel.TornRug.FormattedName();
            Logger.Error($"Merchant inventory has the incorrect number of colored card for Ascension {ascensionLevel} ({ascensionName}): {inventory.CharacterCardEntries.Count}, expected 3");
            return;
        }

        if (__instance._characterCardContainer is null) return;

        // Remove the left 2 colored cards from the shop
        while (__instance._characterCardContainer.GetChildCount() > inventory.CharacterCardEntries.Count) {
            var leftMostChild = __instance._characterCardContainer.GetChild(0);
            __instance._characterCardContainer.RemoveChild(leftMostChild);
            leftMostChild.QueueFreeSafely();
        }
    }
}