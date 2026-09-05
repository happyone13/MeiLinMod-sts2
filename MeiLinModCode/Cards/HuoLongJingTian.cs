using System;
using System.Collections.Generic;
using MeiLinMod.MeiLinModCode.Migration;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.HoverTips;
using MeiLinMod.MeiLinModCode.Powers;
using MeiLinMod.MeiLinModCode.Services;
using MeiLinMod.MeiLinModCode.Vfx;
using MeiLinMod.MeiLinModCode.Mechanics.Settings;
using MeiLinMod.MeiLinModCode.Config;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class HuoLongJingTian() : MeiLinModCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    private const string EmberKey = "Ember";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(15m, ValueProp.Move),
        new DynamicVar(EmberKey, 2m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [MeiLinHoverTipFactory.Ember];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;
    public override string? CustomSpinePortraitScenePath =>
        "res://MeiLinMod/scenes/cards/huo_long_jing_tian_dynamic.tscn";
    public override SpinePortraitSlot CustomSpinePortraitSlot => SpinePortraitSlot.Ancient;
    public override bool UseCustomAncientFrame => true;
    public override bool UsesDynamicChaosFrame => true;
    public override string? CustomAncientBorderMaterialPath => ChaosAncientFrameMaterialPath;
    public override string? CustomAncientBannerMaterialPath => ChaosAncientBannerMaterialPath;

    // This card owns its complete UG timeline; do not enqueue a normal attack.
    public override Task BeforeCardPlayed(CardPlay cardPlay) => Task.CompletedTask;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (MeiLinModConfig.UseCombatEffects && MeiLinSharedSettings.UltimateCinematicsEnabled)
        {
            MeiLinAudioService.SuppressNextDefaultAttackSfx(Owner);
            MeiLinAudioService.TryPlayUgAttackVoice(Owner);
            MeiLinAudioService.TryPlayUgAttackSound(Owner);
        }

        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        await MeiLinUgPresentation.PlayAsync(Owner.Creature, [cardPlay.Target], async cinematic =>
        {
            var attack = DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target);
            if (cinematic)
                attack.WithNoAttackerAnim();
            else
                attack.WithHitFx("vfx/vfx_attack_slash");
            await attack.Execute(choiceContext);

            await PowerCmd.Apply<EmberPower>(new BlockingPlayerChoiceContext(), cardPlay.Target, DynamicVars[EmberKey].BaseValue, Owner.Creature, this);
            await PowerCmd.Apply<EmberNoExpireThisTurnPower>(new BlockingPlayerChoiceContext(), cardPlay.Target, 1m, Owner.Creature, this, silent: true);
        });
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
        DynamicVars[EmberKey].UpgradeValueBy(1m);
    }
}
