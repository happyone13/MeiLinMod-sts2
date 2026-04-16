using System;
using System.Collections.Generic;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
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
public class KaiTian() : MeiLinModCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override bool GainsBlock => true;
    protected override bool IsPlayable => (Owner.Creature.GetPower<QiPower>()?.Amount ?? 0m) >= 1m;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(20m, ValueProp.Move),
        new BlockVar(15m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        MeiLinHoverTipFactory.Qi,
        MeiLinHoverTipFactory.QiGauge,
        MeiLinHoverTipFactory.QiConsume,
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if ((Owner.Creature.GetPower<QiPower>()?.Amount ?? 0m) < 1m)
            return;

        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        await DamageCmd.Attack(1m)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        await PowerCmd.Apply<QiPower>(Owner.Creature, -1m, Owner.Creature, this);

        if (XiangzuLegacyPower.IsInAttackStance(Owner.Creature))
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .Execute(choiceContext);
        }

        if (XiangzuLegacyPower.IsInGuardStance(Owner.Creature))
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move | ValueProp.Unpowered, null, fast: true);
    }

    protected override PileType GetResultPileType()
    {
        var result = base.GetResultPileType();
        if (result == PileType.Discard)
            return PileType.Hand;

        return result;
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(10m);
        DynamicVars.Block.UpgradeValueBy(5m);
    }
}
