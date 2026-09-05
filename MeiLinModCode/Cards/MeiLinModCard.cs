using MeiLinMod.MeiLinModCode.Migration;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.Vfx;
using MeiLinMod.MeiLinModCode.Mechanics.Settings;
using MeiLinMod.MeiLinModCode.Config;
using MeiLinMod.MeiLinModCode.Services;
using MeiLinCharacterModel = MeiLinMod.MeiLinModCode.Character.MeiLinMod;
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
    protected bool UltimateCinematicPlayedForCurrentPlay { get; private set; }
    protected const string ChaosAncientFrameMaterialPath =
        "res://MeiLinMod/materials/cards/frames/card_frame_chaos_mat.tres";
    protected const string ChaosAncientBannerMaterialPath =
        "res://MeiLinMod/materials/cards/banners/card_banner_chaos_mat.tres";

    protected string IdPortraitPath => $"{GetType().ToSnakeCaseAssetStem()}.png".CardImagePathOrDefault();
    // Static and full-art slots share the same 606x852 source image, matching YukiMod's
    // unified full-frame card presentation.
    protected string IdBigPortraitPath => $"{GetType().ToSnakeCaseAssetStem()}.png".BigCardImagePathOrDefault();

    // CustomPortraitPath is the full-art hook and resolves to the same vertical source.
    public override string? CustomPortraitPath => IdBigPortraitPath;

    //Smaller variants of card images for efficiency:
    //Smaller variant of fullart: 250x350
    //Smaller variant of normalart: 250x190

    //Uses card_portraits/card_name.png as image path. These should be smaller images.
    public override string? CustomBetaPortraitPath => null;
    public virtual string? CustomSpinePortraitScenePath => null;
    public virtual SpinePortraitSlot CustomSpinePortraitSlot => SpinePortraitSlot.Ancient;
    public virtual bool UseCustomAncientFrame => true;
    public virtual bool UsesDynamicChaosFrame => false;
    public override string? CustomAncientBorderMaterialPath => ChaosAncientFrameMaterialPath;
    public override string? CustomAncientBannerMaterialPath => ChaosAncientBannerMaterialPath;
    protected virtual string? CombatTimelineName => Type switch
    {
        CardType.Power => "u4_buff",
        CardType.Skill => "debuff",
        _ => null
    };

    protected Task PlayPowerCastAnim()
    {
        if (UltimateCinematicPlayedForCurrentPlay)
            return Task.CompletedTask;

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
        UltimateCinematicPlayedForCurrentPlay = false;
        if (cardPlay.Card != this || Owner?.Creature == null || Owner.Character is not MeiLinCharacterModel)
            return;

        if (MeiLinModConfig.UseCombatEffects && MeiLinSharedSettings.UltimateCinematicsEnabled && ShouldPlayUxCinematic())
        {
            MeiLinAudioService.SuppressNextDefaultCastSfx(Owner);
            MeiLinAudioService.TryPlayUxVoice(Owner);
            MeiLinAudioService.TryPlayUxSound(Owner);
            bool played = false;
            await MeiLinUxPresentation.PlayAsync(Owner.Creature, [], cinematic =>
            {
                played = cinematic;
                return Task.CompletedTask;
            });
            UltimateCinematicPlayedForCurrentPlay = played;
            if (played)
                return;
        }

        if (MeiLinModConfig.UseCombatEffects && MeiLinSharedSettings.UltimateCinematicsEnabled && ShouldPlayUgCinematic())
        {
            MeiLinAudioService.SuppressNextDefaultAttackSfx(Owner);
            MeiLinAudioService.TryPlayUgAttackVoice(Owner);
            MeiLinAudioService.TryPlayUgAttackSound(Owner);
            var targets = cardPlay.Target != null
                ? new[] { cardPlay.Target }
                : CombatState?.HittableEnemies.ToArray() ?? [];
            bool played = false;
            await MeiLinUgPresentation.PlayAsync(Owner.Creature, targets, cinematic =>
            {
                played = cinematic;
                return Task.CompletedTask;
            });
            UltimateCinematicPlayedForCurrentPlay = played;
            if (played)
                return;
        }

        if (Type == CardType.Attack)
        {
            MeiLinBattleAnimationService.PrepareNextAttackContext(Owner.Creature, cardPlay.Target);
            return;
        }

        if (Type == CardType.Skill)
            await PlayConfiguredTimeline(cardPlay.Target);
    }

    private bool ShouldPlayUxCinematic()
    {
        if (this is ZuiZhongAoYiYanLongJiangLin or ShenGongFangYiTi)
            return true;

        return Type == CardType.Power
            && !EnergyCost.CostsX
            && EnergyCost.GetWithModifiers(CostModifiers.All) >= 3;
    }

    private bool ShouldPlayUgCinematic()
    {
        if (Type != CardType.Attack
            || !DynamicVars.ContainsKey("Damage")
            || DynamicVars.Damage.BaseValue <= 100m)
            return false;

        return this is not LianYun
            and not ShanDianWuLianBian
            and not ShengLongJiao
            and not ShuangLongChuHai
            and not YanLongJiangLin
            and not YiQiHeCheng
            and not KaiTian
            and not ShanHe;
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
