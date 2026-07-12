using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace GlutonsAscensions.Helpers;

public static class RunExtensions {
    extension(CardPile deck) {
        public int RemovableCardCount() => deck.Cards.Count(card => card.IsRemovable);
    }

    extension(Player player) {
        public string Name => PlatformUtil.GetPlayerNameRaw(RunManager.Instance.NetService.Platform, player.NetId);
        
        public bool HasRemovableCards(int min = 1) => player.Deck.RemovableCardCount() >= min;
    }
}