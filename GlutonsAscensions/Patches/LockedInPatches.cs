using GlutonsAscensions.Helpers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;

namespace GlutonsAscensions.Patches;

[HarmonyPatch]
public class LockedInPatches {
    [HarmonyPatch(typeof(AscensionManager), nameof(AscensionManager.ApplyEffectsTo))]
    [HarmonyPostfix]
    static void MakeStartingDeckEternal(Player player) {
        if (!GlutonsAscensionLevel.LockedIn.HasAscension()) return;
        
        foreach (var card in player.Deck._cards) {
            if (card.Rarity == CardRarity.Basic && card.Tags.Contains(CardTag.Strike)) {
                card.AddKeyword(CardKeyword.Eternal);
            }
        }
    }
}