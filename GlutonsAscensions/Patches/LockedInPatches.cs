using GlutonsAscensions.Helpers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace GlutonsAscensions.Patches;

[HarmonyPatch]
public class LockedInPatches {
    [HarmonyPatch(typeof(AscensionManager), nameof(AscensionManager.ApplyEffectsTo))]
    [HarmonyPostfix]
    static void MakeStartingDeckEternal(Player player) {
        if (!GlutonsAscensionLevel.LockedIn.HasAscension()) return;
        
        foreach (var card in player.Deck.Cards) {
            card.AddKeyword(CardKeyword.Eternal);
        }
    }

    private static bool IsNeow(IRunState runState) {
        var currentRoom = runState.CurrentRoom;
        return currentRoom?.RoomType == RoomType.Event && currentRoom is EventRoom { CanonicalEvent: Neow };
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.ModifyCardBeingAddedToDeck))]
    [HarmonyPostfix]
    static void MakeAddedCardsAtRunStartEternal(IRunState runState, ref CardModel __result) {
        if (!GlutonsAscensionLevel.LockedIn.HasAscension() || !IsNeow(runState)) return;
        
        var card = runState.CloneCard(__result);
        card.AddKeyword(CardKeyword.Eternal);
        __result = card;
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.TryModifyCardRewardOptions))]
    [HarmonyPostfix]
    static void MakeCardRewardOptionsAtRunStartEternal(IRunState runState, List<CardCreationResult> cardRewardOptions, ref bool __result) {
        if (!GlutonsAscensionLevel.LockedIn.HasAscension() || !IsNeow(runState)) return;
        
        foreach (var cardRewardOption in cardRewardOptions) {
            cardRewardOption.Card.AddKeyword(CardKeyword.Eternal);
            __result = true;
        }
    }

    [HarmonyPatch(typeof(RunState), nameof(RunState.FromSerializable))]
    [HarmonyPostfix]
    static void MakeFloor1CardsEternal(RunState __result) {
        if (__result.AscensionLevel < GlutonsAscensionLevel.LockedIn.Level()) return;

        foreach (var player in __result.Players) {
            foreach (var card in player.Deck.Cards) {
                if (card.FloorAddedToDeck == 1) {
                    card.AddKeyword(CardKeyword.Eternal);
                }
            }
        }
    }
}