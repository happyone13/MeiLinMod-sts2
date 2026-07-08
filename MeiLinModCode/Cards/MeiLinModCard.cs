using MeiLinMod.MeiLinModCode.Migration;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.Vfx;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Scaffolding.Content;

namespace MeiLinMod.MeiLinModCode.Cards;

public enum SpinePortraitSlot
{
    Normal,
    Ancient
}

[Pool(typeof(MeiLinModCardPool))]
public abstract class MeiLinModCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    ModCardTemplate(cost, type, rarity, target)
{
    protected const string ChaosAncientFrameMaterialPath =
        "res://MeiLinMod/materials/cards/frames/card_frame_chaos_mat.tres";
    protected const string ChaosAncientBannerMaterialPath =
        "res://MeiLinMod/materials/cards/banners/card_banner_chaos_mat.tres";

    protected string IdPortraitPath => $"{GetType().ToSnakeCaseAssetStem()}.png".CardImagePathOrDefault();
    // The "big portrait" slot now reuses the regular small portrait asset.
    protected string IdBigPortraitPath => $"{GetType().ToSnakeCaseAssetStem()}.png".BigCardImagePathOrDefault();

    // CustomPortraitPath is still the full-art hook, but it now resolves to the same
    // small portrait asset as the regular portrait slot.
    public override string? CustomPortraitPath => IdBigPortraitPath;

    //Smaller variants of card images for efficiency:
    //Smaller variant of fullart: 250x350
    //Smaller variant of normalart: 250x190

    //Uses card_portraits/card_name.png as image path. These should be smaller images.
    public override string? CustomBetaPortraitPath => null;
    public virtual string? CustomSpinePortraitScenePath => null;
    public virtual SpinePortraitSlot CustomSpinePortraitSlot => SpinePortraitSlot.Normal;
    public virtual bool UseCustomAncientFrame => false;
    public virtual bool UsesDynamicChaosFrame => false;
    public override string? CustomAncientBorderMaterialPath => null;
    public override string? CustomAncientBannerMaterialPath => null;
    protected virtual string? CombatTimelineName => Type switch
    {
        CardType.Power => "u4_buff",
        CardType.Skill => "debuff",
        _ => null
    };

    protected Task PlayPowerCastAnim()
    {
        if (Type != CardType.Power || Owner?.Creature == null || Owner.Character == null)
            return Task.CompletedTask;

        return PlayConfiguredTimeline();
    }

    protected void PrepareAttackAnimation(int hitCount = 1)
    {
        MeiLinBattleAnimationService.PrepareNextAttackHits(hitCount);
    }

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card != this || Owner?.Creature == null)
            return;

        if (Type == CardType.Attack)
        {
            MeiLinBattleAnimationService.PrepareNextAttackContext(Owner.Creature, cardPlay.Target);
            return;
        }

        if (Type == CardType.Skill)
            await PlayConfiguredTimeline(cardPlay.Target);
    }

    protected Task PlayConfiguredTimeline()
    {
        return PlayConfiguredTimeline(Owner?.Creature);
    }

    private Task PlayConfiguredTimeline(MegaCrit.Sts2.Core.Entities.Creatures.Creature? target)
    {
        if (Owner?.Creature == null || Owner.Character == null || string.IsNullOrWhiteSpace(CombatTimelineName))
            return Task.CompletedTask;

        target ??= Owner.Creature;

        if (CombatTimelineName == "debuff")
        {
            return MeiLinCommandVfxCoordinator.PlayCommandSequenceUntilFirstHitAsync(
                ["debuff_ready", "debuff_play"],
                Owner.Creature,
                target);
        }

        return MeiLinCommandVfxCoordinator.PlayCommandSetUntilFirstHitAsync(
            CombatTimelineName,
            Owner.Creature,
            target);
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        // Keep DynamicVar objects in the LocString so built-in :diff() highlighting works.
        DynamicVars.AddTo(description);
    }
}
