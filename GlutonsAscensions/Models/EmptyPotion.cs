using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Models.PotionPools;

namespace GlutonsAscensions.Models;

using static GlutonsAscensionsMod;

[Pool(typeof(TokenPotionPool))]
public class EmptyPotion : CustomPotionModel {
    public override PotionRarity Rarity => PotionRarity.None;
    public override PotionUsage Usage => PotionUsage.AnyTime;
    public override TargetType TargetType => TargetType.None;

    public override bool PassesCustomUsabilityCheck => false;

    public override string CustomPackedImagePath => ModResource("images/potions/empty_potion.png");
    public override string CustomPackedOutlinePath => ModResource("images/potions/empty_potion_outline.png");
}