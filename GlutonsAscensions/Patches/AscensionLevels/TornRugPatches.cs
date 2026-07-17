using GlutonsAscensions.Helpers;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
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

        if (__instance.GetType() != typeof(NMerchantInventory)) return; // Skips NFakeMerchantInventory
        
        if (__instance._slotsContainer is TextureRect textureRect) {
            textureRect.SetTexture(ResourceLoader.Load<Texture2D>(TornRugPath));
        }
    }

    [HarmonyPatch(typeof(NMerchantInventory), nameof(NMerchantInventory.Initialize))]
    [HarmonyPrefix]
    static void RemoveColoredCardSlots(NMerchantInventory __instance, MerchantInventory inventory) {
        if (!GlutonsAscensionLevel.TornRug.HasAscension()) return;
        
        if (__instance.GetType() != typeof(NMerchantInventory)) return; // Skips NFakeMerchantInventory
        if (__instance._characterCardContainer is null) return;

        while (__instance._characterCardContainer.GetChildCount() > inventory.CharacterCardEntries.Count) {
            var leftMostColoredCardSlot = __instance._characterCardContainer.GetChild<NMerchantCard>(0);
            __instance._characterCardContainer.RemoveChildSafely(leftMostColoredCardSlot);
            leftMostColoredCardSlot.QueueFreeSafely();
        }
    }

    [HarmonyPatch(typeof(NMerchantCard), nameof(NMerchantCard._ExitTree))]
    [HarmonyPrefix]
    static bool _ExitTreePrefix(NMerchantCard __instance) {
        if (!GlutonsAscensionLevel.TornRug.HasAscension()) return PrefixRunOriginal;

        // Despite _cardEntry not being nullable, if NMerchantCard::FillSlot is never called
        // (which would be the case when removing the node early like in RemoveColoredCardSlots),
        // then it will not have been set by the time _ExitTree is called,
        // thus is a good indication of whether _ExitTree needs to be ran or not
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (__instance._cardEntry is null) {
            return PrefixSkipOriginal;
        }

        return PrefixRunOriginal;
    }
}