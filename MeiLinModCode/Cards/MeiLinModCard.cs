using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;

namespace MeiLinMod.MeiLinModCode.Cards;

public enum SpinePortraitSlot
{
    Normal,
    Ancient
}

[Pool(typeof(MeiLinModCardPool))]
public abstract class MeiLinModCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    CustomCardModel(cost, type, rarity, target)
{
    protected const string ChaosAncientFrameMaterialPath =
        "res://MeiLinMod/materials/cards/frames/card_frame_chaos_mat.tres";
    protected const string ChaosAncientBannerMaterialPath =
        "res://MeiLinMod/materials/cards/banners/card_banner_chaos_mat.tres";

    protected string IdPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePathOrDefault();
    // The "big portrait" slot now reuses the regular small portrait asset.
    protected string IdBigPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePathOrDefault();

    // CustomPortraitPath is still the full-art hook, but it now resolves to the same
    // small portrait asset as the regular portrait slot.
    public override string CustomPortraitPath => IdBigPortraitPath;

    //Smaller variants of card images for efficiency:
    //Smaller variant of fullart: 250x350
    //Smaller variant of normalart: 250x190

    //Uses card_portraits/card_name.png as image path. These should be smaller images.
    public override string PortraitPath => IdPortraitPath;
    public override string BetaPortraitPath => $"beta/{Id.Entry.ToLowerInvariant()}.png".CardImagePath();
    public virtual string? CustomSpinePortraitScenePath => null;
    public virtual SpinePortraitSlot CustomSpinePortraitSlot => SpinePortraitSlot.Normal;
    public virtual bool UseCustomAncientFrame => false;
    public virtual bool UsesDynamicChaosFrame => false;
    public virtual string? CustomAncientFrameMaterialPath => null;
    public virtual string? CustomAncientBannerMaterialPath => null;

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
