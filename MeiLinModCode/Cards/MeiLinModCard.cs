using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public abstract class MeiLinModCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    CustomCardModel(cost, type, rarity, target)
{
    protected string IdPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePathOrDefault();
    protected string IdBigPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePathOrDefault();

    //Image size:
    //Normal art: 1000x760 (Using 500x380 should also work, it will simply be scaled.)
    //Full art: 606x852
    public override string CustomPortraitPath => IdBigPortraitPath;

    //Smaller variants of card images for efficiency:
    //Smaller variant of fullart: 250x350
    //Smaller variant of normalart: 250x190

    //Uses card_portraits/card_name.png as image path. These should be smaller images.
    public override string PortraitPath => IdPortraitPath;
    public override string BetaPortraitPath => $"beta/{Id.Entry.ToLowerInvariant()}.png".CardImagePath();

    protected Task PlayPowerCastAnim()
    {
        if (Type != CardType.Power || Owner?.Creature == null || Owner.Character == null)
            return Task.CompletedTask;

        return CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
    }

    protected void PrepareAttackAnimation(int hitCount = 1)
    {
        MeiLinBattleAnimationService.PrepareNextAttackHits(hitCount);
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        // Keep DynamicVar objects in the LocString so built-in :diff() highlighting works.
        DynamicVars.AddTo(description);
    }
}
