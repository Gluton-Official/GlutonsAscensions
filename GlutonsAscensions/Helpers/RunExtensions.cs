using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Rooms;

namespace GlutonsAscensions.Helpers;

public static class RunExtensions {
    public static bool IsNeow(this AbstractRoom? room) =>
        room is { RoomType: RoomType.Event } and EventRoom { CanonicalEvent: Neow };

    public static int RemovableCardCount(this CardPile deck) => deck.Cards.Count(card => card.IsRemovable);
    public static bool HasRemovableCards(this Player player, int min = 1) => player.Deck.RemovableCardCount() >= min;
}