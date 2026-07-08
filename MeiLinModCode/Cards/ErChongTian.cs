using System;
using System.Collections.Generic;
using MeiLinMod.MeiLinModCode.Migration;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.HoverTips;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class ErChongTian() : MeiLinModCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    private const string EmberKey = "Ember";
    private const string ProgressKey = "Progress";

    protected override bool ShouldGlowGoldInternal => AwakeningHelper.CanAwakenNow(this);
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
        new DynamicVar(EmberKey, 2m),
        new DynamicVar(ProgressKey, 3m)
    ];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [MeiLinHoverTipFactory.Awakening, MeiLinHoverTipFactory.Ember, MeiLinHoverTipFactory.Qi];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCardCompat(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        var awakened = AwakeningHelper.IsAwakened(cardPlay);

        if (XiangzuCombatState.IsInAttackStance(Owner.Creature) || awakened)
            await PowerCmd.Apply<EmberPower>(new BlockingPlayerChoiceContext(), cardPlay.Target, DynamicVars[EmberKey].BaseValue, Owner.Creature, this);

        if (XiangzuCombatState.IsInGuardStance(Owner.Creature) || awakened)
            await QiCounterPower.AddProgress(Owner.Creature, DynamicVars[ProgressKey].IntValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars[EmberKey].UpgradeValueBy(1m);
        DynamicVars[ProgressKey].UpgradeValueBy(2m);
    }
}

