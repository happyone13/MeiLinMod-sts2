using System;
using System.Collections.Generic;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.HoverTips;
using MeiLinMod.MeiLinModCode.Powers;
using MeiLinMod.MeiLinModCode.Services;
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

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [MeiLinHoverTipFactory.Ember];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        MeiLinAudioService.SuppressNextDefaultAttackSfx(Owner);
        MeiLinAudioService.TryPlayCustomCardClip("huo_long_jing_tian", Owner);

        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        await PowerCmd.Apply<EmberPower>(cardPlay.Target, DynamicVars[EmberKey].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<EmberNoExpireThisTurnPower>(cardPlay.Target, 1m, Owner.Creature, this, silent: true);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
        DynamicVars[EmberKey].UpgradeValueBy(1m);
    }
}
